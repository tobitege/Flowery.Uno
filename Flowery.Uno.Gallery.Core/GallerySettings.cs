using System;
using System.Collections.Generic;
using System.Globalization;
using Flowery.Controls;
using Flowery.Services;

namespace Flowery.Uno.Gallery
{
    /// <summary>
    /// Represents saved window position and size.
    /// </summary>
    public record WindowBounds(int X, int Y, int Width, int Height);

    public static class GallerySettings
    {
        private const string ThemeKey = "theme";
        private const string LanguageKey = "language";
        private const string SizeKey = "size";
        private const string WindowKey = "window";
        private const string CarouselKey = "carousel.settings";
        private const string CarouselTextFillKey = "carousel.textfill";
        private const string CarouselGlKey = "carousel.gl";

        /// <summary>
        /// Default window size: Half HD (960×540).
        /// </summary>
        public static readonly WindowBounds DefaultWindowBounds = new(0, 0, 960, 540);
        private static readonly CarouselSettings DefaultCarouselSettings = new(
            Enabled: true,
            ModeTag: "Slideshow",
            TransitionTag: "None",
            DurationTag: "0.4",
            EffectTag: "PanAndZoom",
            IntervalSeconds: 3m,
            Infinite: false,
            SliceCount: 8m,
            Stagger: true,
            StaggerMs: 50m,
            PixelateSize: 20m,
            DissolveDensity: 0.5m,
            FlipAngle: 90m);
        private static readonly CarouselTextFillSettings DefaultCarouselTextFillSettings = new(
            Enabled: true,
            Animate: false,
            AutoReverse: true,
            Duration: 6m,
            PanX: 0.25m,
            PanY: 0.1m,
            Scale: 1m,
            Rotation: 0m,
            OffsetX: 0m,
            OffsetY: 0m);
        private static readonly CarouselGlSettings DefaultCarouselGlSettings = new(
            Enabled: true,
            ModeTag: "Slideshow",
            TransitionTag: "Fade",
            DurationTag: "0.4",
            EffectTag: "None",
            IntervalSeconds: 3m,
            Infinite: false,
            SliceCount: 8m,
            Stagger: true,
            StaggerMs: 50m,
            PixelateSize: 20m,
            DissolveDensity: 0.5m,
            FlipAngle: 90m);

        public static string? Load()
        {
            var lines = StateStorageProvider.Instance.LoadLines(ThemeKey);
            var line = lines.Count > 0 ? lines[0] : null;
            return string.IsNullOrWhiteSpace(line) ? null : line.Trim();
        }

        public static void Save(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return;

            StateStorageProvider.Instance.SaveLines(ThemeKey, [themeName]);
        }

        public static string? LoadLanguage()
        {
            var lines = StateStorageProvider.Instance.LoadLines(LanguageKey);
            var line = lines.Count > 0 ? lines[0] : null;
            return string.IsNullOrWhiteSpace(line) ? null : line.Trim();
        }

        public static void SaveLanguage(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                return;

            StateStorageProvider.Instance.SaveLines(LanguageKey, [cultureName]);
        }

        public static DaisySize? LoadGlobalSize()
        {
            var lines = StateStorageProvider.Instance.LoadLines(SizeKey);
            var line = lines.Count > 0 ? lines[0] : null;
            if (string.IsNullOrWhiteSpace(line))
                return null;

            return Enum.TryParse(line.Trim(), ignoreCase: true, out DaisySize size) ? size : null;
        }

        public static void SaveGlobalSize(DaisySize size)
        {
            StateStorageProvider.Instance.SaveLines(SizeKey, [size.ToString()]);
        }

        /// <summary>
        /// Loads saved window bounds (position and size) from persistent storage.
        /// </summary>
        /// <returns>Saved window bounds, or null if not found or invalid.</returns>
        public static WindowBounds? LoadWindowBounds()
        {
            var lines = StateStorageProvider.Instance.LoadLines(WindowKey);
            if (lines.Count < 4)
                return null;

            try
            {
                var x = int.Parse(lines[0].Trim(), CultureInfo.InvariantCulture);
                var y = int.Parse(lines[1].Trim(), CultureInfo.InvariantCulture);
                var width = int.Parse(lines[2].Trim(), CultureInfo.InvariantCulture);
                var height = int.Parse(lines[3].Trim(), CultureInfo.InvariantCulture);

                // Validate reasonable bounds
                if (width < 100 || height < 100 || width > 10000 || height > 10000)
                    return null;

                return new WindowBounds(x, y, width, height);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Saves window bounds (position and size) to persistent storage.
        /// </summary>
        public static void SaveWindowBounds(WindowBounds bounds)
        {
            StateStorageProvider.Instance.SaveLines(WindowKey,
            [
                bounds.X.ToString(CultureInfo.InvariantCulture),
                bounds.Y.ToString(CultureInfo.InvariantCulture),
                bounds.Width.ToString(CultureInfo.InvariantCulture),
                bounds.Height.ToString(CultureInfo.InvariantCulture)
            ]);
        }

        public static CarouselSettings? LoadCarouselSettings()
        {
            var values = LoadKeyValueLines(CarouselKey);
            if (values.Count == 0)
                return null;

            var defaults = DefaultCarouselSettings;
            var intervalSeconds = GetDecimal(values, "Interval", defaults.IntervalSeconds);
            if (intervalSeconds < 1m)
            {
                intervalSeconds = defaults.IntervalSeconds;
            }
            return new CarouselSettings(
                Enabled: GetBool(values, "Enabled", defaults.Enabled),
                ModeTag: GetString(values, "Mode") ?? defaults.ModeTag,
                TransitionTag: GetString(values, "Transition") ?? defaults.TransitionTag,
                DurationTag: GetString(values, "Duration") ?? defaults.DurationTag,
                EffectTag: GetString(values, "Effect") ?? defaults.EffectTag,
                IntervalSeconds: intervalSeconds,
                Infinite: GetBool(values, "Infinite", defaults.Infinite),
                SliceCount: GetDecimal(values, "SliceCount", defaults.SliceCount),
                Stagger: GetBool(values, "Stagger", defaults.Stagger),
                StaggerMs: GetDecimal(values, "StaggerMs", defaults.StaggerMs),
                PixelateSize: GetDecimal(values, "PixelateSize", defaults.PixelateSize),
                DissolveDensity: GetDecimal(values, "DissolveDensity", defaults.DissolveDensity),
                FlipAngle: GetDecimal(values, "FlipAngle", defaults.FlipAngle));
        }

        public static void SaveCarouselSettings(CarouselSettings settings)
        {
            SaveKeyValueLines(CarouselKey,
            [
                new("Enabled", settings.Enabled ? "1" : "0"),
                new("Mode", settings.ModeTag),
                new("Transition", settings.TransitionTag),
                new("Duration", settings.DurationTag),
                new("Effect", settings.EffectTag),
                new("Interval", settings.IntervalSeconds.ToString(CultureInfo.InvariantCulture)),
                new("Infinite", settings.Infinite ? "1" : "0"),
                new("SliceCount", settings.SliceCount.ToString(CultureInfo.InvariantCulture)),
                new("Stagger", settings.Stagger ? "1" : "0"),
                new("StaggerMs", settings.StaggerMs.ToString(CultureInfo.InvariantCulture)),
                new("PixelateSize", settings.PixelateSize.ToString(CultureInfo.InvariantCulture)),
                new("DissolveDensity", settings.DissolveDensity.ToString(CultureInfo.InvariantCulture)),
                new("FlipAngle", settings.FlipAngle.ToString(CultureInfo.InvariantCulture))
            ]);
        }

        public static CarouselTextFillSettings? LoadCarouselTextFillSettings()
        {
            var values = LoadKeyValueLines(CarouselTextFillKey);
            if (values.Count == 0)
                return null;

            var defaults = DefaultCarouselTextFillSettings;
            return new CarouselTextFillSettings(
                Enabled: GetBool(values, "Enabled", defaults.Enabled),
                Animate: GetBool(values, "Animate", defaults.Animate),
                AutoReverse: GetBool(values, "AutoReverse", defaults.AutoReverse),
                Duration: GetDecimal(values, "Duration", defaults.Duration),
                PanX: GetDecimal(values, "PanX", defaults.PanX),
                PanY: GetDecimal(values, "PanY", defaults.PanY),
                Scale: GetDecimal(values, "Scale", defaults.Scale),
                Rotation: GetDecimal(values, "Rotation", defaults.Rotation),
                OffsetX: GetDecimal(values, "OffsetX", defaults.OffsetX),
                OffsetY: GetDecimal(values, "OffsetY", defaults.OffsetY));
        }

        public static void SaveCarouselTextFillSettings(CarouselTextFillSettings settings)
        {
            SaveKeyValueLines(CarouselTextFillKey,
            [
                new("Enabled", settings.Enabled ? "1" : "0"),
                new("Animate", settings.Animate ? "1" : "0"),
                new("AutoReverse", settings.AutoReverse ? "1" : "0"),
                new("Duration", settings.Duration.ToString(CultureInfo.InvariantCulture)),
                new("PanX", settings.PanX.ToString(CultureInfo.InvariantCulture)),
                new("PanY", settings.PanY.ToString(CultureInfo.InvariantCulture)),
                new("Scale", settings.Scale.ToString(CultureInfo.InvariantCulture)),
                new("Rotation", settings.Rotation.ToString(CultureInfo.InvariantCulture)),
                new("OffsetX", settings.OffsetX.ToString(CultureInfo.InvariantCulture)),
                new("OffsetY", settings.OffsetY.ToString(CultureInfo.InvariantCulture))
            ]);
        }

        public static CarouselGlSettings? LoadCarouselGlSettings()
        {
            var values = LoadKeyValueLines(CarouselGlKey);
            if (values.Count == 0)
                return null;

            var defaults = DefaultCarouselGlSettings;
            var intervalSeconds = GetDecimal(values, "Interval", defaults.IntervalSeconds);
            if (intervalSeconds < 1m)
            {
                intervalSeconds = defaults.IntervalSeconds;
            }
            return new CarouselGlSettings(
                Enabled: GetBool(values, "Enabled", defaults.Enabled),
                ModeTag: GetString(values, "Mode") ?? defaults.ModeTag,
                TransitionTag: GetString(values, "Transition") ?? defaults.TransitionTag,
                DurationTag: GetString(values, "Duration") ?? defaults.DurationTag,
                EffectTag: GetString(values, "Effect") ?? defaults.EffectTag,
                IntervalSeconds: intervalSeconds,
                Infinite: GetBool(values, "Infinite", defaults.Infinite),
                SliceCount: GetDecimal(values, "SliceCount", defaults.SliceCount),
                Stagger: GetBool(values, "Stagger", defaults.Stagger),
                StaggerMs: GetDecimal(values, "StaggerMs", defaults.StaggerMs),
                PixelateSize: GetDecimal(values, "PixelateSize", defaults.PixelateSize),
                DissolveDensity: GetDecimal(values, "DissolveDensity", defaults.DissolveDensity),
                FlipAngle: GetDecimal(values, "FlipAngle", defaults.FlipAngle));
        }

        public static void SaveCarouselGlSettings(CarouselGlSettings settings)
        {
            SaveKeyValueLines(CarouselGlKey,
            [
                new("Enabled", settings.Enabled ? "1" : "0"),
                new("Mode", settings.ModeTag),
                new("Transition", settings.TransitionTag),
                new("Duration", settings.DurationTag),
                new("Effect", settings.EffectTag),
                new("Interval", settings.IntervalSeconds.ToString(CultureInfo.InvariantCulture)),
                new("Infinite", settings.Infinite ? "1" : "0"),
                new("SliceCount", settings.SliceCount.ToString(CultureInfo.InvariantCulture)),
                new("Stagger", settings.Stagger ? "1" : "0"),
                new("StaggerMs", settings.StaggerMs.ToString(CultureInfo.InvariantCulture)),
                new("PixelateSize", settings.PixelateSize.ToString(CultureInfo.InvariantCulture)),
                new("DissolveDensity", settings.DissolveDensity.ToString(CultureInfo.InvariantCulture)),
                new("FlipAngle", settings.FlipAngle.ToString(CultureInfo.InvariantCulture))
            ]);
        }

        private static Dictionary<string, string> LoadKeyValueLines(string key)
        {
            var lines = StateStorageProvider.Instance.LoadLines(key);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var name = parts[0].Trim();
                if (name.Length == 0)
                    continue;

                values[name] = parts[1].Trim();
            }

            return values;
        }

        private static void SaveKeyValueLines(string key, IEnumerable<KeyValuePair<string, string?>> values)
        {
            var lines = new List<string>();
            foreach (var entry in values)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                var value = entry.Value ?? string.Empty;
                lines.Add($"{entry.Key}={value}");
            }

            StateStorageProvider.Instance.SaveLines(key, lines);
        }

        private static string? GetString(Dictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
        {
            if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return fallback;

            if (bool.TryParse(raw, out var parsed))
                return parsed;

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                return number != 0;

            return fallback;
        }

        private static decimal GetDecimal(Dictionary<string, string> values, string key, decimal fallback)
        {
            if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return fallback;

            return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }
    }

    public sealed record CarouselSettings(
        bool Enabled,
        string? ModeTag,
        string? TransitionTag,
        string? DurationTag,
        string? EffectTag,
        decimal IntervalSeconds,
        bool Infinite,
        decimal SliceCount,
        bool Stagger,
        decimal StaggerMs,
        decimal PixelateSize,
        decimal DissolveDensity,
        decimal FlipAngle);

    public sealed record CarouselTextFillSettings(
        bool Enabled,
        bool Animate,
        bool AutoReverse,
        decimal Duration,
        decimal PanX,
        decimal PanY,
        decimal Scale,
        decimal Rotation,
        decimal OffsetX,
        decimal OffsetY);

    public sealed record CarouselGlSettings(
        bool Enabled,
        string? ModeTag,
        string? TransitionTag,
        string? DurationTag,
        string? EffectTag,
        decimal IntervalSeconds,
        bool Infinite,
        decimal SliceCount,
        bool Stagger,
        decimal StaggerMs,
        decimal PixelateSize,
        decimal DissolveDensity,
        decimal FlipAngle);
}
