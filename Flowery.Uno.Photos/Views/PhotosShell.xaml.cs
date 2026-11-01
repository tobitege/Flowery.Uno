using System.IO;
using System.Threading.Tasks;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Automation;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Flowery.Uno.Photos.Views
{
    public enum PhotosPage
    {
        Home,
        Gallery,
        Favorites,
        Albums,
        Folders,
        Settings
    }

    public enum DialogResult
    {
        None,
        Primary,
        Secondary
    }

    /// <summary>
    /// Application shell: title bar with search, navigation rail, pages, toasts and dialogs.
    /// Skia: the viewer overlay arrives in phase 4, so <see cref="ViewerAvailable"/> is false until then.
    /// </summary>
    public sealed partial class PhotosShell : UserControl
    {
        private readonly Dictionary<PhotosPage, (Border Indicator, DaisyButton Button, string TooltipKey)> _railItems = [];
        private MainWindow? _window;
        private PhotosPage _currentPage = PhotosPage.Home;
        private TaskCompletionSource<DialogResult>? _dialogTcs;
        private DaisyInput? _dialogInput;
        private long _dialogOpenToken;
        private DispatcherQueueTimer? _searchTimer;

        public PhotosShell()
        {
            InitializeComponent();
            Current = this;
            // Accelerators live on the shell; without this every tooltip in the app would append "Ctrl+F".
            KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
            BuildRail();
            PhotosLocalization.Instance.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(UpdateLocalizedChrome);
        }

        public static PhotosShell? Current { get; private set; }

        /// <summary>Skia: the viewer and slideshow come with phase 4; actions that start them stay hidden until then.</summary>
        public static bool ViewerAvailable => false;

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        public UIElement TitleBarElement => AppTitleBar;

        public MainWindow? HostWindow => _window;

        public PhotosPage CurrentPage => _currentPage;

        public void Initialize(MainWindow window, string? startupFile)
        {
            _window = window;

            // Skia: Windows extends the content into the title bar. The caption buttons overlay the right edge, so the row
            // keeps clear of them. Window.SetTitleBar is not used: with it the caption buttons did not show on Windows
            // (checked on the Windows 11 VM). Elsewhere the row is a normal row below the system title bar.
            if (window.UsesExtendedTitleBar)
            {
                AppTitleBar.Padding = new Thickness(14, 0, 150, 0);
            }

            var userName = Environment.UserName;
            UserAvatar.Initials = string.IsNullOrWhiteSpace(userName) ? "?" : userName[..1].ToUpperInvariant();
            ToolTipService.SetToolTip(UserAvatar, userName);

            var library = PhotoLibrary.Instance;
            library.Initialize(DispatcherQueue);
            library.ScanningChanged += (_, scanning) => ScanIndicator.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
            library.Changed += OnLibraryChanged;

            // Focus frame of the search box follows the Windows accent color like the other frames.
            SearchBox.BorderRingBrush = WindowsAccent.Brush;
            SettingsDialogFrame.BorderBrush = WindowsAccent.Brush;
            DialogFrame.BorderBrush = WindowsAccent.Brush;
            Home.Initialize(this);
            Gallery.Initialize(this);
            Collections.Initialize(this);
            SettingsPage.Initialize(this);
            SettingsPage.SetDialogMode();
            SettingsDialog.RegisterPropertyChangedCallback(DaisyModal.IsOpenProperty, (_, _) => OnSettingsDialogOpenChanged());
            RootGrid.SizeChanged += (_, _) => UpdateSettingsDialogSize();

            KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.F, VirtualKeyModifiers.Control, (_, a) =>
            {
                SearchBox.Focus(FocusState.Keyboard);
                a.Handled = true;
            }));
            KeyboardAccelerators.Add(CreateAccelerator((VirtualKey)188, VirtualKeyModifiers.Control, (_, a) =>
            {
                OpenSettings();
                a.Handled = true;
            }));
            KeyboardAccelerators.Add(CreateAccelerator(VirtualKey.F11, VirtualKeyModifiers.None, (_, a) =>
            {
                ToggleFullScreen();
                a.Handled = true;
            }));

            Navigate(PhotosPage.Home);
            UpdateLocalizedChrome();
            _ = library.RescanAsync();

            if (startupFile != null)
            {
                DispatcherQueue.TryEnqueue(() => _ = OpenExternalFilesAsync([startupFile]));
            }
        }

        public void Shutdown()
        {
        }

        private static KeyboardAccelerator CreateAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
        {
            var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
            accelerator.Invoked += handler;
            return accelerator;
        }

        #region Navigation rail

        private void BuildRail()
        {
            AddRailItem(RailTop, PhotosPage.Home, "PhotosIconHome", "Photos_Nav_Home");
            AddRailItem(RailTop, PhotosPage.Gallery, "PhotosIconGallery", "Photos_Nav_Gallery");
            AddRailItem(RailTop, PhotosPage.Favorites, "PhotosIconHeartOutline", "Photos_Nav_Favorites");
            AddRailItem(RailTop, PhotosPage.Albums, "PhotosIconAlbum", "Photos_Nav_Albums");
            AddRailItem(RailTop, PhotosPage.Folders, "DaisyIconFolder", "Photos_Nav_Folders");
            AddRailItem(RailBottom, PhotosPage.Settings, "DaisyIconSettings", "Photos_Nav_Settings");
        }

        private void AddRailItem(Panel host, PhotosPage page, string iconKey, string tooltipKey)
        {
            var indicator = new Border
            {
                Width = 3,
                Height = 18,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                Opacity = 0
            };

            var button = new DaisyButton
            {
                Variant = DaisyButtonVariant.Ghost,
                Shape = DaisyButtonShape.Square,
                Size = DaisySize.Medium,
                IconData = Application.Current.Resources[iconKey] as string,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = page
            };
            FlowerySizeManager.SetIgnoreGlobalSize(button, true);
            button.Click += async (_, _) => await NavigateFromRailAsync(page);

            var container = new Grid { Height = 48 };
            container.Children.Add(button);
            container.Children.Add(indicator);
            host.Children.Add(container);

            _railItems[page] = (indicator, button, tooltipKey);
        }

        private void UpdateRailSelection()
        {
            foreach (var (page, item) in _railItems)
            {
                var selected = page == PhotosPage.Settings ? SettingsDialog.IsOpen : page == _currentPage && !SettingsDialog.IsOpen;
                item.Indicator.Opacity = selected ? 1 : 0;
                item.Indicator.Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                item.Button.Variant = selected ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost;
                item.Button.ButtonStyle = selected ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;
            }
        }

        private void UpdateLocalizedChrome()
        {
            foreach (var (_, item) in _railItems)
            {
                var text = PhotosLocalization.GetString(item.TooltipKey);
                ToolTipService.SetToolTip(item.Button, text);
                AutomationProperties.SetName(item.Button, text);
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Rail navigation also works while the viewer is open: it closes the viewer first
        /// (asking about unsaved edits) and then shows the requested page.
        /// </summary>
        private Task NavigateFromRailAsync(PhotosPage page)
        {
            // Settings open as a dialog above the current content (also above the editor).
            if (page == PhotosPage.Settings)
            {
                OpenSettings();
                return Task.CompletedTask;
            }
            Navigate(page);
            return Task.CompletedTask;
        }

        public void Navigate(PhotosPage page)
        {
            if (page == PhotosPage.Settings)
            {
                OpenSettings();
                return;
            }
            _currentPage = page;
            Home.Visibility = page == PhotosPage.Home ? Visibility.Visible : Visibility.Collapsed;
            Gallery.Visibility = page is PhotosPage.Gallery or PhotosPage.Favorites ? Visibility.Visible : Visibility.Collapsed;
            Collections.Visibility = page is PhotosPage.Albums or PhotosPage.Folders ? Visibility.Visible : Visibility.Collapsed;
            SearchBox.Visibility = Visibility.Visible;

            switch (page)
            {
                case PhotosPage.Home:
                    Home.Refresh();
                    break;
                case PhotosPage.Gallery:
                    Gallery.ShowSource(PhotoSourceKind.All, null);
                    break;
                case PhotosPage.Favorites:
                    Gallery.ShowSource(PhotoSourceKind.Favorites, null);
                    break;
                case PhotosPage.Albums:
                    Collections.ShowAlbums();
                    break;
                case PhotosPage.Folders:
                    Collections.ShowFolders();
                    break;
            }

            UpdateRailSelection();
        }

        /// <summary>
        /// Shows an album or folder in the gallery page while keeping the collection rail item highlighted.
        /// </summary>
        public void ShowCollection(PhotoSourceKind kind, string key)
        {
            Home.Visibility = Visibility.Collapsed;
            Gallery.Visibility = Visibility.Visible;
            Collections.Visibility = Visibility.Collapsed;
            SearchBox.Visibility = Visibility.Visible;
            Gallery.ShowSource(kind, key);
        }

        /// <summary>
        /// Opens an album or folder from outside its collection page (e.g. the home page); Back returns to that collection page.
        /// </summary>
        public void OpenCollection(PhotosPage page, PhotoSourceKind kind, string key)
        {
            Navigate(page);
            ShowCollection(kind, key);
        }

        public void BackToCollections()
        {
            Navigate(_currentPage is PhotosPage.Albums or PhotosPage.Folders ? _currentPage : PhotosPage.Albums);
        }

        private void OnLibraryChanged(object? sender, EventArgs e)
        {
            if (Home.Visibility == Visibility.Visible)
                Home.Refresh();
            if (Gallery.Visibility == Visibility.Visible)
                Gallery.RefreshFromLibrary();
            if (Collections.Visibility == Visibility.Visible)
                Collections.Refresh();
            if (SettingsDialog.IsOpen)
                SettingsPage.Refresh();
        }

        public string SearchText => SearchBox.Text ?? string.Empty;

        private void SearchBox_TextChanged(DaisyInput sender, TextChangedEventArgs args)
        {
            if (_searchTimer == null)
            {
                _searchTimer = DispatcherQueue.CreateTimer();
                _searchTimer.Interval = TimeSpan.FromMilliseconds(220);
                _searchTimer.IsRepeating = false;
                _searchTimer.Tick += (_, _) =>
                {
                    // Searching from the home page shows the matching photos in the gallery.
                    if (Home.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(SearchText))
                        Navigate(PhotosPage.Gallery);
                    if (Collections.Visibility == Visibility.Visible)
                        Collections.ApplySearch(SearchText);
                    else if (Gallery.Visibility == Visibility.Visible)
                        Gallery.ApplySearch(SearchText);
                };
            }
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        public void ClearSearch()
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
                SearchBox.Text = string.Empty;
        }

        #endregion

        #region Viewer

        // Skia: the viewer and the slideshow arrive in phase 4. Callers keep their WinUI shape.
        public Task OpenViewerAsync(IReadOnlyList<PhotoItem> items, int index, bool edit = false) => Task.CompletedTask;

        public Task StartSlideshowAsync(IReadOnlyList<PhotoItem> items, int index) => Task.CompletedTask;

        public void ToggleFullScreen()
        {
            if (_window == null)
                return;
            _window.SetFullScreen(!_window.IsFullScreen);
        }

        public bool IsFullScreen => _window?.IsFullScreen == true;

        public void SetFullScreen(bool value) => _window?.SetFullScreen(value);

        /// <summary>
        /// Shows or hides the shell chrome (title bar and navigation rail), e.g. for slideshow and immersive full screen.
        /// </summary>
        public void SetTitleBarVisible(bool visible)
        {
            AppTitleBar.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            Rail.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Settings dialog

        private bool _settingsAccepted;

        public void OpenSettings()
        {
            if (SettingsDialog.IsOpen)
                return;
            _settingsAccepted = false;
            SettingsPage.BeginEdit();
            UpdateSettingsDialogSize();
            SettingsDialog.IsOpen = true;
            UpdateRailSelection();
        }

        private void UpdateSettingsDialogSize()
        {
            var f = UiHelpers.SizeFactor;
            SettingsDialogContent.Width = Math.Max(320, Math.Min(900 * f, RootGrid.ActualWidth - 96));
            SettingsDialogContent.Height = Math.Max(320, Math.Min(860, RootGrid.ActualHeight - 120));
        }

        private void SettingsOk_Click(object sender, RoutedEventArgs e)
        {
            _settingsAccepted = true;
            SettingsDialog.IsOpen = false;
        }

        private void SettingsCancel_Click(object sender, RoutedEventArgs e)
        {
            _settingsAccepted = false;
            SettingsDialog.IsOpen = false;
        }

        /// <summary>
        /// OK keeps the previewed changes; Cancel, Esc, the close button or a click outside restore the previous state.
        /// </summary>
        private void OnSettingsDialogOpenChanged()
        {
            if (SettingsDialog.IsOpen)
                return;

            if (_settingsAccepted)
                SettingsPage.CommitEdit();
            else
                SettingsPage.CancelEdit();
            _settingsAccepted = false;
            UpdateRailSelection();

        }

        #endregion

        #region Toasts

        public void ShowToast(string message, DaisyAlertVariant variant = DaisyAlertVariant.Info)
        {
            var alert = new DaisyAlert
            {
                Variant = variant,
                Message = message,
                MaxWidth = 520,
                Margin = new Thickness(0, 6, 0, 0)
            };
            Toasts.AddToast(alert);
            UiTimers.RunOnce(DispatcherQueue, TimeSpan.FromSeconds(3.2), () => Toasts.RemoveToast(alert));
        }

        #endregion

        #region Dialogs

        public Task<DialogResult> ShowDialogAsync(string title, string message, string primaryText, string? secondaryText = null, bool primaryIsDanger = false)
        {
            return ShowDialogCoreAsync(title, message, primaryText, secondaryText, primaryIsDanger, null, null);
        }

        public async Task<string?> ShowInputDialogAsync(string title, string message, string initialText, string primaryText)
        {
            var result = await ShowDialogCoreAsync(title, message, primaryText, null, false, initialText, null);
            var text = _dialogInput?.Text?.Trim();
            return result == DialogResult.Primary && !string.IsNullOrWhiteSpace(text) ? text : null;
        }

        private Task<DialogResult> ShowDialogCoreAsync(string title, string message, string primaryText, string? secondaryText, bool primaryIsDanger, string? inputText, object? extra)
        {
            _dialogTcs?.TrySetResult(DialogResult.None);
            var tcs = new TaskCompletionSource<DialogResult>();
            _dialogTcs = tcs;

            DialogPanel.Children.Clear();
            DialogPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(message))
            {
                DialogPanel.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.8
                });
            }

            _dialogInput = null;
            if (inputText != null)
            {
                _dialogInput = new DaisyInput { Text = inputText, HorizontalAlignment = HorizontalAlignment.Stretch };
                _dialogInput.KeyDown += (_, e) =>
                {
                    if (e.Key == VirtualKey.Enter)
                    {
                        e.Handled = true;
                        CompleteDialog(DialogResult.Primary);
                    }
                };
                DialogPanel.Children.Add(_dialogInput);
            }

            if (extra is UIElement element)
                DialogPanel.Children.Add(element);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var cancel = new DaisyButton { Content = PhotosLocalization.GetString("Photos_Cancel"), Variant = DaisyButtonVariant.Neutral };
            cancel.Click += (_, _) => CompleteDialog(DialogResult.None);
            buttons.Children.Add(cancel);

            if (secondaryText != null)
            {
                var secondary = new DaisyButton { Content = secondaryText, Variant = DaisyButtonVariant.Neutral };
                secondary.Click += (_, _) => CompleteDialog(DialogResult.Secondary);
                buttons.Children.Add(secondary);
            }

            var primary = new DaisyButton
            {
                Content = primaryText,
                Variant = primaryIsDanger ? DaisyButtonVariant.Error : DaisyButtonVariant.Primary
            };
            primary.Click += (_, _) => CompleteDialog(DialogResult.Primary);
            buttons.Children.Add(primary);
            DialogPanel.Children.Add(buttons);

            if (_dialogOpenToken == 0)
            {
                _dialogOpenToken = Dialog.RegisterPropertyChangedCallback(DaisyModal.IsOpenProperty, (_, _) =>
                {
                    if (!Dialog.IsOpen)
                        _dialogTcs?.TrySetResult(DialogResult.None);
                });
            }

            Dialog.IsOpen = true;
            if (_dialogInput != null)
            {
                var input = _dialogInput;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    input.Focus(FocusState.Programmatic);
                    input.SelectionStart = 0;
                    input.SelectionLength = input.Text?.Length ?? 0;
                });
            }
            return tcs.Task;
        }

        private void CompleteDialog(DialogResult result)
        {
            var tcs = _dialogTcs;
            tcs?.TrySetResult(result);
            Dialog.IsOpen = false;
        }

        public Task<bool> ConfirmDeleteAsync(int count)
        {
            if (!PhotosSettings.ConfirmDelete)
                return Task.FromResult(true);

            var message = count == 1
                ? PhotosLocalization.GetString("Photos_DeleteConfirmOne")
                : PhotosLocalization.Format("Photos_DeleteConfirmMany", count);
            return ShowDialogAsync(PhotosLocalization.GetString("Photos_DeleteTitle"), message, PhotosLocalization.GetString("Photos_Delete"), null, primaryIsDanger: true)
                .ContinueWith(t => t.Result == DialogResult.Primary, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion

        #region Pickers

        public async Task<string?> PickFolderAsync()
        {
            try
            {
                var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
                picker.FileTypeFilter.Add("*");
                var result = await picker.PickSingleFolderAsync();
                return result?.Path;
            }
            catch (Exception ex)
            {
                ShowToast(ex.Message, DaisyAlertVariant.Error);
                return null;
            }
        }

        public async Task<IReadOnlyList<string>> PickFilesAsync()
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    ViewMode = PickerViewMode.Thumbnail
                };
                foreach (var ext in ImageMetadataReader.SupportedExtensions)
                    picker.FileTypeFilter.Add(ext);
                var results = await picker.PickMultipleFilesAsync();
                return results == null ? [] : [.. results.Select(r => r.Path)];
            }
            catch (Exception ex)
            {
                ShowToast(ex.Message, DaisyAlertVariant.Error);
                return [];
            }
        }

        public async Task AddFolderInteractiveAsync()
        {
            var folder = await PickFolderAsync();
            if (folder == null)
                return;
            if (PhotoLibrary.Instance.AddFolder(folder))
                ShowToast(PhotosLocalization.Format("Photos_FolderAdded", PathRules.FolderName(folder)), DaisyAlertVariant.Success);
            else
                ShowToast(PhotosLocalization.GetString("Photos_FolderAlreadyAdded"), DaisyAlertVariant.Info);
        }

        public async Task ImportFilesInteractiveAsync()
        {
            var files = await PickFilesAsync();
            if (files.Count == 0)
                return;
            var count = await PhotoLibrary.Instance.ImportFilesAsync(files);
            ShowToast(PhotosLocalization.Format("Photos_ImportedCount", count), DaisyAlertVariant.Success);
        }

        #endregion

        #region Drag & drop

        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                return;
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = PhotosLocalization.GetString("Photos_DropCaption");
            DropOverlay.Visibility = Visibility.Visible;
        }

        private void RootGrid_DragLeave(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void RootGrid_Drop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
                return;

            var deferral = e.GetDeferral();
            try
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var folders = items.OfType<StorageFolder>().Select(f => f.Path).ToList();
                var files = items.OfType<StorageFile>().Select(f => f.Path).Where(ImageMetadataReader.IsSupported).ToList();

                foreach (var folder in folders)
                {
                    if (PhotoLibrary.Instance.AddFolder(folder))
                        ShowToast(PhotosLocalization.Format("Photos_FolderAdded", PathRules.FolderName(folder)), DaisyAlertVariant.Success);
                }

                if (files.Count > 0)
                {
                    if (ViewerAvailable)
                    {
                        await OpenExternalFilesAsync(files);
                    }
                    else
                    {
                        // Skia: without the viewer (phase 4) a dropped photo is imported into the library, as in phase 2.
                        var count = await PhotoLibrary.Instance.ImportFilesAsync(files);
                        ShowToast(PhotosLocalization.Format("Photos_ImportedCount", count), DaisyAlertVariant.Success);
                    }
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>
        /// Opens files that may live outside the library in the viewer.
        /// </summary>
        public async Task OpenExternalFilesAsync(IReadOnlyList<string> files)
        {
            var items = files
                .Where(File.Exists)
                .Select(f => PhotoLibrary.Instance.GetOrCreateItem(f))
                .Where(i => i != null)
                .Cast<PhotoItem>()
                .ToList();

            if (items.Count == 0)
                return;

            // A single external file: browse its siblings like the Windows Photos app does.
            if (items.Count == 1 && !PhotoLibrary.Instance.IsInLibrary(items[0].Path))
            {
                var folder = items[0].FolderPath;
                try
                {
                    var siblings = Directory.EnumerateFiles(folder)
                        .Where(ImageMetadataReader.IsSupported)
                        .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase)
                        .Select(f => string.Equals(f, items[0].Path, StringComparison.OrdinalIgnoreCase) ? items[0] : PhotoLibrary.Instance.GetOrCreateItem(f))
                        .Where(i => i != null)
                        .Cast<PhotoItem>()
                        .ToList();
                    var index = siblings.FindIndex(i => ReferenceEquals(i, items[0]));
                    if (index >= 0)
                    {
                        await OpenViewerAsync(siblings, index);
                        return;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Fall back to the single file.
                }
            }

            await OpenViewerAsync(items, 0);
        }

        #endregion
    }
}
