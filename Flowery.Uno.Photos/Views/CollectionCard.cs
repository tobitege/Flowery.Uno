using System.Threading;
using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Xaml.Automation;
using Windows.System;

namespace Flowery.Uno.Photos.Views
{
    public enum CollectionEntryKind
    {
        Album,
        Folder
    }

    public sealed class CollectionEntry
    {
        public required CollectionEntryKind Kind { get; init; }
        public required string Key { get; init; }
        public required string Title { get; init; }
        public string? Subtitle { get; init; }
        public int Count { get; init; }
        public PhotoItem? Cover { get; init; }
        public bool IsRootFolder { get; init; }
    }

    public interface ICollectionCardHost
    {
        void OnEntryInvoked(CollectionEntry entry);
        void OnEntryContextRequested(CollectionEntry entry, FrameworkElement target, Point position);
    }

    /// <summary>
    /// Album/folder card with cover image, title, subtitle and count badge.
    /// </summary>
    public sealed partial class CollectionCard : ContentControl
    {
        private readonly ImageBrush _brush = new() { Stretch = Stretch.UniformToFill };
        private readonly Border _cover;
        private readonly Border _coverImage;
        private readonly Path _placeholderIcon;
        private readonly TextBlock _title;
        private readonly TextBlock _subtitle;
        private readonly DaisyBadge _badge;
        private CollectionEntry? _entry;
        private ICollectionCardHost? _host;
        private CancellationTokenSource? _cts;

        public CollectionCard()
        {
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            FocusVisualPrimaryBrush = WindowsAccent.Brush;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            _placeholderIcon = new Path
            {
                Stretch = Stretch.Uniform,
                Width = 44,
                Height = 44,
                Opacity = 0.35,
                Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _coverImage = new Border { CornerRadius = new CornerRadius(12), Background = _brush };
            _badge = new DaisyBadge
            {
                Variant = DaisyBadgeVariant.Neutral,
                Size = DaisySize.Small,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10)
            };
            var coverGrid = new Grid();
            coverGrid.Children.Add(_placeholderIcon);
            coverGrid.Children.Add(_coverImage);
            coverGrid.Children.Add(_badge);
            _cover = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                BorderThickness = new Thickness(1),
                Child = coverGrid
            };

            _title = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(2, 10, 2, 0),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
            _subtitle = new TextBlock
            {
                FontSize = 12,
                Opacity = 0.6,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(2, 2, 2, 0),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };

            var root = new Grid { Background = new SolidColorBrush(Colors.Transparent) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.Children.Add(_cover);
            Grid.SetRow(_title, 1);
            Grid.SetRow(_subtitle, 2);
            root.Children.Add(_title);
            root.Children.Add(_subtitle);
            Content = root;

            PointerEntered += (_, _) => _cover.BorderBrush = WindowsAccent.Brush;
            PointerExited += (_, _) => _cover.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            Tapped += (_, e) =>
            {
                e.Handled = true;
                if (_entry != null)
                    _host?.OnEntryInvoked(_entry);
            };
            RightTapped += (_, e) =>
            {
                e.Handled = true;
                if (_entry != null)
                    _host?.OnEntryContextRequested(_entry, this, e.GetPosition(this));
            };
            KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Enter && _entry != null)
                {
                    e.Handled = true;
                    _host?.OnEntryInvoked(_entry);
                }
            };
        }

        public void SetEntry(CollectionEntry entry, ICollectionCardHost host)
        {
            _entry = entry;
            _host = host;
            _title.Text = entry.Title;
            _subtitle.Text = entry.Subtitle ?? string.Empty;
            _subtitle.Visibility = string.IsNullOrEmpty(entry.Subtitle) ? Visibility.Collapsed : Visibility.Visible;
            _badge.BadgeContent = entry.Count.ToString(Localization.PhotosLocalization.CurrentCulture);
            _placeholderIcon.Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry),
                UiHelpers.Icon(entry.Kind == CollectionEntryKind.Album ? "PhotosIconAlbum" : "DaisyIconFolder") ?? string.Empty);
            AutomationProperties.SetName(this, entry.Title);
            ToolTipService.SetToolTip(this, entry.Kind == CollectionEntryKind.Folder ? entry.Key : entry.Title);

            _cts?.Cancel();
            _brush.ImageSource = null;
            if (entry.Cover is { } cover)
            {
                var cts = new CancellationTokenSource();
                _cts = cts;
                _ = LoadCoverAsync(cover, cts.Token);
            }
        }

        private async System.Threading.Tasks.Task LoadCoverAsync(PhotoItem cover, CancellationToken token)
        {
            var image = await ThumbnailLoader.LoadAsync(cover, 300, token);
            if (!token.IsCancellationRequested && image != null)
                _brush.ImageSource = image;
        }
    }
}
