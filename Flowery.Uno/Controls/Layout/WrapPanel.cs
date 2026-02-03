namespace Flowery.Controls
{
    /// <summary>
    /// A simple wrapping panel for WinUI/Uno (missing in some target stacks).
    /// </summary>
    public partial class WrapPanel : Panel
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(WrapPanel),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(WrapPanel),
                new PropertyMetadata(0d, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the spacing between items and between lines.
        /// This is overridden by HorizontalSpacing/VerticalSpacing if set.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public static readonly DependencyProperty HorizontalSpacingProperty =
            DependencyProperty.Register(
                nameof(HorizontalSpacing),
                typeof(double),
                typeof(WrapPanel),
                new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the horizontal spacing between items. Overrides Spacing for horizontal direction.
        /// </summary>
        public double HorizontalSpacing
        {
            get => (double)GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }

        public static readonly DependencyProperty VerticalSpacingProperty =
            DependencyProperty.Register(
                nameof(VerticalSpacing),
                typeof(double),
                typeof(WrapPanel),
                new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the vertical spacing between lines. Overrides Spacing for vertical direction.
        /// </summary>
        public double VerticalSpacing
        {
            get => (double)GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }

        /// <summary>
        /// Gets the effective spacing between items in the primary direction.
        /// </summary>
        private double GetItemSpacing()
        {
            if (Orientation == Orientation.Horizontal)
                return double.IsNaN(HorizontalSpacing) ? Spacing : HorizontalSpacing;
            else
                return double.IsNaN(VerticalSpacing) ? Spacing : VerticalSpacing;
        }

        /// <summary>
        /// Gets the effective spacing between lines (secondary direction).
        /// </summary>
        private double GetLineSpacing()
        {
            if (Orientation == Orientation.Horizontal)
                return double.IsNaN(VerticalSpacing) ? Spacing : VerticalSpacing;
            else
                return double.IsNaN(HorizontalSpacing) ? Spacing : HorizontalSpacing;
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WrapPanel panel)
            {
                panel.InvalidateMeasure();
                panel.InvalidateArrange();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var itemSpacing = Math.Max(0, GetItemSpacing());
            var lineSpacing = Math.Max(0, GetLineSpacing());

            if (Orientation == Orientation.Horizontal)
            {
                double lineWidth = 0;
                double lineHeight = 0;
                double totalHeight = 0;
                double maxWidth = 0;
                bool anyInLine = false;

                foreach (var child in Children)
                {
                    child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                    var desired = child.DesiredSize;

                    var nextWidth = anyInLine ? lineWidth + itemSpacing + desired.Width : lineWidth + desired.Width;

                    // If we have a finite width and this would overflow, wrap to next line.
                    if (!double.IsInfinity(availableSize.Width) && anyInLine && nextWidth > availableSize.Width)
                    {
                        totalHeight += lineHeight;
                        totalHeight += lineSpacing;
                        maxWidth = Math.Max(maxWidth, lineWidth);

                        lineWidth = desired.Width;
                        lineHeight = desired.Height;
                        anyInLine = true;
                    }
                    else
                    {
                        lineWidth = nextWidth;
                        lineHeight = Math.Max(lineHeight, desired.Height);
                        anyInLine = true;
                    }
                }

                if (anyInLine)
                {
                    totalHeight += lineHeight;
                    maxWidth = Math.Max(maxWidth, lineWidth);
                }

                return new Size(maxWidth, totalHeight);
            }
            else
            {
                double lineHeight = 0;
                double lineWidth = 0;
                double totalWidth = 0;
                double maxHeight = 0;
                bool anyInLine = false;

                foreach (var child in Children)
                {
                    child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    var desired = child.DesiredSize;

                    var nextHeight = anyInLine ? lineHeight + itemSpacing + desired.Height : lineHeight + desired.Height;

                    if (!double.IsInfinity(availableSize.Height) && anyInLine && nextHeight > availableSize.Height)
                    {
                        totalWidth += lineWidth;
                        totalWidth += lineSpacing;
                        maxHeight = Math.Max(maxHeight, lineHeight);

                        lineHeight = desired.Height;
                        lineWidth = desired.Width;
                        anyInLine = true;
                    }
                    else
                    {
                        lineHeight = nextHeight;
                        lineWidth = Math.Max(lineWidth, desired.Width);
                        anyInLine = true;
                    }
                }

                if (anyInLine)
                {
                    totalWidth += lineWidth;
                    maxHeight = Math.Max(maxHeight, lineHeight);
                }

                return new Size(totalWidth, maxHeight);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var itemSpacing = Math.Max(0, GetItemSpacing());
            var lineSpacing = Math.Max(0, GetLineSpacing());

            if (Orientation == Orientation.Horizontal)
            {
                double x = 0;
                double y = 0;
                double lineHeight = 0;
                bool anyInLine = false;

                foreach (var child in Children)
                {
                    var desired = child.DesiredSize;
                    var nextX = anyInLine ? x + itemSpacing + desired.Width : x + desired.Width;

                    if (!double.IsInfinity(finalSize.Width) && anyInLine && nextX > finalSize.Width)
                    {
                        // wrap
                        x = 0;
                        y += lineHeight + lineSpacing;
                        lineHeight = 0;
                        anyInLine = false;
                    }

                    if (anyInLine)
                        x += itemSpacing;

                    child.Arrange(new Rect((float)x, (float)y, (float)desired.Width, (float)desired.Height));
                    x += desired.Width;
                    lineHeight = Math.Max(lineHeight, desired.Height);
                    anyInLine = true;
                }

                return new Size((float)finalSize.Width, (float)Math.Max(finalSize.Height, y + lineHeight));
            }
            else
            {
                double x = 0;
                double y = 0;
                double lineWidth = 0;
                bool anyInLine = false;

                foreach (var child in Children)
                {
                    var desired = child.DesiredSize;
                    var nextY = anyInLine ? y + itemSpacing + desired.Height : y + desired.Height;

                    if (!double.IsInfinity(finalSize.Height) && anyInLine && nextY > finalSize.Height)
                    {
                        // wrap
                        y = 0;
                        x += lineWidth + lineSpacing;
                        lineWidth = 0;
                        anyInLine = false;
                    }

                    if (anyInLine)
                        y += itemSpacing;

                    child.Arrange(new Rect((float)x, (float)y, (float)desired.Width, (float)desired.Height));
                    y += desired.Height;
                    lineWidth = Math.Max(lineWidth, desired.Width);
                    anyInLine = true;
                }

                return new Size((float)Math.Max(finalSize.Width, x + lineWidth), (float)finalSize.Height);
            }
        }
    }
}
