#if WINDOWS
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
#endif

namespace Flowery.Win2D
{
    public static class CompositionHelpers
    {
        public static CompositionGeometry? CreateMaskGeometry(Compositor compositor, DaisyMaskVariant variant, float width, float height)
        {
#if WINDOWS
            var device = CanvasDevice.GetSharedDevice();
            using var builder = new CanvasPathBuilder(device);

            switch (variant)
            {
                case DaisyMaskVariant.Heart:
                    BuildHeartPath(builder, width, height);
                    break;
                case DaisyMaskVariant.Hexagon:
                    BuildHexagonPath(builder, width, height);
                    break;
                case DaisyMaskVariant.Triangle:
                    BuildTrianglePath(builder, width, height);
                    break;
                case DaisyMaskVariant.Diamond:
                    BuildDiamondPath(builder, width, height);
                    break;
                default:
                    return null;
            }

            var canvasGeometry = CanvasGeometry.CreatePath(builder);
            var compositionPath = new CompositionPath(canvasGeometry);
            return compositor.CreatePathGeometry(compositionPath);
#else
            return null;
#endif
        }

#if WINDOWS
        private static void BuildHeartPath(CanvasPathBuilder builder, float width, float height)
        {
            var scaleX = width / 100f;
            var scaleY = height / 100f;

            builder.BeginFigure(new Vector2(50 * scaleX, 90 * scaleY));
            builder.AddCubicBezier(
                new Vector2(10 * scaleX, 50 * scaleY),
                new Vector2(10 * scaleX, 30 * scaleY),
                new Vector2(25 * scaleX, 15 * scaleY));
            builder.AddCubicBezier(
                new Vector2(40 * scaleX, 5 * scaleY),
                new Vector2(50 * scaleX, 10 * scaleY),
                new Vector2(50 * scaleX, 20 * scaleY));
            builder.AddCubicBezier(
                new Vector2(50 * scaleX, 10 * scaleY),
                new Vector2(60 * scaleX, 5 * scaleY),
                new Vector2(75 * scaleX, 15 * scaleY));
            builder.AddCubicBezier(
                new Vector2(90 * scaleX, 30 * scaleY),
                new Vector2(90 * scaleX, 50 * scaleY),
                new Vector2(50 * scaleX, 90 * scaleY));
            builder.EndFigure(CanvasFigureLoop.Closed);
        }

        private static void BuildHexagonPath(CanvasPathBuilder builder, float width, float height)
        {
            var scaleX = width / 100f;
            var scaleY = height / 100f;

            builder.BeginFigure(new Vector2(50 * scaleX, 0));
            builder.AddLine(new Vector2(100 * scaleX, 25 * scaleY));
            builder.AddLine(new Vector2(100 * scaleX, 75 * scaleY));
            builder.AddLine(new Vector2(50 * scaleX, 100 * scaleY));
            builder.AddLine(new Vector2(0, 75 * scaleY));
            builder.AddLine(new Vector2(0, 25 * scaleY));
            builder.EndFigure(CanvasFigureLoop.Closed);
        }

        private static void BuildTrianglePath(CanvasPathBuilder builder, float width, float height)
        {
            builder.BeginFigure(new Vector2(width / 2, 0));
            builder.AddLine(new Vector2(width, height));
            builder.AddLine(new Vector2(0, height));
            builder.EndFigure(CanvasFigureLoop.Closed);
        }

        private static void BuildDiamondPath(CanvasPathBuilder builder, float width, float height)
        {
            builder.BeginFigure(new Vector2(width / 2, 0));
            builder.AddLine(new Vector2(width, height / 2));
            builder.AddLine(new Vector2(width / 2, height));
            builder.AddLine(new Vector2(0, height / 2));
            builder.EndFigure(CanvasFigureLoop.Closed);
        }
#endif
    }
}
