using System.Threading.Tasks;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Windows.System;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// The main photo grid: justified virtualized rows grouped by date, date chip, year timeline,
    /// filtering/sorting/zoom options, multi-selection and batch actions.
    /// </summary>
    public sealed partial class GalleryView : UserControl, IPhotoTileHost
    {
        private const double Gap = 6;

        private readonly HashSet<GalleryRowPresenter> _realized = [];
        private readonly HashSet<PhotoItem> _selected = [];
        private PhotosShell? _shell;
        private PhotoSourceKind _source = PhotoSourceKind.All;
        private string? _sourceKey;
        private string _search = string.Empty;
        private PhotoTypeFilter _typeFilter = PhotoTypeFilter.All;
        private PhotoOrientationFilter _orientationFilter = PhotoOrientationFilter.Any;
        private List<PhotoItem> _items = [];
        private List<GalleryRow> _rows = [];
        private PhotoItem? _anchor;
        private double _layoutWidth;
        private DispatcherQueueTimer? _relayoutTimer;
        private bool _timelineDragging;
        private Border? _timelineThumb;
        private Border? _timelineBubble;
        private bool _dateChipAllowed;
        private TextBlock? _timelineBubbleText;

        public GalleryView()
        {
            InitializeComponent();
            Scroller.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(Scroller_PointerWheelChanged), true);
            SelectionBar.SizeChanged += (_, _) => AdaptSelectionBar();
            RootGrid.SizeChanged += (_, _) => AdaptHeader();
            // Daisy buttons get their final size after templating; re-check when the header content grows.
            HeaderLeft.SizeChanged += (_, _) => QueueAdaptHeader();
            HeaderRight.SizeChanged += (_, _) => QueueAdaptHeader();
            PhotosLocalization.Instance.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(OnLanguageChanged);
            FlowerySizeManager.SizeChanged += (_, _) => DispatcherQueue.TryEnqueue(() =>
            {
                BuildMenus();
                RebuildRows(keepPosition: true);
            });
            ImportMenu.Opening += (_, _) => ApplyFlyoutFont(ImportMenu.Items);
            CollectionMenu.Opening += (_, _) => ApplyFlyoutFont(CollectionMenu.Items);
        }

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        public bool IsSelectionMode => _selected.Count > 0;

        public bool ShowFileNames => PhotosSettings.ShowFileNames;

        public IReadOnlyList<PhotoItem> CurrentItems => _items;

        public void Initialize(PhotosShell shell)
        {
            _shell = shell;
            var library = PhotoLibrary.Instance;
            library.ScanProgress += OnScanProgress;
            library.ScanningChanged += (_, scanning) =>
            {
                ScanProgressBar.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
                if (!scanning)
                    UpdateEmptyState();
            };
            BuildMenus();
            UpdateTooltips();
        }

        #region Source & query

        public void ShowSource(PhotoSourceKind kind, string? key)
        {
            var changed = kind != _source || !string.Equals(key, _sourceKey, PathRules.Comparison);
            _source = kind;
            _sourceKey = key;
            if (changed)
                ClearSelection();

            BackButton.Visibility = kind is PhotoSourceKind.Album or PhotoSourceKind.Folder ? Visibility.Visible : Visibility.Collapsed;
            CollectionMenuButton.Visibility = kind is PhotoSourceKind.Album or PhotoSourceKind.Folder ? Visibility.Visible : Visibility.Collapsed;
            SelectionRemoveFromAlbumButton.Visibility = kind == PhotoSourceKind.Album ? Visibility.Visible : Visibility.Collapsed;
            BuildCollectionMenu();
            UpdateTitle();
            Reload(scrollToTop: changed);
        }

        private void UpdateTitle()
        {
            TitleText.Text = _source switch
            {
                PhotoSourceKind.Favorites => L("Photos_Nav_Favorites"),
                PhotoSourceKind.Album => _sourceKey ?? L("Photos_Nav_Albums"),
                PhotoSourceKind.Folder => _sourceKey is { } key ? PathRules.FolderName(key) : string.Empty,
                _ => L("Photos_Nav_Gallery")
            };
            ToolTipService.SetToolTip(TitleText, _source == PhotoSourceKind.Folder ? _sourceKey : null);
        }

        public void ApplySearch(string text)
        {
            _search = text ?? string.Empty;
            Reload(scrollToTop: true);
        }

        public void RefreshFromLibrary()
        {
            // Albums may have been renamed or deleted.
            if (_source == PhotoSourceKind.Album && PhotoLibrary.Instance.FindAlbum(_sourceKey) == null)
            {
                _shell?.BackToCollections();
                return;
            }
            Reload(scrollToTop: false);
        }

        private PhotoQuery BuildQuery() => new()
        {
            Source = _source,
            SourceKey = _sourceKey,
            SearchText = _search,
            TypeFilter = _typeFilter,
            OrientationFilter = _orientationFilter,
            Sort = PhotosSettings.SortMode
        };

        private bool IsDateSorted => PhotosSettings.SortMode is PhotoSortMode.DateTakenNewest or PhotoSortMode.DateTakenOldest;

        private void Reload(bool scrollToTop)
        {
            _items = PhotoLibrary.Instance.Query(BuildQuery());

            // Drop selections that are no longer visible.
            var visible = new HashSet<PhotoItem>(_items);
            foreach (var item in _selected.Where(i => !visible.Contains(i)).ToList())
            {
                item.IsSelected = false;
                _selected.Remove(item);
            }

            CountText.Text = PhotosLocalization.Format(_items.Count == 1 ? "Photos_ItemCountOne" : "Photos_ItemCount", _items.Count);
            FilterButton.Variant = _typeFilter != PhotoTypeFilter.All || _orientationFilter != PhotoOrientationFilter.Any
                ? DaisyButtonVariant.Accent
                : DaisyButtonVariant.Primary;
            // Active filters: filled accent; otherwise filled primary like the other text buttons.
            FilterButton.ButtonStyle = DaisyButtonStyle.Default;
            SlideshowButton.IsEnabled = _items.Count > 0;

            RebuildRows(keepPosition: !scrollToTop);
            if (scrollToTop)
                Scroller.ChangeView(null, 0, null, true);

            UpdateSelectionBar();
            UpdateEmptyState();
        }

        #endregion

        #region Layout

        private double GetLayoutWidth() =>
            Math.Max(0, Scroller.ActualWidth - Scroller.Padding.Left - Scroller.Padding.Right - 4);

        private void RebuildRows(bool keepPosition)
        {
            var width = GetLayoutWidth();
            if (width < 50)
                return;

            PhotoItem? anchor = null;
            var anchorDelta = 0.0;
            if (keepPosition && _rows.Count > 0)
            {
                var index = GalleryLayout.FindRowAt(_rows, Scroller.VerticalOffset);
                if (index >= 0)
                {
                    var row = _rows[index];
                    anchor = row is GalleryPhotoRow pr ? pr.Tiles[0].Item : (row as GalleryHeaderRow)?.Items.FirstOrDefault();
                    anchorDelta = Scroller.VerticalOffset - row.Top;
                }
            }

            _layoutWidth = width;
            var groupMode = IsDateSorted ? PhotosSettings.GroupMode : PhotoGroupMode.None;
            var tileHeight = PhotosSettings.TileHeight * GetSizeFactor();
            _rows = GalleryLayout.Build(_items, width, tileHeight, Gap, groupMode, PhotosSettings.SquareTiles);
            Repeater.ItemsSource = _rows;

            Timeline.Visibility = IsDateSorted && _rows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            _dateChipAllowed = IsDateSorted && _rows.Count > 0;
            DateChip.Visibility = _dateChipAllowed ? Visibility.Visible : Visibility.Collapsed;
            AdaptHeader();

            if (anchor != null)
            {
                var target = FindRowOf(anchor);
                if (target != null)
                {
                    var offset = target.Top + Math.Min(anchorDelta, target.Height);
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => Scroller.ChangeView(null, offset, null, true));
                }
            }

            BuildTimeline();
            UpdateDateChip();
        }

        private static double GetSizeFactor() => FlowerySizeManager.CurrentSize switch
        {
            DaisySize.ExtraSmall => 0.85,
            DaisySize.Small => 1.0,
            DaisySize.Medium => 1.12,
            DaisySize.Large => 1.25,
            DaisySize.ExtraLarge => 1.4,
            _ => 1.0
        };

        private GalleryRow? FindRowOf(PhotoItem item) =>
            _rows.FirstOrDefault(r => r is GalleryPhotoRow pr && pr.Tiles.Any(t => ReferenceEquals(t.Item, item)));

        private void Scroller_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Math.Abs(GetLayoutWidth() - _layoutWidth) < 1)
            {
                BuildTimeline();
                return;
            }

            if (_relayoutTimer == null)
            {
                _relayoutTimer = DispatcherQueue.CreateTimer();
                _relayoutTimer.Interval = TimeSpan.FromMilliseconds(60);
                _relayoutTimer.IsRepeating = false;
                _relayoutTimer.Tick += (_, _) => RebuildRows(keepPosition: true);
            }
            _relayoutTimer.Stop();
            _relayoutTimer.Start();
        }

        private void Repeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            if (args.Element is GalleryRowPresenter presenter && args.Index >= 0 && args.Index < _rows.Count)
            {
                presenter.SetRow(_rows[args.Index], this, Gap);
                _realized.Add(presenter);
            }
        }

        private void Repeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
        {
            if (args.Element is GalleryRowPresenter presenter)
            {
                presenter.Clear();
                _realized.Remove(presenter);
            }
        }

        private void RefreshVisibleStates()
        {
            foreach (var presenter in _realized)
                presenter.RefreshState();
        }

        private void Scroller_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (!ctrl)
                return;

            var delta = e.GetCurrentPoint(Scroller).Properties.MouseWheelDelta;
            ZoomTiles(delta > 0 ? 1.12 : 1 / 1.12);
            e.Handled = true;
        }

        private void ZoomTiles(double factor)
        {
            var newHeight = Math.Clamp(PhotosSettings.TileHeight * factor, 90, 420);
            if (Math.Abs(newHeight - PhotosSettings.TileHeight) < 0.5)
                return;
            PhotosSettings.TileHeight = newHeight;
            PhotosSettings.SaveOptions();
            RebuildRows(keepPosition: true);
        }

        #endregion

        #region Date chip & timeline

        private void Scroller_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateDateChip();
            UpdateTimelineThumb();
        }

        private void UpdateDateChip()
        {
            if (_rows.Count == 0 || !IsDateSorted)
                return;
            var index = GalleryLayout.FindRowAt(_rows, Scroller.VerticalOffset + 4);
            if (index < 0)
                return;
            var row = _rows[index];
            var date = row is GalleryPhotoRow pr ? pr.Tiles[0].Item.SortDate : row.Date;
            DateChipText.Text = PhotosSettings.GroupMode == PhotoGroupMode.Year
                ? GalleryLayout.FormatGroupTitle(date, PhotoGroupMode.Month, PhotosLocalization.CurrentCulture)
                : GalleryLayout.FormatLongDate(date, PhotosLocalization.CurrentCulture);
        }

        private double TotalHeight => _rows.Count == 0 ? 1 : Math.Max(1, _rows[^1].Top + _rows[^1].Height);

        private void Timeline_SizeChanged(object sender, SizeChangedEventArgs e) => BuildTimeline();

        private void BuildTimeline()
        {
            TimelineCanvas.Children.Clear();
            _timelineThumb = null;
            if (Timeline.Visibility != Visibility.Visible || _rows.Count == 0)
                return;

            var height = Timeline.ActualHeight;
            var width = Timeline.ActualWidth;
            if (height <= 0)
                return;

            var total = TotalHeight;
            var contentBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            var lastLabelY = double.NegativeInfinity;
            var lastDotY = double.NegativeInfinity;
            int? lastYear = null;
            int? lastMonth = null;

            foreach (var row in _rows)
            {
                if (row is not GalleryPhotoRow pr)
                    continue;
                var date = pr.Tiles[0].Item.SortDate;
                var y = row.Top / total * height;

                if (lastYear != date.Year)
                {
                    lastYear = date.Year;
                    lastMonth = date.Month;
                    if (y - lastLabelY >= 22)
                    {
                        var label = new TextBlock
                        {
                            Text = date.Year.ToString(CultureInfo.InvariantCulture),
                            FontSize = 11,
                            FontWeight = FontWeights.SemiBold,
                            Opacity = 0.75,
                            Foreground = contentBrush,
                            Width = width,
                            TextAlignment = TextAlignment.Center,
                            IsHitTestVisible = false
                        };
                        Canvas.SetTop(label, Math.Min(y, height - 14));
                        TimelineCanvas.Children.Add(label);
                        lastLabelY = y;
                        lastDotY = y;
                    }
                }
                else if (lastMonth != date.Month)
                {
                    lastMonth = date.Month;
                    if (y - lastDotY >= 8 && y - lastLabelY >= 18)
                    {
                        var dot = new Ellipse
                        {
                            Width = 4,
                            Height = 4,
                            Fill = contentBrush,
                            Opacity = 0.35,
                            IsHitTestVisible = false
                        };
                        Canvas.SetLeft(dot, width / 2 - 2);
                        Canvas.SetTop(dot, y);
                        TimelineCanvas.Children.Add(dot);
                        lastDotY = y;
                    }
                }
            }

            _timelineThumb = new Border
            {
                Width = 26,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(_timelineThumb, width / 2 - 13);
            TimelineCanvas.Children.Add(_timelineThumb);

            _timelineBubbleText = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush")
            };
            _timelineBubble = new Border
            {
                Padding = new Thickness(10, 4, 10, 4),
                CornerRadius = new CornerRadius(12),
                Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                Child = _timelineBubbleText,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            TimelineCanvas.Children.Add(_timelineBubble);
            UpdateTimelineThumb();
        }

        private void UpdateTimelineThumb()
        {
            if (_timelineThumb == null || Timeline.ActualHeight <= 0)
                return;
            var fraction = Scroller.ScrollableHeight <= 0 ? 0 : Scroller.VerticalOffset / Scroller.ScrollableHeight;
            Canvas.SetTop(_timelineThumb, Math.Clamp(fraction, 0, 1) * (Timeline.ActualHeight - 4));
        }

        private void ScrollToTimelinePosition(double y)
        {
            var height = Timeline.ActualHeight;
            if (height <= 0 || _rows.Count == 0)
                return;
            var fraction = Math.Clamp(y / height, 0, 1);
            var offset = fraction * TotalHeight;
            Scroller.ChangeView(null, Math.Min(offset, Scroller.ScrollableHeight), null, true);
        }

        private void ShowTimelineBubble(double y)
        {
            if (_timelineBubble == null || _timelineBubbleText == null || _rows.Count == 0)
                return;
            var height = Math.Max(1, Timeline.ActualHeight);
            var offset = Math.Clamp(y / height, 0, 1) * TotalHeight;
            var index = GalleryLayout.FindRowAt(_rows, offset);
            if (index < 0)
                return;
            var row = _rows[index];
            var date = row is GalleryPhotoRow pr ? pr.Tiles[0].Item.SortDate : row.Date;
            _timelineBubbleText.Text = GalleryLayout.FormatGroupTitle(date, PhotoGroupMode.Month, PhotosLocalization.CurrentCulture);
            _timelineBubble.Visibility = Visibility.Visible;
            _timelineBubble.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(_timelineBubble, -_timelineBubble.DesiredSize.Width - 4);
            Canvas.SetTop(_timelineBubble, Math.Clamp(y - 12, 0, height - 24));
        }

        private void Timeline_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _timelineDragging = Timeline.CapturePointer(e.Pointer);
            var y = e.GetCurrentPoint(Timeline).Position.Y;
            ScrollToTimelinePosition(y);
            ShowTimelineBubble(y);
            e.Handled = true;
        }

        private void Timeline_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var y = e.GetCurrentPoint(Timeline).Position.Y;
            ShowTimelineBubble(y);
            if (_timelineDragging)
                ScrollToTimelinePosition(y);
        }

        private void Timeline_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _timelineDragging = false;
            Timeline.ReleasePointerCapture(e.Pointer);
        }

        private void Timeline_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => _timelineDragging = false;

        private void Timeline_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_timelineDragging && _timelineBubble != null)
                _timelineBubble.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Tile host

        public async void OnTileInvoked(PhotoItem item, bool ctrl, bool shift)
        {
            if (shift && _anchor != null)
            {
                SelectRange(_anchor, item);
                return;
            }

            if (ctrl || IsSelectionMode)
            {
                ToggleSelection(item);
                return;
            }

            if (_shell != null)
                await _shell.OpenViewerAsync(_items, _items.IndexOf(item));
        }

        public void OnTileSelectToggled(PhotoItem item, bool shift)
        {
            if (shift && _anchor != null)
                SelectRange(_anchor, item);
            else
                ToggleSelection(item);
        }

        public void OnHeaderSelectToggled(GalleryHeaderRow row)
        {
            var allSelected = row.Items.All(i => i.IsSelected);
            foreach (var item in row.Items)
                SetSelected(item, !allSelected);
            OnSelectionChanged();
        }

        public void OnTileContextRequested(PhotoItem item, FrameworkElement target, Point position)
        {
            var targets = item.IsSelected && _selected.Count > 1 ? _selected.ToList() : [item];
            var menu = new MenuFlyout();

            if (targets.Count == 1)
            {
                menu.Items.Add(MenuItem("Photos_Open", Symbol.OpenFile, () => OnTileInvoked(item, false, false)));
                menu.Items.Add(MenuItem("Photos_Edit", null, async () =>
                {
                    if (_shell != null)
                        await _shell.OpenViewerAsync(_items, _items.IndexOf(item), edit: true);
                }, Icon("PhotosIconTune")));
                menu.Items.Add(new MenuFlyoutSeparator());
            }

            var allFavorite = targets.All(t => t.IsFavorite);
            menu.Items.Add(MenuItem(allFavorite ? "Photos_Unfavorite" : "Photos_Favorite", null,
                () => PhotoLibrary.Instance.SetFavorite(targets, !allFavorite), Icon(allFavorite ? "DaisyIconHeart" : "PhotosIconHeartOutline")));

            var albumMenu = new MenuFlyoutSubItem { Text = L("Photos_AddToAlbum"), Icon = new SymbolIcon(Symbol.Library) };
            FillAlbumMenu(albumMenu.Items, targets);
            menu.Items.Add(albumMenu);

            if (_source == PhotoSourceKind.Album)
                menu.Items.Add(MenuItem("Photos_RemoveFromAlbum", Symbol.Remove, () => RemoveFromCurrentAlbum(targets)));

            if (!IsSelectionMode)
                menu.Items.Add(MenuItem("Photos_Select", Symbol.SelectAll, () => ToggleSelection(item)));

            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(MenuItem("Photos_Copy", Symbol.Copy, () => _ = CopyAsync(targets)));

            if (targets.Count == 1)
            {
                menu.Items.Add(MenuItem("Photos_OpenWith", Symbol.OpenWith, () => _ = FileOperations.OpenWithAsync(item.Path)));
                menu.Items.Add(MenuItem("Photos_ShowInExplorer", Symbol.Folder, () => FileOperations.ShowInExplorer(item.Path)));
                menu.Items.Add(MenuItem("Photos_SetAsBackground", Symbol.SetLockScreen, () =>
                {
                    var ok = FileOperations.SetAsWallpaper(item.Path);
                    _shell?.ShowToast(L(ok ? "Photos_BackgroundSet" : "Photos_ActionFailed"), ok ? DaisyAlertVariant.Success : DaisyAlertVariant.Error);
                }));
            }

            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(MenuItem("Photos_Delete", Symbol.Delete, () => _ = DeleteAsync(targets)));

            ApplyFlyoutFont(menu.Items);
            menu.ShowAt(target, new FlyoutShowOptions { Position = position });
        }

        #endregion

        #region Selection

        private void SetSelected(PhotoItem item, bool selected)
        {
            item.IsSelected = selected;
            if (selected)
                _selected.Add(item);
            else
                _selected.Remove(item);
        }

        private void ToggleSelection(PhotoItem item)
        {
            SetSelected(item, !item.IsSelected);
            _anchor = item;
            OnSelectionChanged();
        }

        private void SelectRange(PhotoItem from, PhotoItem to)
        {
            var a = _items.IndexOf(from);
            var b = _items.IndexOf(to);
            if (a < 0 || b < 0)
            {
                ToggleSelection(to);
                return;
            }
            for (var i = Math.Min(a, b); i <= Math.Max(a, b); i++)
                SetSelected(_items[i], true);
            OnSelectionChanged();
        }

        public void SelectAll()
        {
            foreach (var item in _items)
                SetSelected(item, true);
            OnSelectionChanged();
        }

        public void ClearSelection()
        {
            foreach (var item in _selected)
                item.IsSelected = false;
            _selected.Clear();
            _anchor = null;
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            UpdateSelectionBar();
            RefreshVisibleStates();
        }

        private void UpdateSelectionBar()
        {
            SelectionBar.Visibility = _selected.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            SelectionText.Text = PhotosLocalization.Format(_selected.Count == 1 ? "Photos_SelectedOne" : "Photos_SelectedMany", _selected.Count);
            var allFavorite = _selected.Count > 0 && _selected.All(i => i.IsFavorite);
            SelectionFavoriteButton.IconData = Icon(allFavorite ? "DaisyIconHeart" : "PhotosIconHeartOutline");
            AdaptSelectionBar();
        }

        private List<PhotoItem> SelectedInOrder() => [.. _items.Where(i => i.IsSelected)];

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e) => ClearSelection();

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) => SelectAll();

        private void SelectionFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var targets = SelectedInOrder();
            var allFavorite = targets.All(i => i.IsFavorite);
            PhotoLibrary.Instance.SetFavorite(targets, !allFavorite);
            UpdateSelectionBar();
        }

        private void SelectionAlbumMenu_Opening(object sender, object e)
        {
            SelectionAlbumMenu.Items.Clear();
            FillAlbumMenu(SelectionAlbumMenu.Items, SelectedInOrder());
            ApplyFlyoutFont(SelectionAlbumMenu.Items);
        }

        private void SelectionRemoveFromAlbumButton_Click(object sender, RoutedEventArgs e) => RemoveFromCurrentAlbum(SelectedInOrder());

        private void SelectionCopyButton_Click(object sender, RoutedEventArgs e) => _ = CopyAsync(SelectedInOrder());

        private void SelectionDeleteButton_Click(object sender, RoutedEventArgs e) => _ = DeleteAsync(SelectedInOrder());

        #endregion

        #region Actions

        private void FillAlbumMenu(IList<MenuFlyoutItemBase> items, IReadOnlyList<PhotoItem> targets)
        {
            items.Add(MenuItem("Photos_NewAlbum", Symbol.Add, () => _ = CreateAlbumAsync(targets)));
            var albums = PhotoLibrary.Instance.Albums;
            if (albums.Count > 0)
                items.Add(new MenuFlyoutSeparator());
            foreach (var album in albums)
            {
                var a = album;
                items.Add(MenuItemText(a.Name, null, () =>
                {
                    var added = PhotoLibrary.Instance.AddToAlbum(a, targets);
                    _shell?.ShowToast(PhotosLocalization.Format("Photos_AddedToAlbum", added, a.Name), DaisyAlertVariant.Success);
                    ClearSelection();
                }, Icon("PhotosIconAlbum")));
            }
        }

        private async Task CreateAlbumAsync(IReadOnlyList<PhotoItem> targets)
        {
            if (_shell == null)
                return;
            var name = await _shell.ShowInputDialogAsync(L("Photos_NewAlbum"), L("Photos_AlbumNamePrompt"), L("Photos_AlbumDefaultName"), L("Photos_Create"));
            if (name == null)
                return;
            var album = PhotoLibrary.Instance.CreateAlbum(name, targets);
            _shell.ShowToast(PhotosLocalization.Format("Photos_AlbumCreated", album.Name), DaisyAlertVariant.Success);
            ClearSelection();
        }

        private void RemoveFromCurrentAlbum(IReadOnlyList<PhotoItem> targets)
        {
            if (PhotoLibrary.Instance.FindAlbum(_sourceKey) is not { } album)
                return;
            PhotoLibrary.Instance.RemoveFromAlbum(album, targets);
            ClearSelection();
        }

        private async Task CopyAsync(IReadOnlyList<PhotoItem> targets)
        {
            if (targets.Count == 0)
                return;
            var ok = await FileOperations.CopyFilesToClipboardAsync([.. targets.Select(t => t.Path)]);
            _shell?.ShowToast(ok ? PhotosLocalization.Format("Photos_CopiedCount", targets.Count) : L("Photos_ActionFailed"),
                ok ? DaisyAlertVariant.Success : DaisyAlertVariant.Error);
        }

        private async Task DeleteAsync(IReadOnlyList<PhotoItem> targets)
        {
            if (_shell == null || targets.Count == 0)
                return;
            if (!await _shell.ConfirmDeleteAsync(targets.Count))
                return;
            var deleted = await PhotoLibrary.Instance.DeleteAsync(targets);
            foreach (var t in targets)
                _selected.Remove(t);
            _shell.ShowToast(PhotosLocalization.Format("Photos_DeletedCount", deleted), deleted > 0 ? DaisyAlertVariant.Success : DaisyAlertVariant.Error);
            UpdateSelectionBar();
        }

        private async void SlideshowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_shell == null || _items.Count == 0)
                return;
            var list = _selected.Count > 1 ? SelectedInOrder() : _items;
            await _shell.StartSlideshowAsync(list, 0);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => _shell?.BackToCollections();

        public void OnReturnedFromViewer(PhotoItem? lastItem)
        {
            Reload(scrollToTop: false);
            if (lastItem == null)
                return;
            var row = FindRowOf(lastItem);
            if (row == null)
                return;
            if (row.Top < Scroller.VerticalOffset || row.Top + row.Height > Scroller.VerticalOffset + Scroller.ViewportHeight)
            {
                var offset = Math.Max(0, row.Top - Scroller.ViewportHeight / 3);
                Scroller.ChangeView(null, offset, null, true);
            }
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                foreach (var presenter in _realized)
                {
                    if (presenter.FindTile(lastItem) is { } tile)
                    {
                        tile.Focus(FocusState.Programmatic);
                        break;
                    }
                }
            });
        }

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (e.OriginalSource is TextBox)
                return;

            switch (e.Key)
            {
                case VirtualKey.A when ctrl:
                    SelectAll();
                    e.Handled = true;
                    break;
                case VirtualKey.C when ctrl && _selected.Count > 0:
                    _ = CopyAsync(SelectedInOrder());
                    e.Handled = true;
                    break;
                case VirtualKey.Escape when _selected.Count > 0:
                    ClearSelection();
                    e.Handled = true;
                    break;
                case VirtualKey.Delete when _selected.Count > 0:
                    _ = DeleteAsync(SelectedInOrder());
                    e.Handled = true;
                    break;
                case (VirtualKey)187 when ctrl:
                case VirtualKey.Add when ctrl:
                    ZoomTiles(1.12);
                    e.Handled = true;
                    break;
                case (VirtualKey)189 when ctrl:
                case VirtualKey.Subtract when ctrl:
                    ZoomTiles(1 / 1.12);
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Menus & options

        private void BuildMenus()
        {
            ImportMenu.Items.Clear();
            ImportMenu.Items.Add(MenuItem("Photos_AddFolder", Symbol.NewFolder, () => _ = _shell?.AddFolderInteractiveAsync()));
            ImportMenu.Items.Add(MenuItem("Photos_ImportFiles", Symbol.Import, () => _ = _shell?.ImportFilesInteractiveAsync()));
            ImportMenu.Items.Add(new MenuFlyoutSeparator());
            ImportMenu.Items.Add(MenuItem("Photos_Rescan", Symbol.Refresh, () => _ = PhotoLibrary.Instance.RescanAsync()));

            BuildFilterPanel();
            BuildViewOptionsPanel();
        }

        private void BuildFilterPanel()
        {
            FilterPanel.Children.Clear();
            FilterPanel.Children.Add(FlyoutCaption("Photos_FileType", new Thickness(0)));
            FilterPanel.Children.Add(Select(
                [
                    (PhotoTypeFilter.All, "Photos_Filter_AllTypes"),
                    (PhotoTypeFilter.Jpeg, "Photos_Filter_Jpeg"),
                    (PhotoTypeFilter.Png, "Photos_Filter_Png"),
                    (PhotoTypeFilter.Other, "Photos_Filter_OtherTypes")
                ],
                _typeFilter,
                v =>
                {
                    _typeFilter = v;
                    Reload(scrollToTop: true);
                },
                size: FlyoutSize));

            FilterPanel.Children.Add(FlyoutCaption("Photos_Orientation"));
            FilterPanel.Children.Add(Select(
                [
                    (PhotoOrientationFilter.Any, "Photos_Filter_AnyOrientation"),
                    (PhotoOrientationFilter.Landscape, "Photos_Filter_Landscape"),
                    (PhotoOrientationFilter.Portrait, "Photos_Filter_Portrait"),
                    (PhotoOrientationFilter.Square, "Photos_Filter_Square")
                ],
                _orientationFilter,
                v =>
                {
                    _orientationFilter = v;
                    Reload(scrollToTop: true);
                },
                size: FlyoutSize));

            var reset = new DaisyButton
            {
                Content = L("Photos_ResetFilters"),
                Variant = DaisyButtonVariant.Primary,
                Size = FlyoutSize,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0)
            };
            FlowerySizeManager.SetIgnoreGlobalSize(reset, true);
            reset.Click += (_, _) =>
            {
                _typeFilter = PhotoTypeFilter.All;
                _orientationFilter = PhotoOrientationFilter.Any;
                FilterFlyout.Hide();
                BuildFilterPanel();
                Reload(scrollToTop: true);
            };
            FilterPanel.Children.Add(reset);
        }

        private void BuildViewOptionsPanel()
        {
            ViewOptionsPanel.Children.Clear();
            ViewOptionsPanel.Children.Add(FlyoutCaption("Photos_SortBy", new Thickness(0)));
            ViewOptionsPanel.Children.Add(Select(
                [
                    (PhotoSortMode.DateTakenNewest, "Photos_Sort_DateNewest"),
                    (PhotoSortMode.DateTakenOldest, "Photos_Sort_DateOldest"),
                    (PhotoSortMode.DateModifiedNewest, "Photos_Sort_Modified"),
                    (PhotoSortMode.NameAscending, "Photos_Sort_NameAsc"),
                    (PhotoSortMode.NameDescending, "Photos_Sort_NameDesc"),
                    (PhotoSortMode.SizeLargest, "Photos_Sort_Size")
                ],
                PhotosSettings.SortMode,
                v =>
                {
                    PhotosSettings.SortMode = v;
                    PhotosSettings.SaveOptions();
                    Reload(scrollToTop: true);
                },
                size: FlyoutSize));

            ViewOptionsPanel.Children.Add(FlyoutCaption("Photos_GroupBy"));
            ViewOptionsPanel.Children.Add(Select(
                [
                    (PhotoGroupMode.Day, "Photos_Group_Day"),
                    (PhotoGroupMode.Month, "Photos_Group_Month"),
                    (PhotoGroupMode.Year, "Photos_Group_Year"),
                    (PhotoGroupMode.None, "Photos_Group_None")
                ],
                PhotosSettings.GroupMode,
                v =>
                {
                    PhotosSettings.GroupMode = v;
                    PhotosSettings.SaveOptions();
                    RebuildRows(keepPosition: true);
                },
                size: FlyoutSize));

            ViewOptionsPanel.Children.Add(FlyoutCaption("Photos_TileSize"));
            ViewOptionsPanel.Children.Add(Range(90, 420, PhotosSettings.TileHeight, 10, v =>
            {
                PhotosSettings.TileHeight = v;
                PhotosSettings.SaveOptions();
                RebuildRows(keepPosition: true);
            }));

            ViewOptionsPanel.Children.Add(ToggleRow("Photos_SquareTiles", PhotosSettings.SquareTiles, v =>
            {
                PhotosSettings.SquareTiles = v;
                PhotosSettings.SaveOptions();
                RebuildRows(keepPosition: true);
            }, FlyoutSize));
            ViewOptionsPanel.Children.Add(ToggleRow("Photos_ShowFileNames", PhotosSettings.ShowFileNames, v =>
            {
                PhotosSettings.ShowFileNames = v;
                PhotosSettings.SaveOptions();
                RefreshVisibleStates();
            }, FlyoutSize));
        }

        private void BuildCollectionMenu()
        {
            CollectionMenu.Items.Clear();
            if (_source == PhotoSourceKind.Album)
            {
                CollectionMenu.Items.Add(MenuItem("Photos_RenameAlbum", Symbol.Rename, () => _ = RenameAlbumAsync()));
                CollectionMenu.Items.Add(MenuItem("Photos_DeleteAlbum", Symbol.Delete, () => _ = DeleteAlbumAsync()));
            }
            else if (_source == PhotoSourceKind.Folder && _sourceKey != null)
            {
                var folder = _sourceKey;
                CollectionMenu.Items.Add(MenuItem("Photos_OpenInExplorer", Symbol.Folder, () => FileOperations.OpenFolder(folder)));
            }
        }

        private async Task RenameAlbumAsync()
        {
            if (_shell == null || PhotoLibrary.Instance.FindAlbum(_sourceKey) is not { } album)
                return;
            var name = await _shell.ShowInputDialogAsync(L("Photos_RenameAlbum"), L("Photos_AlbumNamePrompt"), album.Name, L("Photos_Rename"));
            if (name == null)
                return;
            if (!PhotoLibrary.Instance.RenameAlbum(album, name))
            {
                _shell.ShowToast(L("Photos_AlbumNameTaken"), DaisyAlertVariant.Warning);
                return;
            }
            ShowSource(PhotoSourceKind.Album, album.Name);
        }

        private async Task DeleteAlbumAsync()
        {
            if (_shell == null || PhotoLibrary.Instance.FindAlbum(_sourceKey) is not { } album)
                return;
            var result = await _shell.ShowDialogAsync(L("Photos_DeleteAlbum"), PhotosLocalization.Format("Photos_DeleteAlbumConfirm", album.Name), L("Photos_Delete"), null, primaryIsDanger: true);
            if (result != DialogResult.Primary)
                return;
            PhotoLibrary.Instance.DeleteAlbum(album);
            _shell.BackToCollections();
        }

        private void HeaderGrid_SizeChanged(object sender, SizeChangedEventArgs e) => AdaptHeader();

        private bool _adaptQueued;

        private void QueueAdaptHeader()
        {
            if (_adaptQueued)
                return;
            _adaptQueued = true;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _adaptQueued = false;
                AdaptHeader();
            });
        }

        /// <summary>
        /// Degrades the header step by step when space is short (count text, button labels, date chip,
        /// then title trimming) so nothing gets clipped. Also compacts the selection bar and empty-state actions.
        /// </summary>
        private void AdaptHeader()
        {
            // Measure against the parent: an overflowing grid is arranged at its desired (clipped) width.
            var available = RootGrid.ActualWidth - HeaderGrid.Padding.Left - HeaderGrid.Padding.Right - HeaderGrid.ColumnSpacing * 2;
            if (available > 0)
            {
                CountText.Visibility = Visibility.Visible;
                DateChip.Visibility = _dateChipAllowed ? Visibility.Visible : Visibility.Collapsed;
                FilterButton.Content = L("Photos_Filter");
                ImportButton.Content = L("Photos_Import");
                TitleText.MaxWidth = 520;

                Action[] degrade =
                [
                    () => CountText.Visibility = Visibility.Collapsed,
                    () => FilterButton.Content = null,
                    () => ImportButton.Content = null,
                    () => DateChip.Visibility = Visibility.Collapsed
                ];
                foreach (var step in degrade)
                {
                    if (HeaderFits(available))
                        break;
                    step();
                }

                if (!HeaderFits(available))
                {
                    var others = HeaderLeft.DesiredSize.Width - TitleText.DesiredSize.Width;
                    TitleText.MaxWidth = Math.Max(60, available - HeaderRight.DesiredSize.Width - others);
                }
            }

            AdaptSelectionBar();
            AdaptEmptyActions();
        }

        private bool HeaderFits(double available)
        {
            var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
            HeaderLeft.Measure(infinite);
            HeaderRight.Measure(infinite);
            return HeaderLeft.DesiredSize.Width + HeaderRight.DesiredSize.Width <= available;
        }

        private void AdaptSelectionBar()
        {
            if (SelectionBar.Visibility != Visibility.Visible || SelectionBar.ActualWidth <= 0)
                return;

            var allFavorite = _selected.Count > 0 && _selected.All(i => i.IsFavorite);
            (DaisyButton Button, string Text)[] buttons =
            [
                (SelectAllButton, L("Photos_SelectAll")),
                (SelectionFavoriteButton, L(allFavorite ? "Photos_Unfavorite" : "Photos_Favorite")),
                (SelectionAlbumButton, L("Photos_AddToAlbum")),
                (SelectionRemoveFromAlbumButton, L("Photos_RemoveFromAlbum")),
                (SelectionCopyButton, L("Photos_Copy")),
                (SelectionDeleteButton, L("Photos_Delete"))
            ];
            foreach (var (button, text) in buttons)
            {
                button.Content = text;
                Tip(button, text);
            }

            var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
            SelectionActions.Measure(infinite);
            ClearSelectionButton.Measure(infinite);
            SelectionText.Measure(infinite);
            var available = RootGrid.ActualWidth - SelectionBar.Margin.Left - SelectionBar.Margin.Right - SelectionBar.Padding.Left - SelectionBar.Padding.Right - 16;
            var needed = SelectionActions.DesiredSize.Width + ClearSelectionButton.DesiredSize.Width + SelectionText.DesiredSize.Width + 12;
            if (needed > available)
            {
                foreach (var (button, _) in buttons)
                    button.Content = null;
            }
        }

        private void AdaptEmptyActions()
        {
            if (EmptyState.Visibility != Visibility.Visible)
                return;
            EmptyActions.Orientation = Orientation.Horizontal;
            EmptyActions.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var available = RootGrid.ActualWidth - 48;
            if (available > 0 && EmptyActions.DesiredSize.Width > available)
                EmptyActions.Orientation = Orientation.Vertical;
        }

        private void UpdateTooltips()
        {
            Tip(SlideshowButton, L("Photos_Slideshow"));
            Tip(ViewOptionsButton, L("Photos_ViewOptions"));
            Tip(BackButton, L("Photos_Back"));
            Tip(CollectionMenuButton, L("Photos_More"));
            Tip(ClearSelectionButton, L("Photos_ClearSelection"));
            Tip(FilterButton, L("Photos_Filter"));
            Tip(ImportButton, L("Photos_Import"));
        }

        private void OnLanguageChanged()
        {
            BuildMenus();
            BuildCollectionMenu();
            UpdateTooltips();
            UpdateTitle();
            CountText.Text = PhotosLocalization.Format(_items.Count == 1 ? "Photos_ItemCountOne" : "Photos_ItemCount", _items.Count);
            UpdateSelectionBar();
            UpdateEmptyState();
            RebuildRows(keepPosition: true);
            AdaptHeader();
        }

        #endregion

        #region Empty state & progress

        private void OnScanProgress(object? sender, (int Done, int Total) progress)
        {
            if (progress.Total <= 0)
                return;
            ScanProgressBar.Maximum = progress.Total;
            ScanProgressBar.Value = progress.Done;
        }

        private void UpdateEmptyState()
        {
            var empty = _items.Count == 0;
            EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            Scroller.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            if (!empty)
                return;

            EmptyActions.Children.Clear();
            var library = PhotoLibrary.Instance;
            var filtered = !string.IsNullOrWhiteSpace(_search) || _typeFilter != PhotoTypeFilter.All || _orientationFilter != PhotoOrientationFilter.Any;

            if (library.IsScanning && library.Items.Count == 0)
            {
                EmptyTitle.Text = L("Photos_Empty_ScanningTitle");
                EmptyText.Text = L("Photos_Empty_ScanningText");
                return;
            }

            if (filtered)
            {
                EmptyTitle.Text = L("Photos_Empty_NoResultsTitle");
                EmptyText.Text = L("Photos_Empty_NoResultsText");
                var clear = new DaisyButton { Content = L("Photos_ResetFilters"), Variant = DaisyButtonVariant.Primary, ButtonStyle = DaisyButtonStyle.Soft };
                clear.Click += (_, _) =>
                {
                    _typeFilter = PhotoTypeFilter.All;
                    _orientationFilter = PhotoOrientationFilter.Any;
                    _shell?.ClearSearch();
                    _search = string.Empty;
                    BuildFilterPanel();
                    Reload(scrollToTop: true);
                };
                EmptyActions.Children.Add(clear);
                return;
            }

            switch (_source)
            {
                case PhotoSourceKind.Favorites:
                    EmptyTitle.Text = L("Photos_Empty_FavoritesTitle");
                    EmptyText.Text = L("Photos_Empty_FavoritesText");
                    return;
                case PhotoSourceKind.Album:
                    EmptyTitle.Text = L("Photos_Empty_AlbumTitle");
                    EmptyText.Text = L("Photos_Empty_AlbumText");
                    return;
                case PhotoSourceKind.Folder:
                    EmptyTitle.Text = L("Photos_Empty_FolderTitle");
                    EmptyText.Text = string.Empty;
                    return;
            }

            EmptyTitle.Text = L("Photos_Empty_LibraryTitle");
            EmptyText.Text = L("Photos_Empty_LibraryText");
            var addFolder = new DaisyButton { Content = L("Photos_AddFolder"), Variant = DaisyButtonVariant.Primary, IconSymbol = Symbol.NewFolder };
            addFolder.Click += (_, _) => _ = _shell?.AddFolderInteractiveAsync();
            EmptyActions.Children.Add(addFolder);

            if (System.IO.Directory.Exists(PhotoLibrary.SamplesFolder))
            {
                var samples = new DaisyButton { Content = L("Photos_UseSamples"), Variant = DaisyButtonVariant.Primary, IconSymbol = Symbol.Pictures };
                samples.Click += (_, _) => PhotoLibrary.Instance.AddFolder(PhotoLibrary.SamplesFolder);
                EmptyActions.Children.Add(samples);
            }
            AdaptEmptyActions();
        }

        #endregion
    }
}
