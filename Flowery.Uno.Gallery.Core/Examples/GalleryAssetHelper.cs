using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
#if (__WASM__ || HAS_UNO_WASM) && !(HAS_UNO_SKIA || __UNO_SKIA__ || __SKIA__)
using Uno.Foundation;
#endif

namespace Flowery.Uno.Gallery.Examples
{
    public static class GalleryAssetHelper
    {
        private static readonly ImageSource TileBlueSource = CreateImageSource("Assets/tile_blue.jpg");
        private static readonly ImageSource TileGreenSource = CreateImageSource("Assets/tile_green.jpg");
        private static readonly ImageSource TilePurpleSource = CreateImageSource("Assets/tile_purple.jpg");
        private static readonly ImageSource WallpaperBlueSource = CreateImageSource("Assets/wallpaper_blue.jpg");
        private static readonly ImageSource WallpaperGreenSource = CreateImageSource("Assets/wallpaper_green.jpg");
        private static readonly ImageSource WallpaperRedSource = CreateImageSource("Assets/wallpaper_red.jpg");
        private static readonly ImageSource WallpaperYellowSource = CreateImageSource("Assets/wallpaper_yellow.jpg");

        public static ImageSource TileBlue => TileBlueSource;
        public static ImageSource TileGreen => TileGreenSource;
        public static ImageSource TilePurple => TilePurpleSource;
        public static ImageSource WallpaperBlue => WallpaperBlueSource;
        public static ImageSource WallpaperGreen => WallpaperGreenSource;
        public static ImageSource WallpaperRed => WallpaperRedSource;
        public static ImageSource WallpaperYellow => WallpaperYellowSource;

        private static BitmapImage CreateImageSource(string assetPath)
        {
            var trimmed = assetPath.TrimStart('/');
            Uri uri;
#if (__WASM__ || HAS_UNO_WASM) && !(HAS_UNO_SKIA || __UNO_SKIA__ || __SKIA__)
            if (OperatingSystem.IsBrowser())
            {
                var origin = GetBrowserOrigin();
                if (!string.IsNullOrEmpty(origin))
                {
                    uri = new Uri($"{origin}/_framework/{trimmed}", UriKind.Absolute);
                    return new BitmapImage(uri);
                }
            }
#endif
            uri = new Uri($"ms-appx:///{trimmed}", UriKind.Absolute);
            return new BitmapImage(uri);
        }

#if (__WASM__ || HAS_UNO_WASM) && !(HAS_UNO_SKIA || __UNO_SKIA__ || __SKIA__)
        private static string? _browserOrigin;

        private static string? GetBrowserOrigin()
        {
            if (!OperatingSystem.IsBrowser())
            {
                return null;
            }

            if (_browserOrigin != null)
            {
                return _browserOrigin;
            }

            try
            {
                var origin = WebAssemblyRuntime.InvokeJS("window.location.origin");
                if (string.IsNullOrWhiteSpace(origin) || string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                _browserOrigin = origin.Trim().TrimEnd('/');
                return _browserOrigin;
            }
            catch
            {
                return null;
            }
        }
#endif
    }
}
