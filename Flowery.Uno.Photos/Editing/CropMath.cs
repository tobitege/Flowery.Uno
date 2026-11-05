using System.Collections.Immutable;
using System.Numerics;
using Windows.Foundation;

namespace Flowery.Uno.Photos.Editing
{
    public enum CropHandle
    {
        None,
        Move,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }

    /// <summary>
    /// Crop rectangle math of the viewer, on normalized rectangles. Skia: moved out of ViewerView so the tests need no UI.
    /// </summary>
    public static class CropMath
    {
        public const double HandleHitRadius = 16;

        /// <summary>Which handle of <paramref name="crop"/> lies under <paramref name="p"/>; <paramref name="image"/> is the image on screen.</summary>
        public static CropHandle HitTest(Rect image, Rect crop, Point p)
        {
            var r = image;
            var l = r.X + crop.X * r.Width;
            var tp = r.Y + crop.Y * r.Height;
            var rt = l + crop.Width * r.Width;
            var b = tp + crop.Height * r.Height;

            bool Near(double a, double v) => Math.Abs(a - v) <= HandleHitRadius;
            var nearL = Near(p.X, l);
            var nearR = Near(p.X, rt);
            var nearT = Near(p.Y, tp);
            var nearB = Near(p.Y, b);
            var withinX = p.X >= l - HandleHitRadius && p.X <= rt + HandleHitRadius;
            var withinY = p.Y >= tp - HandleHitRadius && p.Y <= b + HandleHitRadius;

            if (nearL && nearT) return CropHandle.TopLeft;
            if (nearR && nearT) return CropHandle.TopRight;
            if (nearL && nearB) return CropHandle.BottomLeft;
            if (nearR && nearB) return CropHandle.BottomRight;
            if (nearT && withinX) return CropHandle.Top;
            if (nearB && withinX) return CropHandle.Bottom;
            if (nearL && withinY) return CropHandle.Left;
            if (nearR && withinY) return CropHandle.Right;
            if (p.X > l && p.X < rt && p.Y > tp && p.Y < b) return CropHandle.Move;
            return CropHandle.None;
        }

        /// <summary>Applies a drag of (<paramref name="dx"/>, <paramref name="dy"/>), normalized, to <paramref name="start"/>.</summary>
        public static Rect Resize(Rect start, CropHandle handle, double dx, double dy, Size transformedSize, double? cropAspect)
        {
            const double minSize = 0.04;
            double left = start.X, top = start.Y, right = start.Right, bottom = start.Bottom;

            if (handle == CropHandle.Move)
            {
                var x = Math.Clamp(start.X + dx, 0, 1 - start.Width);
                var y = Math.Clamp(start.Y + dy, 0, 1 - start.Height);
                return new Rect(x, y, start.Width, start.Height);
            }

            if (handle is CropHandle.Left or CropHandle.TopLeft or CropHandle.BottomLeft)
                left = Math.Clamp(start.X + dx, 0, right - minSize);
            if (handle is CropHandle.Right or CropHandle.TopRight or CropHandle.BottomRight)
                right = Math.Clamp(start.Right + dx, left + minSize, 1);
            if (handle is CropHandle.Top or CropHandle.TopLeft or CropHandle.TopRight)
                top = Math.Clamp(start.Y + dy, 0, bottom - minSize);
            if (handle is CropHandle.Bottom or CropHandle.BottomLeft or CropHandle.BottomRight)
                bottom = Math.Clamp(start.Bottom + dy, top + minSize, 1);

            var rect = new Rect(left, top, right - left, bottom - top);
            return cropAspect is { } aspect ? EnforceAspect(rect, handle, aspect, transformedSize) : rect;
        }

        /// <summary>
        /// Adjusts a normalized crop rect to an output aspect ratio (width / height in pixels).
        /// The edge or corner opposite to the dragged handle stays fixed.
        /// </summary>
        public static Rect EnforceAspect(Rect rect, CropHandle handle, double aspect, Size transformedSize)
        {
            // Ratio of normalized width to normalized height that yields the requested pixel aspect.
            var ratio = aspect * transformedSize.Height / transformedSize.Width;
            var w = rect.Width;
            var h = rect.Height;

            if (handle is CropHandle.Top or CropHandle.Bottom)
                w = h * ratio;
            else
                h = w / ratio;

            // Fit into the unit square.
            if (w > 1) { w = 1; h = w / ratio; }
            if (h > 1) { h = 1; w = h * ratio; }

            double x, y;
            switch (handle)
            {
                case CropHandle.TopLeft:
                    x = rect.Right - w; y = rect.Bottom - h;
                    break;
                case CropHandle.TopRight:
                    x = rect.X; y = rect.Bottom - h;
                    break;
                case CropHandle.BottomLeft:
                    x = rect.Right - w; y = rect.Y;
                    break;
                case CropHandle.Left:
                case CropHandle.Right:
                    x = handle == CropHandle.Left ? rect.Right - w : rect.X;
                    y = rect.Y + (rect.Height - h) / 2;
                    break;
                case CropHandle.Top:
                case CropHandle.Bottom:
                    x = rect.X + (rect.Width - w) / 2;
                    y = handle == CropHandle.Top ? rect.Bottom - h : rect.Y;
                    break;
                default:
                    x = rect.X; y = rect.Y;
                    break;
            }

            // Shrink if pushed outside, keeping the aspect.
            if (x < 0) { w += x; x = 0; h = w / ratio; }
            if (y < 0) { h += y; y = 0; w = h * ratio; }
            if (x + w > 1) { w = 1 - x; h = w / ratio; }
            if (y + h > 1) { h = 1 - y; w = h * ratio; }

            return new Rect(x, y, Math.Max(0.02, w), Math.Max(0.02, h));
        }

        /// <summary>Largest centered crop with the given output aspect inside <paramref name="current"/>.</summary>
        public static Rect CreateAspectCrop(Rect current, double aspect, Size transformedSize)
        {
            var ratio = aspect * transformedSize.Height / transformedSize.Width;
            var w = current.Width;
            var h = w / ratio;
            if (h > current.Height)
            {
                h = current.Height;
                w = h * ratio;
            }
            return new Rect(current.X + (current.Width - w) / 2, current.Y + (current.Height - h) / 2, w, h);
        }

        public static Rect Mirror(Rect crop, bool horizontal) => horizontal
            ? new Rect(1 - crop.Right, crop.Y, crop.Width, crop.Height)
            : new Rect(crop.X, 1 - crop.Bottom, crop.Width, crop.Height);

        /// <summary>Rotates <paramref name="state"/> by quarter turns; the crop and the strokes stay on the same content.</summary>
        public static EditState Rotate(EditState state, int quarterTurns)
        {
            var rotation = ((state.Rotation + quarterTurns) % 4 + 4) % 4;
            var c = state.Crop;
            var clockwise = quarterTurns > 0;
            var crop = clockwise
                ? new Rect(1 - c.Y - c.Height, c.X, c.Height, c.Width)
                : new Rect(c.Y, 1 - c.X - c.Width, c.Height, c.Width);
            var strokes = state.Strokes.Select(s => s with
            {
                Points = [.. s.Points.Select(p => clockwise ? new Vector2(1 - p.Y, p.X) : new Vector2(p.Y, 1 - p.X))]
            }).ToImmutableList();
            return state with { Rotation = rotation, Crop = crop, Strokes = strokes };
        }
    }
}
