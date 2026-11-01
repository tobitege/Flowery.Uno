using System.Collections.Immutable;
using Flowery.Uno.Photos.Editing;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Services.Platform;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Windows.System;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Full-window photo viewer. Skia: the editing part (panel tabs, save, compare, crop and markup) arrives with the
    /// editing phase; its controls are hidden until then.
    /// Core: lifecycle, navigation, toolbar actions, slideshow and keyboard handling.
    /// </summary>
    public sealed partial class ViewerView : UserControl
    {
        private readonly EditHistory _history = new();
        private PhotosShell? _shell;
        private List<PhotoItem> _items = [];
        private int _index;
        private LoadedImage? _image;
        private int _loadVersion;
        private bool _slideshow;
        private DispatcherQueueTimer? _slideshowTimer;
        private bool _wasFullScreenBeforeSlideshow;
        private bool _syncingZoom;

        public ViewerView()
        {
            InitializeComponent();
            _history.Changed += OnHistoryChanged;
            ZoomRange.RegisterPropertyChangedCallback(DaisyRange.ValueProperty, (_, _) => OnZoomRangeChanged());
            PhotosLocalization.Instance.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(OnLanguageChanged);
        }

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        public bool IsOpen { get; private set; }

        public PhotoItem? Current => _index >= 0 && _index < _items.Count ? _items[_index] : null;

        private EditState State => _history.Current;

        /// <summary>Raised when the viewer closes, with the last shown item.</summary>
        public event EventHandler<PhotoItem?>? Closed;

        public void Initialize(PhotosShell shell)
        {
            _shell = shell;
            BuildEditPanel();
            BuildMenus();
            BuildViewPanel();
            UpdateTooltips();
            ApplyChromeLayout();
        }

        public void Shutdown()
        {
            StopSlideshow();
            DisposeImage();
        }

        #region Open / close / navigation

        public async Task OpenAsync(IReadOnlyList<PhotoItem> items, int index, bool edit)
        {
            IsOpen = true;
            _items = [.. items];
            // Skia: no editing yet, so the side panel starts closed whatever "edit" asks for.
            SetPanelOpen(false);
            Focus(FocusState.Programmatic);
            await LoadIndexAsync(index, checkUnsaved: false);
        }

        public async Task RequestCloseAsync()
        {
            if (!IsOpen)
                return;
            if (!await ConfirmDiscardAsync())
                return;

            StopSlideshow();
            if (_immersive)
                SetImmersive(false);
            IsOpen = false;
            var last = Current;
            _history.Reset();
            DisposeImage();
            Canvas.Invalidate();
            Closed?.Invoke(this, last);
        }

        // Skia: there is nothing to discard until the editing phase (the edit state never changes).
        private Task<bool> ConfirmDiscardAsync() => Task.FromResult(true);

        private async Task NavigateAsync(int delta, bool wrap = false)
        {
            if (_items.Count == 0)
                return;
            var target = _index + delta;
            if (wrap)
                target = ((target % _items.Count) + _items.Count) % _items.Count;
            if (target < 0 || target >= _items.Count || target == _index)
                return;
            await LoadIndexAsync(target, checkUnsaved: true);
        }

        private async Task LoadIndexAsync(int index, bool checkUnsaved)
        {
            if (checkUnsaved && !await ConfirmDiscardAsync())
                return;

            _index = Math.Clamp(index, 0, Math.Max(0, _items.Count - 1));
            var item = Current;
            _history.Reset();
            ResetView();
            UpdateItemChrome();
            if (item == null)
                return;

            var version = ++_loadVersion;
            ErrorPanel.Visibility = Visibility.Collapsed;
            LoadingIndicator.Visibility = Visibility.Visible;

            try
            {
                var loaded = await Task.Run(() => LoadedImage.Load(item.Path));
                if (version != _loadVersion)
                {
                    loaded.Dispose();
                    return;
                }

                DisposeImage();
                _image = loaded;
            }
            catch (Exception ex)
            {
                if (version != _loadVersion)
                    return;
                DisposeImage();
                ErrorText.Text = PhotosLocalization.Format("Photos_LoadFailed", item.FileName, ex.Message);
                ErrorPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                if (version == _loadVersion)
                    LoadingIndicator.Visibility = Visibility.Collapsed;
            }

            UpdateItemChrome();
            UpdateZoomUi();
            Canvas.Invalidate();
            OnImageLoadedForPanel();
        }

        private void DisposeImage()
        {
            _image?.Dispose();
            _image = null;
        }

        private void UpdateItemChrome()
        {
            var item = Current;
            _shell?.SetViewerTitle(item?.FileName);
            PositionText.Text = _items.Count > 1 ? PhotosLocalization.Format("Photos_PositionFormat", _index + 1, _items.Count) : string.Empty;
            PrevButton.IsEnabled = _index > 0;
            NextButton.IsEnabled = _index < _items.Count - 1;

            if (item == null)
                return;

            FormatBadge.BadgeContent = item.Extension;
            var w = _image?.Width ?? Math.Max(0, item.PixelWidth);
            var h = _image?.Height ?? Math.Max(0, item.PixelHeight);
            if (State.HasGeometry && _image != null)
            {
                var output = EditRenderer.GetOutputSize(new Size(w, h), State);
                DimensionsText.Text = $"{w} × {h} → {output.Width:0} × {output.Height:0}";
            }
            else
            {
                DimensionsText.Text = w > 0 ? $"{w} × {h}" : string.Empty;
            }
            FileSizeText.Text = FileOperations.FormatFileSize(item.FileSize);
            UpdateFavoriteButton();
        }

        private void UpdateFavoriteButton()
        {
            var favorite = Current?.IsFavorite == true;
            FavoriteButton.IconData = Icon(favorite ? "DaisyIconHeart" : "PhotosIconHeartOutline");
            FavoriteButton.Variant = favorite ? DaisyButtonVariant.Error : DaisyButtonVariant.Ghost;
            FavoriteButton.ButtonStyle = favorite ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;
            Tip(FavoriteButton, L(favorite ? "Photos_Unfavorite" : "Photos_Favorite"));
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            var changed = State.HasChanges;
            UndoButton.IsEnabled = _history.CanUndo;
            RedoButton.IsEnabled = _history.CanRedo;
            RevertButton.IsEnabled = changed;
            SaveGroup.Visibility = changed ? Visibility.Visible : Visibility.Collapsed;
            EditedBadge.Visibility = changed ? Visibility.Visible : Visibility.Collapsed;
            EditedBadge.BadgeContent = L("Photos_Edited");
            UpdateItemChrome();
            Canvas.Invalidate();
        }

        private void ApplyState(EditState state, string? coalesceKey = null)
        {
            _history.Apply(state, coalesceKey);
        }

        #endregion

        #region Toolbar actions

        private async void PrevButton_Click(object sender, RoutedEventArgs e) => await NavigateAsync(-1);

        private async void NextButton_Click(object sender, RoutedEventArgs e) => await NavigateAsync(1);

        private void FavoriteButton_Click(object sender, RoutedEventArgs e) => ToggleFavorite();

        private void ToggleFavorite()
        {
            if (Current is not { } item)
                return;
            PhotoLibrary.Instance.ToggleFavorite(item);
            UpdateFavoriteButton();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e) => await DeleteCurrentAsync();

        private async Task DeleteCurrentAsync()
        {
            if (Current is not { } item || _shell == null)
                return;
            if (!await _shell.ConfirmDeleteAsync(1))
                return;

            var ok = PhotoLibrary.Instance.FindByPath(item.Path) != null
                ? await PhotoLibrary.Instance.DeleteAsync([item]) > 0
                : FileOperations.MoveToRecycleBin(item.Path);

            if (!ok)
            {
                _shell.ShowToast(L("Photos_ActionFailed"), DaisyAlertVariant.Error);
                return;
            }

            _shell.ShowToast(PhotosLocalization.Format("Photos_DeletedCount", 1), DaisyAlertVariant.Success);
            _items.RemoveAt(_index);
            _history.Reset();
            if (_items.Count == 0)
            {
                await RequestCloseAsync();
                return;
            }
            await LoadIndexAsync(Math.Min(_index, _items.Count - 1), checkUnsaved: false);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_panelOpen && _tab == EditTab.Info)
                SetPanelOpen(false);
            else
                SetPanelOpen(true, EditTab.Info);
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e) => await CopyAsync();

        private async Task CopyAsync()
        {
            if (Current is not { } item || _shell == null)
                return;

            // Skia: copying the edited image (RenderFinal) arrives with the editing phase.
            var ok = await FileOperations.CopyFilesToClipboardAsync([item.Path]);

            _shell.ShowToast(L(ok ? "Photos_CopiedImage" : "Photos_ActionFailed"), ok ? DaisyAlertVariant.Success : DaisyAlertVariant.Error);
        }

        private void SlideshowButton_Click(object sender, RoutedEventArgs e) => StartSlideshow();

        private void FullScreenButton_Click(object sender, RoutedEventArgs e) => ToggleImmersive();

        private void UpdateFullScreenButton()
        {
            var full = _immersive;
            FullScreenButton.IconSymbol = full ? Symbol.BackToWindow : Symbol.FullScreen;
            Tip(FullScreenButton, L(full ? "Photos_ExitFullScreen" : "Photos_FullScreen"));
        }

        private void MoreMenu_Opening(object sender, object e) => BuildMoreMenu();

        private void BuildMoreMenu()
        {
            MoreMenu.Items.Clear();
            if (Current is not { } item)
                return;

            var albumMenu = new MenuFlyoutSubItem { Text = L("Photos_AddToAlbum"), Icon = new SymbolIcon(Symbol.Library) };
            albumMenu.Items.Add(MenuItem("Photos_NewAlbum", Symbol.Add, async () =>
            {
                if (_shell == null)
                    return;
                var name = await _shell.ShowInputDialogAsync(L("Photos_NewAlbum"), L("Photos_AlbumNamePrompt"), L("Photos_AlbumDefaultName"), L("Photos_Create"));
                if (name == null)
                    return;
                var album = PhotoLibrary.Instance.CreateAlbum(name, [item]);
                _shell.ShowToast(PhotosLocalization.Format("Photos_AlbumCreated", album.Name), DaisyAlertVariant.Success);
            }));
            if (PhotoLibrary.Instance.Albums.Count > 0)
                albumMenu.Items.Add(new MenuFlyoutSeparator());
            foreach (var album in PhotoLibrary.Instance.Albums)
            {
                var a = album;
                albumMenu.Items.Add(MenuItemText(a.Name, null, () =>
                {
                    var added = PhotoLibrary.Instance.AddToAlbum(a, [item]);
                    _shell?.ShowToast(PhotosLocalization.Format("Photos_AddedToAlbum", added, a.Name), DaisyAlertVariant.Success);
                }, Icon("PhotosIconAlbum")));
            }
            MoreMenu.Items.Add(albumMenu);
            MoreMenu.Items.Add(new MenuFlyoutSeparator());

            var platform = PlatformServices.Current;
            if (platform.Features.HasFlag(PlatformFeatures.OpenWithChooser) || platform.Features.HasFlag(PlatformFeatures.OpenWithList))
                MoreMenu.Items.Add(MenuItem("Photos_OpenWith", Symbol.OpenWith, () => _ = FileOperations.OpenWithAsync(item.Path)));
            if (platform.Features.HasFlag(PlatformFeatures.RevealInFileManager))
                MoreMenu.Items.Add(MenuItem("Photos_ShowInExplorer", Symbol.Folder, () => FileOperations.ShowInExplorer(item.Path)));
            MoreMenu.Items.Add(MenuItem("Photos_CopyPath", Symbol.Link, () =>
            {
                var package = new DataPackage();
                package.SetText(item.Path);
                Clipboard.SetContent(package);
                _shell?.ShowToast(L("Photos_PathCopied"), DaisyAlertVariant.Success);
            }));
            if (platform.Features.HasFlag(PlatformFeatures.SetWallpaper))
                MoreMenu.Items.Add(MenuItem("Photos_SetAsBackground", Symbol.SetLockScreen, () =>
                {
                    var ok = FileOperations.SetAsWallpaper(item.Path);
                    _shell?.ShowToast(L(ok ? "Photos_BackgroundSet" : "Photos_ActionFailed"), ok ? DaisyAlertVariant.Success : DaisyAlertVariant.Error);
                }));
        }

        private void BuildMenus()
        {
            ZoomMenu.Items.Clear();
            ZoomMenu.Items.Add(MenuItem("Photos_ZoomFit", null, ZoomToFit, Icon("PhotosIconFitScreen")));
            foreach (var percent in new[] { 25, 50, 100, 200, 400, 800 })
            {
                var p = percent;
                ZoomMenu.Items.Add(MenuItemText($"{p} %", null, () => SetZoomPercent(p)));
            }
        }

        private void UpdateTooltips()
        {
            Tip(DeleteButton, L("Photos_Delete"));
            Tip(InfoButton, L("Photos_Info"));
            Tip(CopyButton, L("Photos_Copy"));
            Tip(SlideshowButton, L("Photos_Slideshow"));
            Tip(MoreButton, L("Photos_More"));
            Tip(ViewButton, L("Photos_View"));
            Tip(PrevButton, L("Photos_Previous"));
            Tip(NextButton, L("Photos_Next"));
            Tip(FitButton, L("Photos_ZoomFit"));
            Tip(ZoomInButton, L("Photos_ZoomIn"));
            Tip(ZoomOutButton, L("Photos_ZoomOut"));
            UpdateFullScreenButton();
            UpdateFavoriteButton();
        }

        private void OnLanguageChanged()
        {
            BuildMenus();
            UpdateTooltips();
            BuildEditPanel();
            BuildViewPanel();
            UpdateItemChrome();
            EditedBadge.BadgeContent = L("Photos_Edited");
            if (_panelOpen && _tab == EditTab.Info)
                _ = RefreshInfoAsync();
        }

        private void CanvasHost_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_slideshow || _items.Count <= 1)
                return;
            PrevButton.Opacity = 1;
            NextButton.Opacity = 1;
        }

        private void CanvasHost_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _lastPointer = null;
            if (PhotosSettings.ShowRulers)
                Canvas.Invalidate();
            PrevButton.Opacity = 0;
            NextButton.Opacity = 0;
        }

        #endregion

        #region Slideshow

        public void StartSlideshow()
        {
            if (_items.Count == 0 || _shell == null)
                return;

            _immersiveBeforeSlideshow = _immersive;
            if (_immersive)
                SetImmersive(false);
            _slideshow = true;
            SetPanelOpen(false);
            Toolbar.Visibility = Visibility.Collapsed;
            StatusBar.Visibility = Visibility.Collapsed;
            PrevButton.Opacity = 0;
            NextButton.Opacity = 0;
            CanvasHost.Margin = new Thickness(0);
            CanvasHost.CornerRadius = new CornerRadius(0);
            CanvasHost.Background = new SolidColorBrush(Colors.Black);
            _shell.SetTitleBarVisible(false);
            _wasFullScreenBeforeSlideshow = _shell.IsFullScreen;
            _shell.SetFullScreen(true);
            ZoomToFit();

            SlideshowHint.Visibility = Visibility.Visible;
            UiTimers.RunOnce(DispatcherQueue, TimeSpan.FromSeconds(2.5), () => SlideshowHint.Visibility = Visibility.Collapsed);

            _slideshowTimer ??= CreateSlideshowTimer();
            _slideshowTimer.Interval = TimeSpan.FromSeconds(PhotosSettings.SlideshowSeconds);
            _slideshowTimer.Start();
            Focus(FocusState.Programmatic);
        }

        private DispatcherQueueTimer CreateSlideshowTimer()
        {
            var timer = DispatcherQueue.CreateTimer();
            timer.IsRepeating = true;
            timer.Tick += async (_, _) => await AdvanceSlideshowAsync();
            return timer;
        }

        private async Task AdvanceSlideshowAsync()
        {
            if (!_slideshow || _items.Count < 2)
                return;
            await FadeCanvasAsync(0, 220);
            await NavigateAsync(1, wrap: true);
            await FadeCanvasAsync(1, 320);
        }

        private Task FadeCanvasAsync(double to, int milliseconds)
        {
            var tcs = new TaskCompletionSource();
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(milliseconds)),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(animation, Canvas);
            Storyboard.SetTargetProperty(animation, "Opacity");
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Completed += (_, _) =>
            {
                Canvas.Opacity = to;
                tcs.TrySetResult();
            };
            storyboard.Begin();
            return tcs.Task;
        }

        public void StopSlideshow()
        {
            if (!_slideshow)
                return;
            _slideshow = false;
            _slideshowTimer?.Stop();
            Canvas.Opacity = 1;
            Toolbar.Visibility = Visibility.Visible;
            SlideshowHint.Visibility = Visibility.Collapsed;
            CanvasHost.Margin = new Thickness(8, 0, 8, 0);
            CanvasHost.CornerRadius = new CornerRadius(12);
            CanvasHost.Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            _shell?.SetTitleBarVisible(true);
            if (!_wasFullScreenBeforeSlideshow)
                _shell?.SetFullScreen(false);
            if (_immersiveBeforeSlideshow)
                SetImmersive(true);
            else
                ApplyChromeLayout();
            UpdateFullScreenButton();
            Canvas.Invalidate();
        }

        #endregion

        #region Keyboard

        private static bool IsDown(VirtualKey key) =>
            InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        private async void ViewerView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled || !IsOpen)
                return;
            if (e.OriginalSource is TextBox)
                return;

            var ctrl = IsDown(VirtualKey.Control);
            var fromSlider = e.OriginalSource is DaisyRange;

            if (_slideshow)
            {
                switch (e.Key)
                {
                    case VirtualKey.Escape:
                    case VirtualKey.F5:
                        StopSlideshow();
                        break;
                    case VirtualKey.Left:
                        await NavigateAsync(-1, wrap: true);
                        break;
                    case VirtualKey.Right:
                    case VirtualKey.Space:
                        await NavigateAsync(1, wrap: true);
                        break;
                }
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case VirtualKey.Left when !fromSlider:
                case VirtualKey.PageUp:
                    e.Handled = true;
                    await NavigateAsync(-1);
                    break;
                case VirtualKey.Right when !fromSlider:
                case VirtualKey.PageDown:
                    e.Handled = true;
                    await NavigateAsync(1);
                    break;
                case VirtualKey.Home when !fromSlider:
                    e.Handled = true;
                    await LoadIndexAsync(0, checkUnsaved: true);
                    break;
                case VirtualKey.End when !fromSlider:
                    e.Handled = true;
                    await LoadIndexAsync(_items.Count - 1, checkUnsaved: true);
                    break;
                case VirtualKey.Escape:
                    e.Handled = true;
                    if (_immersive)
                        SetImmersive(false);
                    else if (_panelOpen)
                        SetPanelOpen(false);
                    else
                        await RequestCloseAsync();
                    break;
                case VirtualKey.C when ctrl:
                    e.Handled = true;
                    await CopyAsync();
                    break;
                case VirtualKey.Add:
                case (VirtualKey)187:
                    e.Handled = true;
                    StepZoom(1);
                    break;
                case VirtualKey.Subtract:
                case (VirtualKey)189:
                    e.Handled = true;
                    StepZoom(-1);
                    break;
                case VirtualKey.Number0:
                case VirtualKey.NumberPad0:
                    e.Handled = true;
                    ZoomToFit();
                    break;
                case VirtualKey.Number1:
                case VirtualKey.NumberPad1:
                    e.Handled = true;
                    SetZoomPercent(100);
                    break;
                case VirtualKey.F when !ctrl:
                    e.Handled = true;
                    ToggleFavorite();
                    break;
                case VirtualKey.I when !ctrl:
                    e.Handled = true;
                    InfoButton_Click(this, new RoutedEventArgs());
                    break;
                case VirtualKey.Delete:
                    e.Handled = true;
                    await DeleteCurrentAsync();
                    break;
                case VirtualKey.F5:
                    e.Handled = true;
                    StartSlideshow();
                    break;
            }
        }

        #endregion
    }
}
