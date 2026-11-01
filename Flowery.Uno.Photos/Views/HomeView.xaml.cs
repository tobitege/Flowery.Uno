using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Xaml.Automation;
using Windows.System;
using static Flowery.Uno.Photos.Views.UiHelpers;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Start page: greeting with library summary and quick actions, library statistics, recent photos,
    /// favorites, folders and albums. An empty library shows a welcome card with the first steps instead.
    /// </summary>
    public sealed partial class HomeView : UserControl, ICollectionCardHost
    {
        private const double ThumbGap = 12;
        private const double ThumbMinSide = 150;
        private const double CardGap = 18;
        private const double CardMinWidth = 210;
        private const double CardHeight = 236;
        private const double NarrowWidth = 560;
        private const double ArtMinWidth = 760;

        private PhotosShell? _shell;
        private CancellationTokenSource? _thumbCts;
        private double _lastWidth;
        private bool _refreshQueued;

        public HomeView()
        {
            InitializeComponent();
            PhotosLocalization.Instance.PropertyChanged += (_, _) => QueueRefresh();
            FlowerySizeManager.SizeChanged += (_, _) => QueueRefresh();
            Root.SizeChanged += (_, e) =>
            {
                if (Math.Abs(e.NewSize.Width - _lastWidth) < 1)
                    return;
                _lastWidth = e.NewSize.Width;
                QueueRefresh();
            };
        }

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        public void Initialize(PhotosShell shell)
        {
            _shell = shell;
            PhotoLibrary.Instance.ScanningChanged += (_, _) => QueueRefresh();
        }

        /// <summary>Coalesces resize, size and language changes into one rebuild.</summary>
        private void QueueRefresh()
        {
            if (_refreshQueued)
                return;
            _refreshQueued = true;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _refreshQueued = false;
                Refresh();
            });
        }

        public void Refresh()
        {
            if (_shell == null || Visibility != Visibility.Visible)
                return;

            var library = PhotoLibrary.Instance;
            var width = Root.ActualWidth - Root.Padding.Left - Root.Padding.Right;
            if (width <= 0)
                width = 900;

            var hasPhotos = library.Items.Count > 0;
            UpdateHero(library, hasPhotos, width);
            BuildStats(library, hasPhotos, width);
            BuildSections(library, hasPhotos, width);
        }

        #region Hero

        private void UpdateHero(PhotoLibrary library, bool hasPhotos, double width)
        {
            HeroActions.Children.Clear();
            HeroActions.Orientation = width < NarrowWidth ? Orientation.Vertical : Orientation.Horizontal;
            HeroArt.Visibility = width >= ArtMinWidth ? Visibility.Visible : Visibility.Collapsed;

            if (hasPhotos)
            {
                GreetingText.Text = L(DateTime.Now.Hour switch
                {
                    >= 5 and < 12 => "Photos_Home_GreetingMorning",
                    >= 12 and < 18 => "Photos_Home_GreetingAfternoon",
                    _ => "Photos_Home_GreetingEvening"
                });
                SummaryText.Text = PhotosLocalization.Format("Photos_Settings_LibraryStats", library.Items.Count, library.Folders.Count,
                    FileOperations.FormatFileSize(library.Items.Sum(i => i.FileSize)));

                AddHeroAction(L("Photos_Home_StartSlideshow"), DaisyButtonVariant.Primary, null, Icon("PhotosIconSlideshow"), () =>
                    _ = _shell?.StartSlideshowAsync(SortedByDate(library.Items), 0));
                AddHeroAction(L("Photos_AddFolder"), DaisyButtonVariant.Primary, Symbol.NewFolder, null, () => _ = _shell?.AddFolderInteractiveAsync());
                AddHeroAction(L("Photos_ImportFiles"), DaisyButtonVariant.Primary, Symbol.Import, null, () => _ = _shell?.ImportFilesInteractiveAsync());
                return;
            }

            if (library.IsScanning)
            {
                GreetingText.Text = L("Photos_Empty_ScanningTitle");
                SummaryText.Text = L("Photos_Empty_ScanningText");
                return;
            }

            GreetingText.Text = L("Photos_Home_WelcomeTitle");
            SummaryText.Text = L("Photos_Home_WelcomeText");
            AddHeroAction(L("Photos_AddFolder"), DaisyButtonVariant.Primary, Symbol.NewFolder, null, () => _ = _shell?.AddFolderInteractiveAsync());
            AddHeroAction(L("Photos_ImportFiles"), DaisyButtonVariant.Primary, Symbol.Import, null, () => _ = _shell?.ImportFilesInteractiveAsync());
            if (System.IO.Directory.Exists(PhotoLibrary.SamplesFolder))
            {
                AddHeroAction(L("Photos_UseSamples"), DaisyButtonVariant.Primary, Symbol.Pictures, null, () =>
                    PhotoLibrary.Instance.AddFolder(PhotoLibrary.SamplesFolder));
            }
        }

        private void AddHeroAction(string text, DaisyButtonVariant variant, Symbol? symbol, string? iconData, Action onClick)
        {
            var button = new DaisyButton
            {
                Content = text,
                Variant = variant,
                HorizontalAlignment = HeroActions.Orientation == Orientation.Vertical ? HorizontalAlignment.Stretch : HorizontalAlignment.Left
            };
            if (symbol is { } s)
                button.IconSymbol = s;
            else if (iconData != null)
                button.IconData = iconData;
            button.Click += (_, _) => onClick();
            HeroActions.Children.Add(button);
        }

        private void Hero_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var w = e.NewSize.Width;
            var h = e.NewSize.Height;
            Hero.Clip = new RectangleGeometry { Rect = new Rect(0, 0, w, h) };

            // Primary blob crosses the right edge, secondary blob the bottom edge; both stay clear of the corners.
            var primarySize = Math.Max(80, Math.Min(190, h - 48));
            BlobPrimary.Width = BlobPrimary.Height = primarySize;
            Canvas.SetLeft(BlobPrimary, w - primarySize * 0.55);
            Canvas.SetTop(BlobPrimary, (h - primarySize) / 2);

            var secondarySize = Math.Min(150, Math.Max(60, w * 0.18));
            BlobSecondary.Width = BlobSecondary.Height = secondarySize;
            Canvas.SetLeft(BlobSecondary, w * 0.42);
            Canvas.SetTop(BlobSecondary, h - secondarySize * 0.45);
        }

        #endregion

        #region Statistics

        private void BuildStats(PhotoLibrary library, bool hasPhotos, double width)
        {
            StatsGrid.Children.Clear();
            StatsGrid.ColumnDefinitions.Clear();
            StatsGrid.RowDefinitions.Clear();
            StatsGrid.Visibility = hasPhotos ? Visibility.Visible : Visibility.Collapsed;
            if (!hasPhotos)
                return;

            var stats = new (string Icon, string Brush, int Value, string Label, PhotosPage Page)[]
            {
                ("PhotosIconGallery", "DaisyPrimaryBrush", library.Items.Count, L("Photos_Home_Photos"), PhotosPage.Gallery),
                ("PhotosIconHeart", "DaisyErrorBrush", library.Items.Count(i => i.IsFavorite), L("Photos_Nav_Favorites"), PhotosPage.Favorites),
                ("PhotosIconAlbum", "DaisyAccentBrush", library.Albums.Count, L("Photos_Nav_Albums"), PhotosPage.Albums),
                ("DaisyIconFolder", "DaisyInfoBrush", library.Folders.Count, L("Photos_Nav_Folders"), PhotosPage.Folders)
            };

            var columns = width >= 760 ? 4 : 2;
            for (var c = 0; c < columns; c++)
                StatsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var r = 0; r < (stats.Length + columns - 1) / columns; r++)
                StatsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (var i = 0; i < stats.Length; i++)
            {
                var (icon, brush, value, label, page) = stats[i];
                var tile = CreateStatTile(icon, brush, value, label, page);
                Grid.SetColumn(tile, i % columns);
                Grid.SetRow(tile, i / columns);
                StatsGrid.Children.Add(tile);
            }
        }

        private ContentControl CreateStatTile(string iconKey, string brushKey, int value, string label, PhotosPage page)
        {
            var accent = DaisyResourceLookup.GetBrush(brushKey);
            var f = SizeFactor;

            var badge = new Grid { Width = 44 * f, Height = 44 * f, VerticalAlignment = VerticalAlignment.Center };
            badge.Children.Add(new Ellipse { Fill = accent, Opacity = 0.16 });
            badge.Children.Add(new Viewbox { Margin = new Thickness(11 * f), Child = new Path { Data = Geo(iconKey), Fill = accent } });

            var number = Responsive(new TextBlock
            {
                Text = value.ToString("N0", PhotosLocalization.CurrentCulture),
                FontWeight = FontWeights.SemiBold,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            }, ResponsiveFontTier.Header);
            var caption = Responsive(new TextBlock
            {
                Text = label,
                Opacity = 0.7,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            }, ResponsiveFontTier.Secondary);
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(number);
            text.Children.Add(caption);

            var grid = new Grid { ColumnSpacing = 14 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(text, 1);
            grid.Children.Add(badge);
            grid.Children.Add(text);

            return CreateCard(grid, new Thickness(16), 14, () => _shell?.Navigate(page), $"{label}: {number.Text}");
        }

        #endregion

        #region Sections

        private void BuildSections(PhotoLibrary library, bool hasPhotos, double width)
        {
            _thumbCts?.Cancel();
            Sections.Children.Clear();
            Sections.Visibility = hasPhotos || !library.IsScanning ? Visibility.Visible : Visibility.Collapsed;
            if (!hasPhotos)
            {
                if (!library.IsScanning)
                    Sections.Children.Add(CreateFeatureRow(width));
                return;
            }

            var cts = new CancellationTokenSource();
            _thumbCts = cts;

            var thumbColumns = Math.Max(3, (int)Math.Floor((width + ThumbGap) / (ThumbMinSide * SizeFactor + ThumbGap)));
            var thumbSide = Math.Floor((width - ThumbGap * (thumbColumns - 1)) / thumbColumns);

            var recent = SortedByDate(library.Items);
            Sections.Children.Add(CreateSection("PhotosIconHistory", L("Photos_Home_Recent"),
                () => _shell?.Navigate(PhotosPage.Gallery),
                CreateThumbStrip(recent, thumbColumns, thumbSide, cts.Token)));

            var favorites = recent.Where(i => i.IsFavorite).ToList();
            if (favorites.Count > 0)
            {
                Sections.Children.Add(CreateSection("PhotosIconHeart", L("Photos_Nav_Favorites"),
                    () => _shell?.Navigate(PhotosPage.Favorites),
                    CreateThumbStrip(favorites, thumbColumns, thumbSide, cts.Token)));
            }

            var cardColumns = Math.Max(2, (int)Math.Floor((width + CardGap) / (CardMinWidth + CardGap)));
            var folders = library.GetFolderSummaries().Select(f => new CollectionEntry
            {
                Kind = CollectionEntryKind.Folder,
                Key = f.Path,
                Title = f.Name,
                Subtitle = f.Path,
                Count = f.Count,
                Cover = f.Cover,
                IsRootFolder = f.IsRoot
            }).ToList();
            if (folders.Count > 0)
            {
                Sections.Children.Add(CreateSection("DaisyIconFolder", L("Photos_Nav_Folders"),
                    () => _shell?.Navigate(PhotosPage.Folders),
                    CreateCardRow(folders, cardColumns)));
            }

            var albums = library.Albums.Select(a =>
            {
                var items = library.GetAlbumItems(a).ToList();
                return new CollectionEntry
                {
                    Kind = CollectionEntryKind.Album,
                    Key = a.Name,
                    Title = a.Name,
                    Subtitle = items.Count > 0 ? null : L("Photos_EmptyAlbum"),
                    Count = items.Count,
                    Cover = items.FirstOrDefault()
                };
            }).ToList();
            if (albums.Count > 0)
            {
                Sections.Children.Add(CreateSection("PhotosIconAlbum", L("Photos_Nav_Albums"),
                    () => _shell?.Navigate(PhotosPage.Albums),
                    CreateCardRow(albums, cardColumns)));
            }
        }

        /// <summary>Three short feature tiles for the welcome state (empty library).</summary>
        private static Grid CreateFeatureRow(double width)
        {
            var features = new (string Icon, string Brush, string Title, string Text)[]
            {
                ("PhotosIconGallery", "DaisyPrimaryBrush", "Photos_Home_FeatureOrganizeTitle", "Photos_Home_FeatureOrganizeText"),
                ("PhotosIconTune", "DaisySecondaryBrush", "Photos_Home_FeatureEditTitle", "Photos_Home_FeatureEditText"),
                ("PhotosIconSlideshow", "DaisyAccentBrush", "Photos_Home_FeatureShowTitle", "Photos_Home_FeatureShowText")
            };

            var columns = width >= 760 ? features.Length : 1;
            var row = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
            for (var c = 0; c < columns; c++)
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var r = 0; r < features.Length / columns; r++)
                row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var f = SizeFactor;
            for (var i = 0; i < features.Length; i++)
            {
                var (iconKey, brushKey, titleKey, textKey) = features[i];
                var accent = DaisyResourceLookup.GetBrush(brushKey);
                var badge = new Grid { Width = 44 * f, Height = 44 * f, HorizontalAlignment = HorizontalAlignment.Left };
                badge.Children.Add(new Ellipse { Fill = accent, Opacity = 0.16 });
                badge.Children.Add(new Viewbox { Margin = new Thickness(11 * f), Child = new Path { Data = Geo(iconKey), Fill = accent } });

                var panel = new StackPanel { Spacing = 6 };
                panel.Children.Add(badge);
                panel.Children.Add(Responsive(new TextBlock
                {
                    Text = L(titleKey),
                    Margin = new Thickness(0, 6, 0, 0),
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }, ResponsiveFontTier.Primary));
                panel.Children.Add(Responsive(new TextBlock
                {
                    Text = L(textKey),
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }, ResponsiveFontTier.Secondary));

                var tile = new Border
                {
                    Padding = new Thickness(18),
                    CornerRadius = new CornerRadius(14),
                    BorderThickness = new Thickness(1),
                    Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                    BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                    Child = panel
                };
                Grid.SetColumn(tile, i % columns);
                Grid.SetRow(tile, i / columns);
                row.Children.Add(tile);
            }
            return row;
        }

        private static StackPanel CreateSection(string iconKey, string title, Action showAll, UIElement content)
        {
            var f = SizeFactor;
            var icon = new Viewbox
            {
                Width = 20 * f,
                Height = 20 * f,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new Path { Data = Geo(iconKey), Fill = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush") }
            };
            var heading = Responsive(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            }, ResponsiveFontTier.SectionHeader);
            var button = new DaisyButton
            {
                Content = L("Photos_Home_ShowAll"),
                Variant = DaisyButtonVariant.Primary,
                Size = FlyoutSize,
                VerticalAlignment = VerticalAlignment.Center
            };
            FlowerySizeManager.SetIgnoreGlobalSize(button, true);
            button.Click += (_, _) => showAll();

            var header = new Grid { ColumnSpacing = 10 };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(heading, 1);
            Grid.SetColumn(button, 2);
            header.Children.Add(icon);
            header.Children.Add(heading);
            header.Children.Add(button);

            var section = new StackPanel { Spacing = 12 };
            section.Children.Add(header);
            section.Children.Add(content);
            return section;
        }

        private Grid CreateThumbStrip(List<PhotoItem> items, int columns, double side, CancellationToken token)
        {
            var strip = new Grid { ColumnSpacing = ThumbGap, HorizontalAlignment = HorizontalAlignment.Left };
            var count = Math.Min(columns, items.Count);
            for (var i = 0; i < count; i++)
            {
                strip.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var thumb = CreateThumb(items, i, side, token);
                Grid.SetColumn(thumb, i);
                strip.Children.Add(thumb);
            }
            return strip;
        }

        private ContentControl CreateThumb(List<PhotoItem> items, int index, double side, CancellationToken token)
        {
            var item = items[index];
            var brush = new ImageBrush { Stretch = Stretch.UniformToFill };
            var image = new Border { CornerRadius = new CornerRadius(9), Background = brush };
            var placeholder = new Viewbox
            {
                Width = side * 0.3,
                Height = side * 0.3,
                Opacity = 0.3,
                Child = new Path { Data = Geo("PhotosIconLandscape"), Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") }
            };
            var content = new Grid { Width = side - 6, Height = side - 6 };
            content.Children.Add(placeholder);
            content.Children.Add(image);

            var card = CreateCard(content, new Thickness(2), 12, () => _ = _shell?.OpenViewerAsync(items, index), item.FileName);
            ToolTipService.SetToolTip(card, item.FileName);

            if (ThumbnailLoader.TryGetCached(item, side) is { } cached)
                brush.ImageSource = cached;
            else
                _ = LoadThumbAsync(item, side, brush, token);
            return card;
        }

        private static async Task LoadThumbAsync(PhotoItem item, double side, ImageBrush brush, CancellationToken token)
        {
            var image = await ThumbnailLoader.LoadAsync(item, side, token);
            if (!token.IsCancellationRequested && image != null)
                brush.ImageSource = image;
        }

        private Grid CreateCardRow(List<CollectionEntry> entries, int columns)
        {
            var row = new Grid { ColumnSpacing = CardGap };
            for (var c = 0; c < columns; c++)
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var i = 0; i < Math.Min(columns, entries.Count); i++)
            {
                var card = new CollectionCard { Height = CardHeight };
                card.SetEntry(entries[i], this);
                Grid.SetColumn(card, i);
                row.Children.Add(card);
            }
            return row;
        }

        public void OnEntryInvoked(CollectionEntry entry)
        {
            var isAlbum = entry.Kind == CollectionEntryKind.Album;
            _shell?.OpenCollection(isAlbum ? PhotosPage.Albums : PhotosPage.Folders,
                isAlbum ? PhotoSourceKind.Album : PhotoSourceKind.Folder, entry.Key);
        }

        public void OnEntryContextRequested(CollectionEntry entry, FrameworkElement target, Point position)
        {
            // Album and folder actions live on their own pages.
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Focusable, clickable card: rounded base-200 surface whose border takes the Windows accent color on hover.
        /// </summary>
        private static ContentControl CreateCard(UIElement content, Thickness padding, double radius, Action onInvoke, string name)
        {
            var normalBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var surface = new Border
            {
                Padding = padding,
                CornerRadius = new CornerRadius(radius),
                BorderThickness = new Thickness(1),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                BorderBrush = normalBrush,
                Child = content
            };
            var card = new ContentControl
            {
                Content = surface,
                IsTabStop = true,
                UseSystemFocusVisuals = true,
                FocusVisualPrimaryBrush = WindowsAccent.Brush,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            AutomationProperties.SetName(card, name);
            card.PointerEntered += (_, _) => surface.BorderBrush = WindowsAccent.Brush;
            card.PointerExited += (_, _) => surface.BorderBrush = normalBrush;
            card.Tapped += (_, e) =>
            {
                e.Handled = true;
                onInvoke();
            };
            card.KeyDown += (_, e) =>
            {
                if (e.Key is VirtualKey.Enter or VirtualKey.Space)
                {
                    e.Handled = true;
                    onInvoke();
                }
            };
            return card;
        }

        private static Geometry Geo(string key) =>
            (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), Icon(key) ?? string.Empty);

        private static List<PhotoItem> SortedByDate(IEnumerable<PhotoItem> items) =>
            [.. items.OrderByDescending(i => i.SortDate)];

        #endregion
    }
}
