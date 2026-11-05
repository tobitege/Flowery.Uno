using System.Buffers.Binary;
using Flowery.Uno.Photos.Editing;
using SkiaSharp;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>
    /// Carries the EXIF block of a JPEG over to the JPEG saved from its edit, so camera, lens, capture date and GPS stay.
    /// SkiaSharp writes no metadata of its own. The edited pixels are upright and may be cropped, so the copy says
    /// orientation 1, gives the new pixel size, and drops the embedded thumbnail (IFD1), which would still show the photo
    /// as it was before the edit. Other formats are saved without EXIF.
    /// </summary>
    public static class ExifTransfer
    {
        private const ushort OrientationTag = 0x0112;
        private const ushort ExifIfdTag = 0x8769;
        private const ushort PixelXDimensionTag = 0xA002;
        private const ushort PixelYDimensionTag = 0xA003;
        private const ushort ShortType = 3;
        private const ushort LongType = 4;
        private static readonly byte[] ExifHeader = "Exif\0\0"u8.ToArray();

        /// <summary>
        /// Returns <paramref name="encoded"/> (the edit of <paramref name="sourcePath"/>, <paramref name="width"/> ×
        /// <paramref name="height"/> pixels) with the source's EXIF block when both files are JPEG; otherwise, and when the
        /// block is broken, unchanged: the metadata never stops a save.
        /// </summary>
        public static byte[] CarryOver(string sourcePath, string destinationPath, byte[] encoded, int width, int height)
        {
            // Only a JPEG source has an Exif segment (ReadTiff checks the file), so only the written format is checked here.
            if (EditRenderer.GetFormatForPath(destinationPath) != SKEncodedImageFormat.Jpeg || ExifDetails.ReadTiff(sourcePath) is not { } tiff)
                return encoded;
            return PrepareTiff(tiff, width, height) is { } prepared ? InsertExif(encoded, prepared) : encoded;
        }

        /// <summary>
        /// A copy of <paramref name="tiff"/> for the edited image: orientation 1, pixel size <paramref name="width"/> ×
        /// <paramref name="height"/>, no IFD1. Null if it is no valid TIFF structure.
        /// </summary>
        public static byte[]? PrepareTiff(ReadOnlySpan<byte> tiff, int width, int height)
        {
            if (tiff.Length < 8 || !(tiff[0] == 'I' && tiff[1] == 'I' || tiff[0] == 'M' && tiff[1] == 'M'))
                return null;

            var little = tiff[0] == 'I';
            var copy = tiff.ToArray();
            var ifd0 = ImageMetadataReader.ReadU32(copy, 4, little);
            if (ifd0 + 2L > copy.Length)
                return null;

            uint exifIfd = 0;
            foreach (var entry in ImageMetadataReader.EnumerateIfd(copy, ifd0, little))
            {
                if (entry.Tag == OrientationTag)
                    WriteValue(copy, entry, 1, little);
                else if (entry.Tag == ExifIfdTag)
                    exifIfd = entry.ValueOffset;
            }

            // The offset after IFD0's entries links to IFD1, the embedded thumbnail; 0 ends the chain there.
            var nextIfd = ifd0 + 2L + ImageMetadataReader.ReadU16(copy, (int)ifd0, little) * 12;
            if (nextIfd + 4 <= copy.Length)
                WriteU32(copy, (int)nextIfd, 0, little);

            if (exifIfd != 0)
            {
                foreach (var entry in ImageMetadataReader.EnumerateIfd(copy, exifIfd, little))
                {
                    if (entry.Tag == PixelXDimensionTag)
                        WriteValue(copy, entry, (uint)width, little);
                    else if (entry.Tag == PixelYDimensionTag)
                        WriteValue(copy, entry, (uint)height, little);
                }
            }

            return copy;
        }

        /// <summary>
        /// Inserts an Exif APP1 segment holding <paramref name="tiff"/> into <paramref name="jpeg"/>: after the JFIF APP0
        /// segment when there is one, otherwise right after the start marker. Returns <paramref name="jpeg"/> unchanged if
        /// it is no JPEG or the block does not fit into one segment.
        /// </summary>
        public static byte[] InsertExif(byte[] jpeg, ReadOnlySpan<byte> tiff)
        {
            var segmentLength = 2 + ExifHeader.Length + tiff.Length;
            if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8 || segmentLength > ushort.MaxValue)
                return jpeg;

            var at = 2;
            if (jpeg[2] == 0xFF && jpeg[3] == 0xE0 && jpeg.Length >= 6)
                at = Math.Min(jpeg.Length, 4 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(4)));

            var result = new byte[jpeg.Length + 2 + segmentLength];
            jpeg.AsSpan(0, at).CopyTo(result);
            result[at] = 0xFF;
            result[at + 1] = 0xE1;
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(at + 2), (ushort)segmentLength);
            ExifHeader.CopyTo(result, at + 4);
            tiff.CopyTo(result.AsSpan(at + 4 + ExifHeader.Length));
            jpeg.AsSpan(at).CopyTo(result.AsSpan(at + 2 + segmentLength));
            return result;
        }

        /// <summary>Overwrites the value of a one-value SHORT or LONG entry; other entries stay as they are.</summary>
        private static void WriteValue(byte[] tiff, ImageMetadataReader.IfdEntry entry, uint value, bool little)
        {
            var type = ImageMetadataReader.ReadU16(tiff, entry.EntryOffset + 2, little);
            var at = entry.EntryOffset + 8;
            if (entry.Count != 1)
                return;
            if (type == ShortType && value <= ushort.MaxValue)
            {
                if (little)
                    BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(at), (ushort)value);
                else
                    BinaryPrimitives.WriteUInt16BigEndian(tiff.AsSpan(at), (ushort)value);
            }
            else if (type == LongType)
            {
                WriteU32(tiff, at, value, little);
            }
        }

        private static void WriteU32(byte[] data, int offset, uint value, bool little)
        {
            if (little)
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
            else
                BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), value);
        }
    }
}
