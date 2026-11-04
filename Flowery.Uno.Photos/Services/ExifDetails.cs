using System.Buffers.Binary;
using System.Text;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// Camera, lens, exposure and GPS details of a photo for the viewer's info panel.
    /// </summary>
    public sealed record ExifInfo(
        DateTime? DateTaken,
        string? Camera,
        string? Lens,
        double? FNumber,
        double? ExposureSeconds,
        int? Iso,
        double? FocalLengthMm,
        double? Latitude,
        double? Longitude);

    /// <summary>
    /// Reads the EXIF block of a JPEG by parsing the file directly. Skia: replaces StorageFile.Properties, which the Uno
    /// desktop heads do not implement (phase 0 spike). Only JPEG carries EXIF here; other formats return null and the
    /// info panel shows the file facts only.
    /// </summary>
    public static class ExifDetails
    {
        private const int MaxSegmentBytes = 256 * 1024;

        /// <summary>Returns null if the file is not a JPEG or has no EXIF block. Never throws for unreadable files.</summary>
        public static ExifInfo? Read(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var tiff = FindExifSegment(fs);
                return tiff == null ? null : Parse(tiff);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Parses a TIFF structure (the bytes after the "Exif\0\0" header). Returns null if it is not valid.</summary>
        public static ExifInfo? Parse(ReadOnlySpan<byte> tiff)
        {
            if (tiff.Length < 8 || !(tiff[0] == 'I' && tiff[1] == 'I' || tiff[0] == 'M' && tiff[1] == 'M'))
                return null;

            var little = tiff[0] == 'I';
            var ifd0 = ImageMetadataReader.ReadU32(tiff, 4, little);
            uint exifIfd = 0, gpsIfd = 0;
            string? make = null, model = null;

            foreach (var entry in ImageMetadataReader.EnumerateIfd(tiff, ifd0, little))
            {
                var raw = tiff.Slice(entry.EntryOffset + 8, 4);
                switch (entry.Tag)
                {
                    case 0x010F:
                        make = ImageMetadataReader.ReadAscii(tiff, entry.Count, entry.ValueOffset, raw);
                        break;
                    case 0x0110:
                        model = ImageMetadataReader.ReadAscii(tiff, entry.Count, entry.ValueOffset, raw);
                        break;
                    case 0x8769:
                        exifIfd = entry.ValueOffset;
                        break;
                    case 0x8825:
                        gpsIfd = entry.ValueOffset;
                        break;
                }
            }

            DateTime? taken = null;
            string? lens = null;
            double? fNumber = null, exposure = null, focal = null;
            int? iso = null;

            if (exifIfd > 0)
            {
                foreach (var entry in ImageMetadataReader.EnumerateIfd(tiff, exifIfd, little))
                {
                    var raw = tiff.Slice(entry.EntryOffset + 8, 4);
                    switch (entry.Tag)
                    {
                        case 0x9003 when taken == null:
                        case 0x9004 when taken == null:
                            var text = ImageMetadataReader.ReadAscii(tiff, entry.Count, entry.ValueOffset, raw);
                            if (text != null && DateTime.TryParseExact(text.Trim(), "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                                taken = parsed;
                            break;
                        case 0x829A:
                            exposure = ReadRational(tiff, entry.ValueOffset, 0, little);
                            break;
                        case 0x829D:
                            fNumber = ReadRational(tiff, entry.ValueOffset, 0, little);
                            break;
                        case 0x8827:
                            iso = ReadShort(raw, little);
                            break;
                        case 0x920A:
                            focal = ReadRational(tiff, entry.ValueOffset, 0, little);
                            break;
                        case 0xA434:
                            lens = ImageMetadataReader.ReadAscii(tiff, entry.Count, entry.ValueOffset, raw);
                            break;
                    }
                }
            }

            double? latitude = null, longitude = null;
            if (gpsIfd > 0)
                (latitude, longitude) = ReadGps(tiff, gpsIfd, little);

            string? camera = null;
            if (!string.IsNullOrWhiteSpace(model))
            {
                camera = !string.IsNullOrWhiteSpace(make) && !model.StartsWith(make, StringComparison.OrdinalIgnoreCase)
                    ? $"{make.Trim()} {model.Trim()}"
                    : model.Trim();
            }

            return new ExifInfo(taken, camera, string.IsNullOrWhiteSpace(lens) ? null : lens.Trim(), fNumber, exposure, iso, focal, latitude, longitude);
        }

        private static (double? Latitude, double? Longitude) ReadGps(ReadOnlySpan<byte> tiff, uint gpsIfd, bool little)
        {
            string? latRef = null, lonRef = null;
            double? lat = null, lon = null;
            foreach (var entry in ImageMetadataReader.EnumerateIfd(tiff, gpsIfd, little))
            {
                var raw = tiff.Slice(entry.EntryOffset + 8, 4);
                switch (entry.Tag)
                {
                    case 1:
                        latRef = ImageMetadataReader.ReadAscii(tiff, entry.Count, entry.ValueOffset, raw);
                        break;
                    case 2:
                        lat = ReadDegrees(tiff, entry.ValueOffset, little);
                        break;
                    case 3:
                        lonRef = ImageMetadataReader.ReadAscii(tiff, entry.Count, entry.ValueOffset, raw);
                        break;
                    case 4:
                        lon = ReadDegrees(tiff, entry.ValueOffset, little);
                        break;
                }
            }

            if (lat == null || lon == null)
                return (null, null);
            if (string.Equals(latRef, "S", StringComparison.OrdinalIgnoreCase))
                lat = -lat;
            if (string.Equals(lonRef, "W", StringComparison.OrdinalIgnoreCase))
                lon = -lon;
            return (lat, lon);
        }

        /// <summary>Degrees, minutes and seconds as three rationals.</summary>
        private static double? ReadDegrees(ReadOnlySpan<byte> tiff, uint offset, bool little)
        {
            var d = ReadRational(tiff, offset, 0, little);
            var m = ReadRational(tiff, offset, 1, little);
            var s = ReadRational(tiff, offset, 2, little);
            return d == null || m == null || s == null ? null : d + m / 60 + s / 3600;
        }

        /// <summary>The <paramref name="index"/>-th rational at <paramref name="offset"/>; null if outside the data or the denominator is 0.</summary>
        private static double? ReadRational(ReadOnlySpan<byte> tiff, uint offset, int index, bool little)
        {
            var position = (long)offset + index * 8L;
            if (position + 8 > tiff.Length)
                return null;
            var numerator = ImageMetadataReader.ReadU32(tiff, (int)position, little);
            var denominator = ImageMetadataReader.ReadU32(tiff, (int)position + 4, little);
            return denominator == 0 ? null : (double)numerator / denominator;
        }

        private static int? ReadShort(ReadOnlySpan<byte> raw, bool little)
        {
            var value = little ? raw[0] | (raw[1] << 8) : (raw[0] << 8) | raw[1];
            return value > 0 ? value : null;
        }

        /// <summary>Returns the TIFF part of the Exif APP1 segment, or null.</summary>
        private static byte[]? FindExifSegment(FileStream fs)
        {
            Span<byte> head = stackalloc byte[4];
            if (fs.Read(head[..2]) < 2 || head[0] != 0xFF || head[1] != 0xD8)
                return null;

            while (fs.Position < fs.Length)
            {
                if (fs.Read(head[..2]) < 2 || head[0] != 0xFF)
                    return null;

                var code = head[1];
                while (code == 0xFF)
                {
                    var next = fs.ReadByte();
                    if (next < 0)
                        return null;
                    code = (byte)next;
                }

                if (code is 0x01 || code is >= 0xD0 and <= 0xD8)
                    continue;
                if (code is 0xD9 or 0xDA)
                    return null;

                if (fs.Read(head[..2]) < 2)
                    return null;
                var length = BinaryPrimitives.ReadUInt16BigEndian(head) - 2;
                if (length < 0)
                    return null;
                var start = fs.Position;

                if (code == 0xE1 && length > 14)
                {
                    var data = new byte[Math.Min(length, MaxSegmentBytes)];
                    var got = fs.Read(data, 0, data.Length);
                    if (got > 14 && Encoding.ASCII.GetString(data, 0, 4) == "Exif")
                        return data.AsSpan(6, got - 6).ToArray();
                }

                fs.Position = start + length;
            }

            return null;
        }
    }
}
