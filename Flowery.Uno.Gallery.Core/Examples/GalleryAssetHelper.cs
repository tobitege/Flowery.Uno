using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Uno.Gallery.Examples
{
    // Every access returns a new image: Uno 6.7's ImageBrush stays subscribed to its ImageSource until the source
    // changes, so a shared static image kept every page that painted it with an ImageBrush alive. Uno master
    // subscribes weakly since 9f71cb998 ("Let a long-lived source drop the brushes it painted").
    public static class GalleryAssetHelper
    {
        public static ImageSource TileBlue => CreateImageSource("Assets/tile_blue.jpg");
        public static ImageSource TileGreen => CreateImageSource("Assets/tile_green.jpg");
        public static ImageSource TilePurple => CreateImageSource("Assets/tile_purple.jpg");
        public static ImageSource WallpaperBlue => CreateImageSource("Assets/wallpaper_blue.jpg");
        public static ImageSource WallpaperGreen => CreateImageSource("Assets/wallpaper_green.jpg");
        public static ImageSource WallpaperRed => CreateImageSource("Assets/wallpaper_red.jpg");
        public static ImageSource WallpaperYellow => CreateImageSource("Assets/wallpaper_yellow.jpg");

        private static BitmapImage CreateImageSource(string assetPath)
        {
            var trimmed = assetPath.TrimStart('/');
            var uri = new Uri($"ms-appx:///{trimmed}", UriKind.Absolute);
            return new BitmapImage(uri);
        }
    }
}
