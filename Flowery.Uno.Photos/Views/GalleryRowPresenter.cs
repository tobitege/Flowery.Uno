using Flowery.Uno.Photos.Localization;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Recyclable element for <see cref="ItemsRepeater"/>: renders either a date header or a row of tiles.
    /// </summary>
    public sealed partial class GalleryRowPresenter : Grid
    {
        private readonly StackPanel _tilesPanel;
        private readonly Grid _header;
        private readonly TextBlock _headerTitle;
        private readonly TextBlock _headerCount;
        private readonly DaisyButton _headerSelect;
        private readonly List<PhotoTile> _tilePool = [];
        private GalleryRow? _row;
        private IPhotoTileHost? _host;

        public GalleryRowPresenter()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;

            _tilesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _headerTitle = new TextBlock
            {
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
            _headerCount = new TextBlock
            {
                FontSize = 13,
                Opacity = 0.6,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 2, 0, 0),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
            _headerSelect = new DaisyButton
            {
                Variant = DaisyButtonVariant.Primary,
                Size = DaisySize.ExtraSmall,
                Margin = new Thickness(8, 0, 0, 0),
                Opacity = 0,
                VerticalAlignment = VerticalAlignment.Center
            };
            FlowerySizeManager.SetIgnoreGlobalSize(_headerSelect, true);
            _headerSelect.Click += (_, _) =>
            {
                if (_row is GalleryHeaderRow header)
                    _host?.OnHeaderSelectToggled(header);
            };

            var headerContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2, 0, 0, 10)
            };
            headerContent.Children.Add(_headerTitle);
            headerContent.Children.Add(_headerCount);
            headerContent.Children.Add(_headerSelect);

            _header = new Grid { Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Colors.Transparent) };
            _header.Children.Add(headerContent);
            _header.PointerEntered += (_, _) => _headerSelect.Opacity = 1;
            _header.PointerExited += (_, _) => _headerSelect.Opacity = _host?.IsSelectionMode == true ? 1 : 0;

            Children.Add(_tilesPanel);
            Children.Add(_header);
        }

        public GalleryRow? Row => _row;

        public void SetRow(GalleryRow row, IPhotoTileHost host, double gap)
        {
            _row = row;
            _host = host;
            Height = row.Height;

            if (row is GalleryHeaderRow header)
            {
                _tilesPanel.Visibility = Visibility.Collapsed;
                _header.Visibility = Visibility.Visible;
                _headerTitle.Text = header.Title;
                _headerCount.Text = PhotosLocalization.Format(header.Items.Count == 1 ? "Photos_ItemCountOne" : "Photos_ItemCount", header.Items.Count);
                UpdateHeaderSelect(header);
                foreach (var tile in _tilePool)
                    tile.Detach();
                return;
            }

            if (row is not GalleryPhotoRow photoRow)
                return;

            _header.Visibility = Visibility.Collapsed;
            _tilesPanel.Visibility = Visibility.Visible;
            _tilesPanel.Spacing = gap;

            while (_tilePool.Count < photoRow.Tiles.Count)
                _tilePool.Add(new PhotoTile());

            for (var i = 0; i < photoRow.Tiles.Count; i++)
            {
                var tile = _tilePool[i];
                if (i >= _tilesPanel.Children.Count)
                    _tilesPanel.Children.Add(tile);
                var (item, width) = photoRow.Tiles[i];
                tile.SetItem(item, width, photoRow.TileHeight, host);
            }

            while (_tilesPanel.Children.Count > photoRow.Tiles.Count)
            {
                var last = (PhotoTile)_tilesPanel.Children[^1];
                last.Detach();
                _tilesPanel.Children.RemoveAt(_tilesPanel.Children.Count - 1);
            }
        }

        private void UpdateHeaderSelect(GalleryHeaderRow header)
        {
            var allSelected = header.Items.Count > 0 && header.Items.All(i => i.IsSelected);
            _headerSelect.Content = PhotosLocalization.GetString(allSelected ? "Photos_DeselectGroup" : "Photos_SelectGroup");
            _headerSelect.Opacity = _host?.IsSelectionMode == true ? 1 : 0;
        }

        /// <summary>
        /// Refreshes selection/favorite visuals without rebuilding.
        /// </summary>
        public void RefreshState()
        {
            if (_row is GalleryHeaderRow header)
            {
                UpdateHeaderSelect(header);
                return;
            }

            foreach (var child in _tilesPanel.Children)
            {
                if (child is PhotoTile tile)
                    tile.UpdateState();
            }
        }

        public void Clear()
        {
            foreach (var tile in _tilePool)
                tile.Detach();
            _row = null;
        }

        public PhotoTile? FindTile(Models.PhotoItem item) =>
            _tilesPanel.Children.OfType<PhotoTile>().FirstOrDefault(t => ReferenceEquals(t.Item, item));
    }
}
