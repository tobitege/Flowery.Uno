using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Flowery.Controls
{
    public sealed class DaisyTabPaletteSwatch
    {
        public DaisyTabPaletteColor Color { get; }
        public string Name { get; }
        public string? Hex { get; }

        public DaisyTabPaletteSwatch(DaisyTabPaletteColor color, string name, string? hex)
        {
            Color = color;
            Name = name;
            Hex = hex;
        }
    }

    public sealed partial class DaisyTabColorGridMenuItem : MenuFlyoutItem
    {
        private const int SwatchColumns = 6;
        private const double SwatchButtonSize = 22;
        private const double SwatchDotSize = 12;
        private static readonly SolidColorBrush DefaultStrokeBrush =
            new SolidColorBrush(Flowery.Helpers.FloweryColorHelpers.ColorFromHex("#9ca3af"));

        private Grid? _swatchGrid;
        private IReadOnlyList<DaisyTabPaletteSwatch> _swatches = Array.Empty<DaisyTabPaletteSwatch>();

        public DaisyTabItem? TargetItem { get; set; }
        public MenuFlyout? OwnerFlyout { get; set; }

        public IReadOnlyList<DaisyTabPaletteSwatch> Swatches
        {
            get => _swatches;
            set
            {
                _swatches = value ?? Array.Empty<DaisyTabPaletteSwatch>();
                BuildSwatches();
            }
        }

        public event EventHandler<DaisyTabPaletteColorEventArgs>? ColorSelected;

        public DaisyTabColorGridMenuItem()
        {
            DefaultStyleKey = typeof(DaisyTabColorGridMenuItem);
            IsTabStop = false;
        }

        public static IReadOnlyList<DaisyTabPaletteSwatch> CreateDefaultSwatches()
        {
            return DaisyTabPaletteDefinitions.GetSwatches();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _swatchGrid = GetTemplateChild("PART_SwatchGrid") as Grid;
            BuildSwatches();
        }

        private void BuildSwatches()
        {
            if (_swatchGrid == null) return;

            _swatchGrid.Children.Clear();
            _swatchGrid.RowDefinitions.Clear();
            _swatchGrid.ColumnDefinitions.Clear();

            int count = _swatches.Count;
            if (count == 0) return;

            int rows = (int)Math.Ceiling(count / (double)SwatchColumns);

            for (int column = 0; column < SwatchColumns; column++)
                _swatchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int row = 0; row < rows; row++)
                _swatchGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int i = 0; i < count; i++)
            {
                var swatch = _swatches[i];
                var button = CreateSwatchButton(swatch);
                Grid.SetRow(button, i / SwatchColumns);
                Grid.SetColumn(button, i % SwatchColumns);
                _swatchGrid.Children.Add(button);
            }
        }

        private Button CreateSwatchButton(DaisyTabPaletteSwatch swatch)
        {
            var button = new Button
            {
                Width = SwatchButtonSize,
                Height = SwatchButtonSize,
                Padding = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(SwatchButtonSize / 2),
                Tag = swatch.Color,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Brush? fill = null;
            Brush? stroke = null;
            if (!string.IsNullOrWhiteSpace(swatch.Hex))
            {
                fill = new SolidColorBrush(Flowery.Helpers.FloweryColorHelpers.ColorFromHex(swatch.Hex));
            }
            else
            {
                stroke = DefaultStrokeBrush;
            }

            var ellipse = new Ellipse
            {
                Width = SwatchDotSize,
                Height = SwatchDotSize,
                Fill = fill ?? new SolidColorBrush(Colors.Transparent),
                Stroke = stroke,
                StrokeThickness = stroke == null ? 0 : 1
            };

            button.Content = ellipse;
            ToolTipService.SetToolTip(button, swatch.Name);
            button.Click += OnSwatchButtonClick;
            return button;
        }

        private void OnSwatchButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not DaisyTabPaletteColor color)
                return;

            if (TargetItem == null)
                return;

            ColorSelected?.Invoke(this, new DaisyTabPaletteColorEventArgs(TargetItem, color));
            OwnerFlyout?.Hide();
        }
    }
}
