using System.IO;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// App settings: appearance (theme, size, language), library folders, viewer/editor options, shortcuts and about.
    /// </summary>
    public sealed partial class SettingsView : UserControl
    {
        private PhotosShell? _shell;
        private bool _updatingLanguage;

        public SettingsView()
        {
            InitializeComponent();
            LanguageSelect.SelectionChanged += LanguageSelect_SelectionChanged;
            PhotosLocalization.Instance.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
            FlowerySizeManager.SizeChanged += (_, _) => DispatcherQueue.TryEnqueue(() =>
            {
                ApplyScale();
                Refresh();
            });
            Loaded += (_, _) => ApplyScale();
            var version = typeof(SettingsView).Assembly.GetName().Version;
            VersionText.Text = version == null ? string.Empty : $"Version {version.Major}.{version.Minor}.{version.Build}";
        }

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        private SettingsSnapshot? _snapshot;

        /// <summary>
        /// Hosted inside the settings dialog: the dialog shows the title, the page scrolls without outer padding.
        /// </summary>
        public void SetDialogMode()
        {
            PageTitle.Visibility = Visibility.Collapsed;
            RootScroller.Padding = new Thickness(0, 0, 14, 0);
        }

        /// <summary>Starts an editing session; changes preview live until <see cref="CommitEdit"/> or <see cref="CancelEdit"/>.</summary>
        public void BeginEdit()
        {
            _snapshot = SettingsSnapshot.Capture();
            Refresh();
            ApplyScale();
            RootScroller.ChangeView(null, 0, null, true);
        }

        public void CommitEdit() => _snapshot = null;

        public void CancelEdit()
        {
            var snapshot = _snapshot;
            _snapshot = null;
            snapshot?.Restore();
        }

        public void Initialize(PhotosShell shell)
        {
            _shell = shell;
            Refresh();
        }

        /// <summary>
        /// Scales the non-Daisy dimensions (page title, selector widths, icon) with the global size.
        /// Text blocks follow via FlowerySizeManager.ResponsiveFont.
        /// </summary>
        private void ApplyScale()
        {
            var f = SizeFactor;
            var size = FlowerySizeManager.CurrentSize;
            var buttonSize = size;
            ThemeDropdown.Size = size;
            SizeDropdown.Size = size;
            LanguageSelect.Size = size;
            foreach (var button in new[] { RescanButton, AddFolderButton, GitHubButton, DataFolderButton, ClearCacheButton })
                button.Size = buttonSize;
            PageTitle.FontSize = 34 * f;
            ThemeDropdown.Width = 240 * f;
            SizeDropdown.Width = 240 * f;
            LanguageSelect.Width = 240 * f;
            AboutIcon.Width = AboutIcon.Height = 56 * f;
            PageStack.Spacing = 16 * f;
            PageStack.MaxWidth = 860 * f;
        }

        public void Refresh()
        {
            BuildLanguageSelect();
            BuildFolders();
            BuildLibraryOptions();
            BuildViewerOptions();
            BuildShortcuts();
        }

        private void BuildLanguageSelect()
        {
            _updatingLanguage = true;
            try
            {
                List<SelectOption> options = [];
                foreach (var code in FloweryLocalization.SupportedLanguages)
                {
                    var name = FloweryLocalization.LanguageDisplayNames.TryGetValue(code, out var display) ? display : code;
                    options.Add(new SelectOption(code, name));
                }
                LanguageSelect.ItemsSource = options;
                var current = FloweryLocalization.CurrentCultureName;
                LanguageSelect.SelectedItem = options.FirstOrDefault(o => string.Equals((string)o.Key, current, StringComparison.OrdinalIgnoreCase))
                    ?? options.FirstOrDefault(o => current.StartsWith((string)o.Key, StringComparison.OrdinalIgnoreCase))
                    ?? options.FirstOrDefault();
            }
            finally
            {
                _updatingLanguage = false;
            }
        }

        private void LanguageSelect_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_updatingLanguage || LanguageSelect.SelectedItem is not SelectOption { Key: string code })
                return;
            if (string.Equals(code, FloweryLocalization.CurrentCultureName, StringComparison.OrdinalIgnoreCase))
                return;
            DispatcherQueue.TryEnqueue(() => FloweryLocalization.SetCulture(code));
        }

        private void BuildFolders()
        {
            var library = PhotoLibrary.Instance;
            LibraryStats.Text = PhotosLocalization.Format("Photos_Settings_LibraryStats", library.Items.Count, library.Folders.Count,
                FileOperations.FormatFileSize(library.Items.Sum(i => i.FileSize)));

            FoldersList.Children.Clear();
            if (library.Folders.Count == 0)
            {
                FoldersList.Children.Add(Responsive(new TextBlock
                {
                    Text = L("Photos_Settings_NoFolders"),
                    Opacity = 0.6,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }, ResponsiveFontTier.Primary));
                return;
            }

            foreach (var folder in library.Folders)
            {
                var path = folder;
                var row = new Grid
                {
                    ColumnSpacing = 10,
                    Padding = new Thickness(12, 6, 6, 6),
                    CornerRadius = new CornerRadius(8),
                    Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush")
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new Viewbox
                {
                    Width = 22 * SizeFactor,
                    Height = 22 * SizeFactor,
                    Opacity = 0.7,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new SymbolIcon(Symbol.Folder)
                };
                var count = library.Items.Count(i => PathRules.IsUnder(i.Path, path));
                var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                text.Children.Add(Responsive(new TextBlock
                {
                    Text = PathRules.FolderName(path),
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }, ResponsiveFontTier.Primary));
                text.Children.Add(Responsive(new TextBlock
                {
                    Text = Directory.Exists(path)
                        ? $"{path} · {PhotosLocalization.Format(count == 1 ? "Photos_ItemCountOne" : "Photos_ItemCount", count)}"
                        : $"{path} · {L("Photos_Settings_FolderMissing")}",
                    Opacity = 0.6,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }, ResponsiveFontTier.Secondary));

                var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                var open = new DaisyButton { Variant = DaisyButtonVariant.Ghost, Shape = DaisyButtonShape.Square, IconSymbol = Symbol.OpenLocal };
                ToolTipService.SetToolTip(open, L("Photos_OpenInExplorer"));
                open.Click += (_, _) => FileOperations.OpenFolder(path);
                var remove = new DaisyButton { Variant = DaisyButtonVariant.Ghost, Shape = DaisyButtonShape.Square, IconSymbol = Symbol.Delete };
                ToolTipService.SetToolTip(remove, L("Photos_RemoveFromLibrary"));
                remove.Click += async (_, _) =>
                {
                    if (_shell == null)
                        return;
                    var result = await _shell.ShowDialogAsync(L("Photos_RemoveFromLibrary"), PhotosLocalization.Format("Photos_RemoveFolderConfirm", path), L("Photos_Remove"));
                    if (result == DialogResult.Primary)
                        PhotoLibrary.Instance.RemoveFolder(path);
                };
                actions.Children.Add(open);
                actions.Children.Add(remove);

                Grid.SetColumn(text, 1);
                Grid.SetColumn(actions, 2);
                row.Children.Add(icon);
                row.Children.Add(text);
                row.Children.Add(actions);
                FoldersList.Children.Add(row);
            }
        }

        private void BuildLibraryOptions()
        {
            LibraryOptions.Children.Clear();
            LibraryOptions.Children.Add(ToggleRow("Photos_Settings_IncludeSubfolders", PhotosSettings.IncludeSubfolders,
                v => PhotoLibrary.Instance.ApplyIncludeSubfolders(v)));
        }

        private void BuildViewerOptions()
        {
            ViewerOptions.Children.Clear();
            ViewerOptions.Children.Add(ToggleRow("Photos_Settings_WheelNavigates", PhotosSettings.WheelNavigates, v =>
            {
                PhotosSettings.WheelNavigates = v;
                PhotosSettings.SaveOptions();
            }));
            ViewerOptions.Children.Add(ToggleRow("Photos_Settings_ConfirmDelete", PhotosSettings.ConfirmDelete, v =>
            {
                PhotosSettings.ConfirmDelete = v;
                PhotosSettings.SaveOptions();
            }));

            ViewerOptions.Children.Add(LabeledRange("Photos_Settings_SlideshowInterval", 1, 30, PhotosSettings.SlideshowSeconds, 1,
                v => PhotosLocalization.Format("Photos_SecondsFormat", v),
                v =>
                {
                    PhotosSettings.SlideshowSeconds = v;
                    PhotosSettings.SaveOptions();
                }));

            ViewerOptions.Children.Add(LabeledRange("Photos_Settings_JpegQuality", 40, 100, PhotosSettings.JpegQuality, 1,
                v => $"{v:0} %",
                v =>
                {
                    PhotosSettings.JpegQuality = (int)Math.Round(v);
                    PhotosSettings.SaveOptions();
                }));
        }

        private static Grid LabeledRange(string key, double min, double max, double value, double step, Func<double, string> format, Action<double> onChanged)
        {
            var grid = new Grid { ColumnSpacing = 12, RowSpacing = 4 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = Responsive(new TextBlock { Text = L(key), Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") }, ResponsiveFontTier.Primary);
            var valueText = Responsive(new TextBlock { Text = format(value), Opacity = 0.7, Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") }, ResponsiveFontTier.Primary);
            var range = Range(min, max, value, step, v =>
            {
                valueText.Text = format(v);
                onChanged(v);
            });
            range.Size = FlowerySizeManager.CurrentSize;
            Grid.SetColumn(valueText, 1);
            Grid.SetRow(range, 1);
            Grid.SetColumnSpan(range, 2);
            grid.Children.Add(label);
            grid.Children.Add(valueText);
            grid.Children.Add(range);
            return grid;
        }

        private void BuildShortcuts()
        {
            // Skia: the shortcuts that need the viewer (marked true) are listed once phase 4 ships it.
            (string Keys, string TextKey, bool NeedsViewer)[] allShortcuts =
            [
                ("Ctrl+F", "Photos_Shortcut_Search", false),
                ("Ctrl+,", "Photos_Shortcut_Settings", false),
                ("Ctrl+A", "Photos_Shortcut_SelectAll", false),
                ("Ctrl+Wheel", "Photos_Shortcut_ZoomTiles", false),
                ("← →", "Photos_Shortcut_Navigate", true),
                ("Ctrl+E", "Photos_Shortcut_Edit", true),
                ("Ctrl+Z", "Photos_Shortcut_Undo", true),
                ("Ctrl+Y", "Photos_Shortcut_Redo", true),
                ("Ctrl+S", "Photos_Shortcut_Save", true),
                ("+ / −", "Photos_Shortcut_Zoom", true),
                ("0", "Photos_Shortcut_Fit", true),
                ("F", "Photos_Shortcut_Favorite", true),
                ("X", "Photos_Shortcut_SwapColors", true),
                ("Del", "Photos_Shortcut_Delete", false),
                ("F5", "Photos_Shortcut_Slideshow", true),
                ("F11", "Photos_Shortcut_FullScreen", false),
                ("Esc", "Photos_Shortcut_Back", false)
            ];
            var shortcuts = allShortcuts.Where(s => PhotosShell.ViewerAvailable || !s.NeedsViewer).ToArray();

            ShortcutsGrid.Children.Clear();
            ShortcutsGrid.RowDefinitions.Clear();
            ShortcutsGrid.ColumnDefinitions.Clear();
            for (var c = 0; c < 2; c++)
            {
                ShortcutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                ShortcutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var rows = (shortcuts.Length + 1) / 2;
            for (var r = 0; r < rows; r++)
                ShortcutsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (var i = 0; i < shortcuts.Length; i++)
            {
                var row = i % rows;
                var col = i / rows * 2;
                var kbd = new DaisyKbd { Text = shortcuts[i].Keys, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                var text = Responsive(new TextBlock
                {
                    Text = L(shortcuts[i].TextKey),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }, ResponsiveFontTier.Primary);
                Grid.SetRow(kbd, row);
                Grid.SetColumn(kbd, col);
                Grid.SetRow(text, row);
                Grid.SetColumn(text, col + 1);
                ShortcutsGrid.Children.Add(kbd);
                ShortcutsGrid.Children.Add(text);
            }
        }

        private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_shell != null)
                await _shell.AddFolderInteractiveAsync();
        }

        private async void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            await PhotoLibrary.Instance.RescanAsync();
            _shell?.ShowToast(L("Photos_RescanDone"), DaisyAlertVariant.Success);
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e) =>
            FileOperations.OpenUri("https://github.com/tobitege/Flowery.Uno");

        private void DataFolderButton_Click(object sender, RoutedEventArgs e) =>
            FileOperations.OpenFolder(PhotosSettings.StateDirectory);

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            PhotoLibrary.Instance.ClearCache();
            _ = PhotoLibrary.Instance.RescanAsync();
            _shell?.ShowToast(L("Photos_CacheCleared"), DaisyAlertVariant.Success);
        }
    }
}
