using System;
using System.Collections.Generic;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Helpers
{
    /// <summary>
    /// Loads and caches pattern assets for DaisyPatternedCard.
    /// Uses PNG files (auto-generated from SVGs by Uno.Resizetizer) for fast loading.
    /// Selects appropriate color variant (black/white) based on current theme.
    /// </summary>
    internal static class FloweryPatternSvgLoader
    {
        /// <summary>
        /// Gets the PNG asset path for a pattern, selecting the appropriate color folder.
        /// </summary>
        public static string? GetAssetPath(DaisyCardPattern pattern)
        {
            var fileName = GetPatternFileName(pattern);
            if (fileName == null) return null;

            // Select color based on theme - use white for dark themes, black for light themes
            var colorFolder = IsDarkTheme() ? "white" : "black";

            // Browser builds don't get PNG resizetizer outputs, so use SVG directly.
            if (OperatingSystem.IsBrowser())
            {
                return $"ms-appx:///Flowery.Uno/Assets/Patterns/{colorFolder}/{fileName}.svg";
            }

            // Use PNG format (scale-qualified assets are resolved automatically)
            return $"ms-appx:///Flowery.Uno/Assets/Patterns/{colorFolder}/{fileName}.png";
        }

        /// <summary>
        /// Gets the asset path (kept for backwards compatibility).
        /// </summary>
        public static string? GetSvgAssetPath(DaisyCardPattern pattern)
        {
            return GetAssetPath(pattern);
        }

        private static string? GetPatternFileName(DaisyCardPattern pattern)
        {
            return pattern switch
            {
                DaisyCardPattern.CarbonFiber => "carbon_fiber",
                DaisyCardPattern.Dots => "dots",
                DaisyCardPattern.Grid => "grid",
                DaisyCardPattern.Stripes => "stripes",
                DaisyCardPattern.Noise => "noise",
                DaisyCardPattern.Honeycomb => "honeycomb",
                DaisyCardPattern.Circuit => "circuit",
                DaisyCardPattern.Twill => "twill",
                DaisyCardPattern.DiamondPlate => "diamond_plate",
                DaisyCardPattern.Mesh => "mesh",
                DaisyCardPattern.Perforated => "perforated",
                DaisyCardPattern.Bumps => "bumps",
                DaisyCardPattern.Scales => "scales",
                _ => null
            };
        }

        private static bool IsDarkTheme()
        {
            // Check if we're using a dark theme via DaisyThemeManager
            var themeName = DaisyThemeManager.CurrentThemeName;
            if (!string.IsNullOrEmpty(themeName))
            {
                // Known dark themes
                var darkThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dark", "Dracula", "Night", "Sunset", "Coffee", "Forest",
                    "Synthwave", "Halloween", "Aqua", "Luxury", "Business",
                    "Night Owl", "Dim"
                };
                return darkThemes.Contains(themeName);
            }

            // Fallback: check system theme
            try
            {
                var rootElement = Application.Current?.Resources;
                if (rootElement != null && rootElement.TryGetValue("DaisyBase100Brush", out var brush))
                {
                    if (brush is SolidColorBrush scb)
                    {
                        // If base background is dark, it's a dark theme
                        var luminance = (0.299 * scb.Color.R + 0.587 * scb.Color.G + 0.114 * scb.Color.B) / 255;
                        return luminance < 0.5;
                    }
                }
            }
            catch { }

            return false; // Default to light theme
        }

        /// <summary>
        /// Creates a tiled canvas using PNG images.
        /// </summary>
        public static Canvas? CreateTiledCanvas(DaisyCardPattern pattern, double width, double height)
        {
            var assetPath = GetAssetPath(pattern);
            if (assetPath == null) return null;

            const double TileSize = 120;
            int tilesX = (int)Math.Ceiling(width / TileSize) + 1;
            int tilesY = (int)Math.Ceiling(height / TileSize) + 1;

            var canvas = new Canvas
            {
                Width = width,
                Height = height,
                IsHitTestVisible = false
            };

            // Create tiled PNG images
            for (int tx = 0; tx < tilesX; tx++)
            {
                for (int ty = 0; ty < tilesY; ty++)
                {
                    var imageSource = CreateImageSource(assetPath);

                    var image = new Image
                    {
                        Source = imageSource,
                        Width = TileSize,
                        Height = TileSize,
                        Stretch = Stretch.Fill,
                        IsHitTestVisible = false
                    };

                    Canvas.SetLeft(image, tx * TileSize);
                    Canvas.SetTop(image, ty * TileSize);
                    canvas.Children.Add(image);
                }
            }

            return canvas;
        }

        private static ImageSource CreateImageSource(string assetPath)
        {
            if (assetPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return new SvgImageSource(new Uri(assetPath));
            }

            return new BitmapImage(new Uri(assetPath));
        }

        /// <summary>
        /// Creates a tiled canvas using assets (kept for backwards compatibility).
        /// </summary>
        public static Canvas? CreateSvgTiledCanvas(DaisyCardPattern pattern, double width, double height)
        {
            return CreateTiledCanvas(pattern, width, height);
        }

        /// <summary>
        /// Check if a pattern has an asset available.
        /// </summary>
        public static bool HasSvgAsset(DaisyCardPattern pattern)
        {
            return pattern != DaisyCardPattern.None && GetPatternFileName(pattern) != null;
        }
    }
}
