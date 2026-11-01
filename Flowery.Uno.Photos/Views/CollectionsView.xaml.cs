using System.Threading.Tasks;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Albums and folders overview as a responsive card grid.
    /// </summary>
    public sealed partial class CollectionsView : UserControl, ICollectionCardHost
    {
        private PhotosShell? _shell;
        private CollectionEntryKind _mode = CollectionEntryKind.Album;
        private List<CollectionEntry> _entries = [];
        private string _search = string.Empty;

        public CollectionsView()
        {
            InitializeComponent();
            PhotosLocalization.Instance.PropertyChanged += (_, _) => DispatcherQueue.TryEnqueue(Refresh);
        }

        public PhotosLocalization Localization => PhotosLocalization.Instance;

        public void Initialize(PhotosShell shell) => _shell = shell;

        public void ShowAlbums()
        {
            _mode = CollectionEntryKind.Album;
            Refresh();
        }

        public void ShowFolders()
        {
            _mode = CollectionEntryKind.Folder;
            Refresh();
        }

        public void ApplySearch(string text)
        {
            _search = text ?? string.Empty;
            Refresh();
        }

        public void Refresh()
        {
            var library = PhotoLibrary.Instance;
            var culture = PhotosLocalization.CurrentCulture;

            if (_mode == CollectionEntryKind.Album)
            {
                TitleText.Text = L("Photos_Nav_Albums");
                PrimaryActionButton.Content = L("Photos_NewAlbum");
                PrimaryActionButton.IconSymbol = Symbol.Add;
            }
            else
            {
                TitleText.Text = L("Photos_Nav_Folders");
                PrimaryActionButton.Content = L("Photos_AddFolder");
                PrimaryActionButton.IconSymbol = Symbol.NewFolder;
            }

            // Skia: the list is built by CollectionEntryBuilder so it can be tested.
            _entries = CollectionEntryBuilder.Build(library, _mode, _search, culture);

            CountText.Text = PhotosLocalization.Format(_entries.Count == 1 ? "Photos_ItemCountOne" : "Photos_ItemCount", _entries.Count);
            Repeater.ItemsSource = _entries;

            var empty = _entries.Count == 0;
            EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            Scroller.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            if (empty)
            {
                EmptyIcon.Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry),
                    Icon(_mode == CollectionEntryKind.Album ? "PhotosIconAlbum" : "DaisyIconFolder") ?? string.Empty);
                EmptyTitle.Text = L(_mode == CollectionEntryKind.Album ? "Photos_Empty_AlbumsTitle" : "Photos_Empty_FoldersTitle");
                EmptyText.Text = L(_mode == CollectionEntryKind.Album ? "Photos_Empty_AlbumsText" : "Photos_Empty_FoldersText");
            }
        }

        private void Repeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
            if (args.Element is CollectionCard card && args.Index >= 0 && args.Index < _entries.Count)
                card.SetEntry(_entries[args.Index], this);
        }

        private async void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_shell == null)
                return;

            if (_mode == CollectionEntryKind.Folder)
            {
                await _shell.AddFolderInteractiveAsync();
                return;
            }

            var name = await _shell.ShowInputDialogAsync(L("Photos_NewAlbum"), L("Photos_AlbumNamePrompt"), L("Photos_AlbumDefaultName"), L("Photos_Create"));
            if (name == null)
                return;
            var album = PhotoLibrary.Instance.CreateAlbum(name);
            _shell.ShowToast(PhotosLocalization.Format("Photos_AlbumCreatedHint", album.Name), DaisyAlertVariant.Success);
        }

        public void OnEntryInvoked(CollectionEntry entry)
        {
            _shell?.ShowCollection(entry.Kind == CollectionEntryKind.Album ? PhotoSourceKind.Album : PhotoSourceKind.Folder, entry.Key);
        }

        public void OnEntryContextRequested(CollectionEntry entry, FrameworkElement target, Point position)
        {
            var menu = new MenuFlyout();
            menu.Items.Add(MenuItem("Photos_Open", Symbol.OpenFile, () => OnEntryInvoked(entry)));
            menu.Items.Add(MenuItem("Photos_Slideshow", null, () => _ = StartSlideshowAsync(entry), Icon("PhotosIconSlideshow")));

            if (entry.Kind == CollectionEntryKind.Album)
            {
                menu.Items.Add(new MenuFlyoutSeparator());
                menu.Items.Add(MenuItem("Photos_RenameAlbum", Symbol.Rename, () => _ = RenameAsync(entry)));
                menu.Items.Add(MenuItem("Photos_DeleteAlbum", Symbol.Delete, () => _ = DeleteAlbumAsync(entry)));
            }
            else
            {
                menu.Items.Add(new MenuFlyoutSeparator());
                menu.Items.Add(MenuItem("Photos_OpenInExplorer", Symbol.Folder, () => FileOperations.OpenFolder(entry.Key)));
                if (entry.IsRootFolder)
                {
                    menu.Items.Add(MenuItem("Photos_RemoveFromLibrary", Symbol.Remove, () => _ = RemoveFolderAsync(entry)));
                }
            }

            menu.ShowAt(target, new FlyoutShowOptions { Position = position });
        }

        private async Task StartSlideshowAsync(CollectionEntry entry)
        {
            if (_shell == null)
                return;
            var items = PhotoLibrary.Instance.Query(new PhotoQuery
            {
                Source = entry.Kind == CollectionEntryKind.Album ? PhotoSourceKind.Album : PhotoSourceKind.Folder,
                SourceKey = entry.Key,
                Sort = PhotoSortMode.DateTakenOldest
            });
            if (items.Count > 0)
                await _shell.StartSlideshowAsync(items, 0);
        }

        private async Task RenameAsync(CollectionEntry entry)
        {
            if (_shell == null || PhotoLibrary.Instance.FindAlbum(entry.Key) is not { } album)
                return;
            var name = await _shell.ShowInputDialogAsync(L("Photos_RenameAlbum"), L("Photos_AlbumNamePrompt"), album.Name, L("Photos_Rename"));
            if (name != null && !PhotoLibrary.Instance.RenameAlbum(album, name))
                _shell.ShowToast(L("Photos_AlbumNameTaken"), DaisyAlertVariant.Warning);
        }

        private async Task DeleteAlbumAsync(CollectionEntry entry)
        {
            if (_shell == null || PhotoLibrary.Instance.FindAlbum(entry.Key) is not { } album)
                return;
            var result = await _shell.ShowDialogAsync(L("Photos_DeleteAlbum"), PhotosLocalization.Format("Photos_DeleteAlbumConfirm", album.Name), L("Photos_Delete"), null, primaryIsDanger: true);
            if (result == DialogResult.Primary)
                PhotoLibrary.Instance.DeleteAlbum(album);
        }

        private async Task RemoveFolderAsync(CollectionEntry entry)
        {
            if (_shell == null)
                return;
            var result = await _shell.ShowDialogAsync(L("Photos_RemoveFromLibrary"), PhotosLocalization.Format("Photos_RemoveFolderConfirm", entry.Key), L("Photos_Remove"));
            if (result == DialogResult.Primary)
                PhotoLibrary.Instance.RemoveFolder(entry.Key);
        }
    }
}
