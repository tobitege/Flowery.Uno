using System;
using System.Collections.Generic;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// A grid control for displaying and selecting colors from a palette.
    /// Supports automatic font scaling when contained within a FloweryScaleManager.EnableScaling="True" container.
    /// </summary>
    public partial class DaisyColorGrid : DaisyBaseContentControl, IScalableControl
    {
        private const double BaseTextFontSize = 12.0;

        private readonly Dictionary<int, Border> _cells = new();
        private Canvas? _rootCanvas;
        private int _hotIndex = -1;
        private bool _lockUpdates;
        private ImageBrush? _checkerboardBrush;

        /// <inheritdoc/>
        public void ApplyScaleFactor(double scaleFactor)
        {
            FontSize = FloweryScaleManager.ApplyScale(BaseTextFontSize, 10.0, scaleFactor);
        }

        #region Dependency Properties

        /// <summary>
        /// Gets or sets the currently selected color.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(DaisyColorGrid),
                new PropertyMetadata(Colors.Black, OnColorPropertyChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the index of the selected color in the palette.
        /// </summary>
        public static readonly DependencyProperty ColorIndexProperty =
            DependencyProperty.Register(
                nameof(ColorIndex),
                typeof(int),
                typeof(DaisyColorGrid),
                new PropertyMetadata(-1, OnColorIndexPropertyChanged));

        public int ColorIndex
        {
            get => (int)GetValue(ColorIndexProperty);
            set => SetValue(ColorIndexProperty, value);
        }

        /// <summary>
        /// Gets or sets the color collection to display.
        /// </summary>
        public static readonly DependencyProperty PaletteProperty =
            DependencyProperty.Register(
                nameof(Palette),
                typeof(ColorCollection),
                typeof(DaisyColorGrid),
                new PropertyMetadata(null, OnPalettePropertyChanged));

        public ColorCollection? Palette
        {
            get => (ColorCollection?)GetValue(PaletteProperty);
            set => SetValue(PaletteProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom colors collection.
        /// </summary>
        public static readonly DependencyProperty CustomColorsProperty =
            DependencyProperty.Register(
                nameof(CustomColors),
                typeof(ColorCollection),
                typeof(DaisyColorGrid),
                new PropertyMetadata(null, OnCustomColorsPropertyChanged));

        public ColorCollection? CustomColors
        {
            get => (ColorCollection?)GetValue(CustomColorsProperty);
            set => SetValue(CustomColorsProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the custom colors section.
        /// </summary>
        public static readonly DependencyProperty ShowCustomColorsProperty =
            DependencyProperty.Register(
                nameof(ShowCustomColors),
                typeof(bool),
                typeof(DaisyColorGrid),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool ShowCustomColors
        {
            get => (bool)GetValue(ShowCustomColorsProperty);
            set => SetValue(ShowCustomColorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of each color cell.
        /// </summary>
        public static readonly DependencyProperty CellSizeProperty =
            DependencyProperty.Register(
                nameof(CellSize),
                typeof(Size),
                typeof(DaisyColorGrid),
                new PropertyMetadata(new Size(16, 16), OnLayoutPropertyChanged));

        public Size CellSize
        {
            get => (Size)GetValue(CellSizeProperty);
            set => SetValue(CellSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the spacing between cells.
        /// </summary>
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(Size),
                typeof(DaisyColorGrid),
                new PropertyMetadata(new Size(3, 3), OnLayoutPropertyChanged));

        public Size Spacing
        {
            get => (Size)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of columns in the grid.
        /// </summary>
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(DaisyColorGrid),
                new PropertyMetadata(16, OnLayoutPropertyChanged));

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, Math.Max(1, value));
        }

        /// <summary>
        /// Gets or sets the cell border color.
        /// </summary>
        public static readonly DependencyProperty CellBorderColorProperty =
            DependencyProperty.Register(
                nameof(CellBorderColor),
                typeof(Color),
                typeof(DaisyColorGrid),
                new PropertyMetadata(Color.FromArgb(255, 160, 160, 160), OnLayoutPropertyChanged));

        public Color CellBorderColor
        {
            get => (Color)GetValue(CellBorderColorProperty);
            set => SetValue(CellBorderColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the selection border color.
        /// </summary>
        public static readonly DependencyProperty SelectionBorderColorProperty =
            DependencyProperty.Register(
                nameof(SelectionBorderColor),
                typeof(Color),
                typeof(DaisyColorGrid),
                new PropertyMetadata(Color.FromArgb(255, 0, 120, 215), OnLayoutPropertyChanged));

        public Color SelectionBorderColor
        {
            get => (Color)GetValue(SelectionBorderColorProperty);
            set => SetValue(SelectionBorderColorProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to automatically add selected colors to custom colors.
        /// </summary>
        public static readonly DependencyProperty AutoAddColorsProperty =
            DependencyProperty.Register(
                nameof(AutoAddColors),
                typeof(bool),
                typeof(DaisyColorGrid),
                new PropertyMetadata(true));

        public bool AutoAddColors
        {
            get => (bool)GetValue(AutoAddColorsProperty);
            set => SetValue(AutoAddColorsProperty, value);
        }

        /// <summary>
        /// Gets or sets an optional callback invoked when the color changes.
        /// This provides a simpler alternative to the ColorChanged event.
        /// </summary>
        public static readonly DependencyProperty OnColorChangedProperty =
            DependencyProperty.Register(
                nameof(OnColorChanged),
                typeof(Action<Color>),
                typeof(DaisyColorGrid),
                new PropertyMetadata(null));

        public Action<Color>? OnColorChanged
        {
            get => (Action<Color>?)GetValue(OnColorChangedProperty);
            set => SetValue(OnColorChangedProperty, value);
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyColorGrid),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when the selected color changes.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        public DaisyColorGrid()
        {
            DefaultStyleKey = typeof(DaisyColorGrid);
            BuildVisualTree();

            Palette = ColorCollection.Paint;
            CustomColors = ColorCollection.CreateCustom(16);

            PointerExited += OnPointerExited;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorGrid grid)
            {
                grid.HandleColorChanged((Color)e.NewValue);
            }
        }

        private static void OnColorIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorGrid grid)
            {
                grid.HandleColorIndexChanged((int)e.NewValue);
            }
        }

        private static void OnPalettePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorGrid grid)
            {
                grid.HandlePaletteChanged(e.OldValue as ColorCollection, e.NewValue as ColorCollection);
            }
        }

        private static void OnCustomColorsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorGrid grid)
            {
                grid.HandleCustomColorsChanged(e.OldValue as ColorCollection, e.NewValue as ColorCollection);
            }
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorGrid grid)
            {
                grid.RebuildCells();
            }
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorGrid grid)
            {
                grid.UpdateAutomationProperties();
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || GetTotalColorCount() == 0)
                return;

            var handled = true;
            var delta = 0;
            switch (e.Key)
            {
                case VirtualKey.Left:
                    delta = -1;
                    break;
                case VirtualKey.Right:
                    delta = 1;
                    break;
                case VirtualKey.Up:
                    delta = -Columns;
                    break;
                case VirtualKey.Down:
                    delta = Columns;
                    break;
                case VirtualKey.Home:
                    SetSelectionIndex(0);
                    break;
                case VirtualKey.End:
                    SetSelectionIndex(GetTotalColorCount() - 1);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    SetSelectionIndex(Math.Max(0, ColorIndex));
                    break;
                default:
                    handled = false;
                    break;
            }

            if (delta != 0)
            {
                SetSelectionIndex(Math.Clamp(Math.Max(0, ColorIndex) + delta, 0, GetTotalColorCount() - 1));
            }

            e.Handled = handled;
        }

        private void BuildVisualTree()
        {
            _rootCanvas = new Canvas();
            Content = _rootCanvas;
            RebuildCells();
        }

        private void HandlePaletteChanged(ColorCollection? oldPalette, ColorCollection? newPalette)
        {
            if (oldPalette != null)
                oldPalette.CollectionChanged -= OnPaletteCollectionChanged;

            if (newPalette != null)
                newPalette.CollectionChanged += OnPaletteCollectionChanged;

            RebuildCells();
        }

        private void HandleCustomColorsChanged(ColorCollection? oldCustom, ColorCollection? newCustom)
        {
            if (oldCustom != null)
                oldCustom.CollectionChanged -= OnCustomColorsCollectionChanged;

            if (newCustom != null)
                newCustom.CollectionChanged += OnCustomColorsCollectionChanged;

            RebuildCells();
        }

        private void OnPaletteCollectionChanged(object? sender, EventArgs e)
        {
            RebuildCells();
        }

        private void OnCustomColorsCollectionChanged(object? sender, EventArgs e)
        {
            RebuildCells();
        }

        private void HandleColorChanged(Color color)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                var index = FindColorIndex(color);
                if (index != ColorIndex)
                {
                    ColorIndex = index;
                }
                UpdateSelectionHighlight();
                OnColorChangedRaised(new ColorChangedEventArgs(color));
            }
            finally
            {
                _lockUpdates = false;
            }
            UpdateAutomationProperties();
        }

        private void HandleColorIndexChanged(int index)
        {
            if (_lockUpdates) return;

            _lockUpdates = true;
            try
            {
                var color = GetColorAtIndex(index);
                if (color.HasValue && color.Value != Color)
                {
                    Color = color.Value;
                }
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateSelectionHighlight();
            UpdateAutomationProperties();
        }

        private int GetTotalColorCount()
        {
            int count = Palette?.Count ?? 0;
            if (ShowCustomColors && CustomColors != null)
                count += CustomColors.Count;
            return count;
        }

        private int FindColorIndex(Color color)
        {
            int index = 0;

            if (Palette != null)
            {
                int found = Palette.Find(color, 0);
                if (found >= 0) return found;
                index = Palette.Count;
            }

            if (ShowCustomColors && CustomColors != null)
            {
                int found = CustomColors.Find(color, 0);
                if (found >= 0) return index + found;
            }

            return -1;
        }

        private Color? GetColorAtIndex(int index)
        {
            if (Palette != null)
            {
                if (index < Palette.Count)
                    return Palette[index];
                index -= Palette.Count;
            }

            if (ShowCustomColors && CustomColors != null && index >= 0 && index < CustomColors.Count)
            {
                return CustomColors[index];
            }

            return null;
        }

        private void RebuildCells()
        {
            if (_rootCanvas == null)
                return;

            _rootCanvas.Children.Clear();
            _cells.Clear();

            int totalColors = GetTotalColorCount();
            if (totalColors == 0)
                return;

            double cellWidth = CellSize.Width;
            double cellHeight = CellSize.Height;
            double spacingX = Spacing.Width;
            double spacingY = Spacing.Height;

            if (cellWidth <= 0 || cellHeight <= 0)
                return;

            int rows = (int)Math.Ceiling(totalColors / (double)Columns);
            double width = Columns * (cellWidth + spacingX) - spacingX;
            double height = rows * (cellHeight + spacingY) - spacingY;

            _rootCanvas.Width = Math.Max(0, width);
            _rootCanvas.Height = Math.Max(0, height);

            BuildCheckerboardBrush();

            int index = 0;
            double x = 0;
            double y = 0;

            if (Palette != null)
            {
                foreach (var color in Palette)
                {
                    AddCell(index, color, x, y);
                    index++;
                    x += cellWidth + spacingX;
                    if (index % Columns == 0)
                    {
                        x = 0;
                        y += cellHeight + spacingY;
                    }
                }
            }

            if (ShowCustomColors && CustomColors != null)
            {
                foreach (var color in CustomColors)
                {
                    AddCell(index, color, x, y);
                    index++;
                    x += cellWidth + spacingX;
                    if (index % Columns == 0)
                    {
                        x = 0;
                        y += cellHeight + spacingY;
                    }
                }
            }

            if (!_lockUpdates)
            {
                _lockUpdates = true;
                try
                {
                    ColorIndex = FindColorIndex(Color);
                }
                finally
                {
                    _lockUpdates = false;
                }
            }

            UpdateSelectionHighlight();
            UpdateAutomationProperties();
        }

        private void BuildCheckerboardBrush()
        {
            int width = Math.Max(1, (int)Math.Round(CellSize.Width));
            int height = Math.Max(1, (int)Math.Round(CellSize.Height));
            var bitmap = ColorPickerRendering.CreateCheckerboardBitmap(
                width,
                height,
                4,
                Colors.White,
                Color.FromArgb(255, 204, 204, 204));

            _checkerboardBrush = new ImageBrush
            {
                ImageSource = bitmap,
                Stretch = Stretch.Fill
            };
        }

        private void AddCell(int index, Color color, double x, double y)
        {
            if (_rootCanvas == null)
                return;

            var cellBorder = new Border
            {
                Width = CellSize.Width,
                Height = CellSize.Height,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(CellBorderColor)
            };

            if (color.A < 255 && _checkerboardBrush != null)
            {
                cellBorder.Background = _checkerboardBrush;
            }

            var colorOverlay = new Border
            {
                Background = new SolidColorBrush(color)
            };

            cellBorder.Child = colorOverlay;
            cellBorder.Tag = index;

            cellBorder.PointerEntered += OnCellPointerEntered;
            cellBorder.PointerExited += OnCellPointerExited;
            cellBorder.PointerPressed += OnCellPointerPressed;

            Canvas.SetLeft(cellBorder, x);
            Canvas.SetTop(cellBorder, y);

            _rootCanvas.Children.Add(cellBorder);
            _cells[index] = cellBorder;
        }

        private void OnCellPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int index)
            {
                _hotIndex = index;
                UpdateSelectionHighlight();
            }
        }

        private void OnCellPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int index && _hotIndex == index)
            {
                _hotIndex = -1;
                UpdateSelectionHighlight();
            }
        }

        private void OnCellPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int index)
            {
                var color = GetColorAtIndex(index);
                if (color.HasValue)
                {
                    ColorIndex = index;
                    Color = color.Value;
                }
                e.Handled = true;
                DaisyAccessibility.FocusOnPointer(this);
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_hotIndex != -1)
            {
                _hotIndex = -1;
                UpdateSelectionHighlight();
            }
        }

        private void UpdateSelectionHighlight()
        {
            foreach (var kvp in _cells)
            {
                int index = kvp.Key;
                var cell = kvp.Value;

                if (index == ColorIndex)
                {
                    cell.BorderThickness = new Thickness(2);
                    cell.BorderBrush = new SolidColorBrush(SelectionBorderColor);
                }
                else if (index == _hotIndex)
                {
                    cell.BorderThickness = new Thickness(1);
                    cell.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    cell.BorderThickness = new Thickness(1);
                    cell.BorderBrush = new SolidColorBrush(CellBorderColor);
                }
            }
        }

        /// <summary>
        /// Adds a color to the custom colors collection.
        /// </summary>
        public void AddCustomColor(Color color)
        {
            CustomColors ??= ColorCollection.CreateCustom(16);

            for (int i = CustomColors.Count - 1; i > 0; i--)
            {
                CustomColors[i] = CustomColors[i - 1];
            }

            if (CustomColors.Count > 0)
            {
                CustomColors[0] = color;
            }
            else
            {
                CustomColors.Add(color);
            }

            RebuildCells();
        }

        private void SetSelectionIndex(int index)
        {
            if (index < 0 || index >= GetTotalColorCount())
                return;

            ColorIndex = index;
            var color = GetColorAtIndex(index);
            if (color.HasValue)
            {
                Color = color.Value;
            }
        }

        private void UpdateAutomationProperties()
        {
            var name = AccessibleText;
            if (string.IsNullOrWhiteSpace(name))
            {
                var color = Color;
                name = $"Color grid #{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        protected virtual void OnColorChangedRaised(ColorChangedEventArgs e)
        {
            ColorChanged?.Invoke(this, e);
            OnColorChanged?.Invoke(e.Color);
        }
    }
}

