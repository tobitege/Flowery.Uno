using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Windows.Storage.Pickers;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace Flowery.Uno.Photos.Views
{
    public enum DialogResult
    {
        None,
        Primary,
        Secondary
    }

    /// <summary>
    /// Skia: phase 2 host for the gallery. It offers only what <see cref="GalleryView"/> calls on the shell
    /// (toasts, dialogs, pickers, drag and drop, search). Phase 3 replaces this file with the PhotosShell
    /// of Flowery.WinUI.Photos (title bar, navigation rail, pages, viewer, settings).
    /// </summary>
    public sealed partial class PhotosShell : UserControl
    {
        private readonly Grid _root = new();
        private readonly GalleryView _gallery = new();
        private readonly DaisyInput _searchBox;
        private readonly DaisyToast _toasts;
        private readonly DaisyModal _dialog;
        private readonly StackPanel _dialogPanel;
        private readonly Border _dropOverlay;
        private MainWindow? _window;
        private TaskCompletionSource<DialogResult>? _dialogTcs;
        private DaisyInput? _dialogInput;
        private long _dialogOpenToken;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _searchTimer;

        public PhotosShell()
        {
            _searchBox = new DaisyInput
            {
                Variant = DaisyInputVariant.Bordered,
                Size = DaisySize.Small,
                Margin = new Thickness(12, 8, 12, 8),
                MaxWidth = 460,
                HorizontalAlignment = HorizontalAlignment.Left,
                PlaceholderText = PhotosLocalization.GetString("Photos_SearchPlaceholder")
            };
            FlowerySizeManager.SetIgnoreGlobalSize(_searchBox, true);
            _searchBox.TextChanged += SearchBox_TextChanged;

            _toasts = new DaisyToast
            {
                HorizontalPosition = ToastHorizontalPosition.Center,
                VerticalPosition = ToastVerticalPosition.Bottom,
                ToastOffset = new Thickness(16, 16, 16, 64)
            };

            _dialogPanel = new StackPanel { Spacing = 14, MinWidth = 320, MaxWidth = 460 };
            _dialog = new DaisyModal
            {
                IsOpen = false,
                Content = new Border
                {
                    Margin = new Thickness(-24),
                    Padding = new Thickness(24),
                    CornerRadius = new CornerRadius(16),
                    BorderThickness = new Thickness(1),
                    BorderBrush = WindowsAccent.Brush,
                    Child = _dialogPanel
                }
            };

            _dropOverlay = new Border
            {
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Margin = new Thickness(16),
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(2),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
                Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush"),
                Opacity = 0.92,
                Child = new TextBlock
                {
                    Text = PhotosLocalization.GetString("Photos_DropHint"),
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                }
            };

            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _root.AllowDrop = true;
            _root.DragOver += RootGrid_DragOver;
            _root.DragLeave += (_, _) => _dropOverlay.Visibility = Visibility.Collapsed;
            _root.Drop += RootGrid_Drop;

            Grid.SetRow(_gallery, 1);
            Grid.SetRowSpan(_toasts, 2);
            Grid.SetRowSpan(_dialog, 2);
            Grid.SetRowSpan(_dropOverlay, 2);
            _root.Children.Add(_searchBox);
            _root.Children.Add(_gallery);
            _root.Children.Add(_dropOverlay);
            _root.Children.Add(_toasts);
            _root.Children.Add(_dialog);
            Content = _root;
        }

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        public UIElement TitleBarElement => _searchBox;

        public MainWindow? HostWindow => _window;

        public void Initialize(MainWindow window, string? startupFile)
        {
            _window = window;

            // The content extends into the title bar on Windows, and the caption area takes the clicks: keep the box below it.
            // The real shell of phase 3 puts it into a TitleBar with its own drag region.
            if (window.UsesExtendedTitleBar)
                _searchBox.Margin = new Thickness(12, 44, 12, 8);

            var library = PhotoLibrary.Instance;
            library.Initialize(DispatcherQueue);
            library.Changed += (_, _) => _gallery.RefreshFromLibrary();

            _gallery.Initialize(this);
            _gallery.ShowSource(PhotoSourceKind.All, null);
            _ = library.RescanAsync();
        }

        public void Shutdown()
        {
        }

        #region Navigation

        public void BackToCollections() => _gallery.ShowSource(PhotoSourceKind.All, null);

        public string SearchText => _searchBox.Text ?? string.Empty;

        private void SearchBox_TextChanged(DaisyInput sender, TextChangedEventArgs args)
        {
            if (_searchTimer == null)
            {
                _searchTimer = DispatcherQueue.CreateTimer();
                _searchTimer.Interval = TimeSpan.FromMilliseconds(220);
                _searchTimer.IsRepeating = false;
                _searchTimer.Tick += (_, _) => _gallery.ApplySearch(SearchText);
            }
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        public void ClearSearch()
        {
            if (!string.IsNullOrEmpty(_searchBox.Text))
                _searchBox.Text = string.Empty;
        }

        // Viewer and slideshow arrive in phase 4.
        public Task OpenViewerAsync(IReadOnlyList<PhotoItem> items, int index, bool edit = false) => Task.CompletedTask;

        public Task StartSlideshowAsync(IReadOnlyList<PhotoItem> items, int index) => Task.CompletedTask;

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
            _toasts.AddToast(alert);
            UiTimers.RunOnce(DispatcherQueue, TimeSpan.FromSeconds(3.2), () => _toasts.RemoveToast(alert));
        }

        #endregion

        #region Dialogs

        public Task<DialogResult> ShowDialogAsync(string title, string message, string primaryText, string? secondaryText = null, bool primaryIsDanger = false)
        {
            return ShowDialogCoreAsync(title, message, primaryText, secondaryText, primaryIsDanger, null);
        }

        public async Task<string?> ShowInputDialogAsync(string title, string message, string initialText, string primaryText)
        {
            var result = await ShowDialogCoreAsync(title, message, primaryText, null, false, initialText);
            var text = _dialogInput?.Text?.Trim();
            return result == DialogResult.Primary && !string.IsNullOrWhiteSpace(text) ? text : null;
        }

        private Task<DialogResult> ShowDialogCoreAsync(string title, string message, string primaryText, string? secondaryText, bool primaryIsDanger, string? inputText)
        {
            _dialogTcs?.TrySetResult(DialogResult.None);
            var tcs = new TaskCompletionSource<DialogResult>();
            _dialogTcs = tcs;

            _dialogPanel.Children.Clear();
            _dialogPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(message))
            {
                _dialogPanel.Children.Add(new TextBlock
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
                    if (e.Key == Windows.System.VirtualKey.Enter)
                    {
                        e.Handled = true;
                        CompleteDialog(DialogResult.Primary);
                    }
                };
                _dialogPanel.Children.Add(_dialogInput);
            }

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
            _dialogPanel.Children.Add(buttons);

            if (_dialogOpenToken == 0)
            {
                _dialogOpenToken = _dialog.RegisterPropertyChangedCallback(DaisyModal.IsOpenProperty, (_, _) =>
                {
                    if (!_dialog.IsOpen)
                        _dialogTcs?.TrySetResult(DialogResult.None);
                });
            }

            _dialog.IsOpen = true;
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
            _dialog.IsOpen = false;
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
            _dropOverlay.Visibility = Visibility.Visible;
        }

        private async void RootGrid_Drop(object sender, DragEventArgs e)
        {
            _dropOverlay.Visibility = Visibility.Collapsed;
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
                    var count = await PhotoLibrary.Instance.ImportFilesAsync(files);
                    ShowToast(PhotosLocalization.Format("Photos_ImportedCount", count), DaisyAlertVariant.Success);
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        #endregion
    }
}
