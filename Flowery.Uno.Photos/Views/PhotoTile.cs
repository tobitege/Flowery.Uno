using System.ComponentModel;
using System.Threading;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Windows.System;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Callbacks from tiles and headers to the hosting gallery.
    /// </summary>
    public interface IPhotoTileHost
    {
        bool IsSelectionMode { get; }
        bool ShowFileNames { get; }
        void OnTileInvoked(PhotoItem item, bool ctrl, bool shift);
        void OnTileSelectToggled(PhotoItem item, bool shift);
        void OnTileContextRequested(PhotoItem item, FrameworkElement target, Point position);
        void OnHeaderSelectToggled(GalleryHeaderRow row);
    }

    /// <summary>
    /// A single thumbnail in the justified gallery with hover selection check, favorite badge and optional file name.
    /// </summary>
    public sealed partial class PhotoTile : ContentControl
    {
        private readonly Grid _root;
        private readonly Border _image;
        private readonly ImageBrush _brush;
        private readonly Border _shade;
        private readonly TextBlock _placeholder;
        private readonly Border _check;
        private readonly Path _checkMark;
        private readonly Path _favorite;
        private readonly Border _nameBar;
        private readonly TextBlock _name;
        private readonly Border _ring;
        private PhotoItem? _item;
        private IPhotoTileHost? _host;
        private CancellationTokenSource? _loadCts;
        private string? _loadedKey;
        private bool _hover;

        public PhotoTile()
        {
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            FocusVisualPrimaryBrush = WindowsAccent.Brush;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            CornerRadius = new CornerRadius(6);

            _brush = new ImageBrush { Stretch = Stretch.UniformToFill, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center };
            _image = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush")
            };
            var imageLayer = new Border { CornerRadius = new CornerRadius(6), Background = _brush };
            _image.Child = imageLayer;

            _shade = new Border
            {
                CornerRadius = new CornerRadius(6),
                Opacity = 0,
                IsHitTestVisible = false,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops =
                    {
                        new GradientStop { Color = Color.FromArgb(110, 0, 0, 0), Offset = 0 },
                        new GradientStop { Color = Color.FromArgb(0, 0, 0, 0), Offset = 0.35 }
                    }
                }
            };

            _checkMark = new Path
            {
                Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), "M4,12 L9.5,17.5 L20,6"),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2.5,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(5),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            _check = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8),
                Opacity = 0,
                Child = _checkMark
            };
            _check.Tapped += OnCheckTapped;

            _favorite = new Path
            {
                Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["DaisyIconHeart"]),
                Fill = new SolidColorBrush(Colors.White),
                Stretch = Stretch.Uniform,
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            _name = new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(8, 14, 8, 6),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _nameBar = new Border
            {
                CornerRadius = new CornerRadius(0, 0, 6, 6),
                VerticalAlignment = VerticalAlignment.Bottom,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops =
                    {
                        new GradientStop { Color = Color.FromArgb(0, 0, 0, 0), Offset = 0 },
                        new GradientStop { Color = Color.FromArgb(150, 0, 0, 0), Offset = 1 }
                    }
                },
                Child = _name
            };

            _ring = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(3),
                BorderBrush = WindowsAccent.Brush,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            // Skia: formats SkiaSharp cannot decode (HEIC, AVIF, TIFF, JXR) have no thumbnail; show the extension instead.
            _placeholder = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.5,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };

            _root = new Grid();
            _root.Children.Add(_image);
            _root.Children.Add(_placeholder);
            _root.Children.Add(_shade);
            _root.Children.Add(_nameBar);
            _root.Children.Add(_favorite);
            _root.Children.Add(_ring);
            _root.Children.Add(_check);
            Content = _root;

            PointerEntered += (_, _) => SetHover(true);
            PointerExited += (_, _) => SetHover(false);
            PointerCanceled += (_, _) => SetHover(false);
            Tapped += OnTapped;
            RightTapped += OnRightTapped;
            KeyDown += OnKeyDown;
            Unloaded += (_, _) => Detach();
        }

        public PhotoItem? Item => _item;

        public void SetItem(PhotoItem item, double width, double height, IPhotoTileHost host)
        {
            _host = host;
            Width = width;
            Height = height;

            if (!ReferenceEquals(_item, item))
            {
                Detach();
                _item = item;
                item.PropertyChanged += OnItemPropertyChanged;
                _name.Text = item.FileName;
                _placeholder.Text = item.Extension;
                _placeholder.Visibility = ThumbnailLoader.CanDecode(item.Extension) ? Visibility.Collapsed : Visibility.Visible;
                AutomationProperties.SetName(this, item.FileName);
            }

            UpdateState();
            LoadThumbnail(height);
        }

        public void Detach()
        {
            _loadCts?.Cancel();
            _loadCts = null;
            if (_item != null)
                _item.PropertyChanged -= OnItemPropertyChanged;
            _item = null;
        }

        private void LoadThumbnail(double height)
        {
            if (_item == null)
                return;

            var item = _item;
            var key = $"{item.Path}|{item.Revision}|{ThumbnailLoader.Bucket(height)}";
            if (key == _loadedKey)
                return;

            _loadCts?.Cancel();
            var cached = ThumbnailLoader.TryGetCached(item, height);
            if (cached != null)
            {
                _brush.ImageSource = cached;
                _loadedKey = key;
                return;
            }

            // Keep a lower-resolution version visible while the new bucket loads (zooming).
            if (_loadedKey == null || !_loadedKey.StartsWith(item.Path + "|", PathRules.Comparison))
                _brush.ImageSource = null;

            var cts = new CancellationTokenSource();
            _loadCts = cts;
            _ = LoadAsync(item, height, key, cts.Token);
        }

        private async System.Threading.Tasks.Task LoadAsync(PhotoItem item, double height, string key, CancellationToken token)
        {
            var image = await ThumbnailLoader.LoadAsync(item, height, token);
            if (token.IsCancellationRequested || !ReferenceEquals(item, _item) || image == null)
                return;
            _brush.ImageSource = image;
            _loadedKey = key;
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            DispatcherQueue?.TryEnqueue(UpdateState);
        }

        public void UpdateState()
        {
            if (_item == null || _host == null)
                return;

            var selected = _item.IsSelected;
            var selectionMode = _host.IsSelectionMode;

            _favorite.Visibility = _item.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
            _nameBar.Visibility = _host.ShowFileNames ? Visibility.Visible : Visibility.Collapsed;
            _ring.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            _image.Margin = selected ? new Thickness(6) : new Thickness(0);

            _check.Opacity = selected || selectionMode || _hover ? 1 : 0;
            _check.Background = selected
                ? WindowsAccent.Brush
                : new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
            _check.BorderBrush = selected ? WindowsAccent.Brush : new SolidColorBrush(Colors.White);
            _checkMark.Opacity = selected ? 1 : 0.55;
            _shade.Opacity = _hover || selectionMode ? 1 : 0;

            ToolTipService.SetToolTip(this, BuildTooltip(_item));
        }

        private static string BuildTooltip(PhotoItem item)
        {
            var culture = PhotosLocalization.CurrentCulture;
            var parts = new List<string> { item.FileName, item.SortDate.ToString("g", culture) };
            if (item.PixelWidth > 0)
                parts.Add($"{item.PixelWidth} × {item.PixelHeight}");
            parts.Add(FileOperations.FormatFileSize(item.FileSize));
            return string.Join(Environment.NewLine, parts);
        }

        private void SetHover(bool hover)
        {
            _hover = hover;
            UpdateState();
        }

        private static bool IsKeyDown(VirtualKey key) =>
            InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_item == null || _host == null)
                return;
            e.Handled = true;
            _host.OnTileInvoked(_item, IsKeyDown(VirtualKey.Control), IsKeyDown(VirtualKey.Shift));
        }

        private void OnCheckTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_item == null || _host == null)
                return;
            e.Handled = true;
            _host.OnTileSelectToggled(_item, IsKeyDown(VirtualKey.Shift));
        }

        private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (_item == null || _host == null)
                return;
            e.Handled = true;
            _host.OnTileContextRequested(_item, this, e.GetPosition(this));
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_item == null || _host == null)
                return;

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    e.Handled = true;
                    _host.OnTileInvoked(_item, false, false);
                    break;
                case VirtualKey.Space:
                    e.Handled = true;
                    _host.OnTileSelectToggled(_item, IsKeyDown(VirtualKey.Shift));
                    break;
                case VirtualKey.Application:
                    e.Handled = true;
                    _host.OnTileContextRequested(_item, this, new Point(ActualWidth / 2, ActualHeight / 2));
                    break;
            }
        }
    }
}
