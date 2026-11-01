using Flowery.Uno.Photos.Services;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>Text for the viewer's info panel, kept free of UI types so it can be tested.</summary>
    public static class ViewerFormat
    {
        /// <summary>Aperture, shutter time, ISO and focal length on one line, in the order the WinUI viewer shows them.</summary>
        public static string Exposure(ExifInfo exif, CultureInfo culture)
        {
            List<string> parts = [];
            if (exif.FNumber is > 0 and var f)
                parts.Add(string.Format(culture, "ƒ/{0:0.#}", f));
            if (exif.ExposureSeconds is > 0 and var time)
                parts.Add(time >= 1 ? string.Format(culture, "{0:0.#} s", time) : $"1/{Math.Round(1 / time)} s");
            if (exif.Iso is { } iso)
                parts.Add($"ISO {iso}");
            if (exif.FocalLengthMm is > 0 and var focal)
                parts.Add(string.Format(culture, "{0:0} mm", focal));
            return string.Join("  ·  ", parts);
        }
    }
}
