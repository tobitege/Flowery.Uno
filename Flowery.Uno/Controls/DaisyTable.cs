using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A Table control styled after DaisyUI's Table component.
    /// Contains DaisyTableRow items which contain DaisyTableCell items.
    /// </summary>
    public partial class DaisyTable : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private StackPanel? _rowsPanel;
        private readonly List<DaisyTableRow> _rows = [];

        public DaisyTable()
        {
            DefaultStyleKey = typeof(DaisyTable);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyTable),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ZebraProperty =
            DependencyProperty.Register(
                nameof(Zebra),
                typeof(bool),
                typeof(DaisyTable),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, alternating rows have different background colors.
        /// </summary>
        public bool Zebra
        {
            get => (bool)GetValue(ZebraProperty);
            set => SetValue(ZebraProperty, value);
        }

        public static readonly DependencyProperty PinRowsProperty =
            DependencyProperty.Register(
                nameof(PinRows),
                typeof(bool),
                typeof(DaisyTable),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, header and footer rows are sticky.
        /// </summary>
        public bool PinRows
        {
            get => (bool)GetValue(PinRowsProperty);
            set => SetValue(PinRowsProperty, value);
        }

        public static readonly DependencyProperty PinColsProperty =
            DependencyProperty.Register(
                nameof(PinCols),
                typeof(bool),
                typeof(DaisyTable),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, first column is sticky.
        /// </summary>
        public bool PinCols
        {
            get => (bool)GetValue(PinColsProperty);
            set => SetValue(PinColsProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTable table)
            {
                table.ApplyAll();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootBorder != null)
            {
                ApplyAll();
                return;
            }

            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            var userContent = Content;
            Content = null;

            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(8)
            };

            _rowsPanel = new StackPanel();

            // Collect rows from user content
            _rows.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);

                    if (child is DaisyTableRow row)
                    {
                        _rows.Add(row);
                        _rowsPanel.Children.Add(row);
                    }
                    else
                    {
                        _rowsPanel.Children.Add(child);
                    }
                }
            }

            _rootBorder.Child = _rowsPanel;
            Content = _rootBorder;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_rootBorder == null)
                return;

            // Check for lightweight styling overrides
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTable", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTable", "BorderBrush");

            _rootBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            _rootBorder.BorderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _rootBorder.BorderThickness = new Thickness(1);

            // Apply size and zebra to rows
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.Size = Size;

                if (Zebra && i % 2 == 1 && !row.IsActive)
                {
                    row.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                }
                else if (!row.IsActive)
                {
                    row.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// A table row (tr equivalent).
    /// </summary>
    public partial class DaisyTableRow : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private Grid? _cellsGrid;
        private readonly List<UIElement> _cells = [];
        private bool _isHovering;

        public DaisyTableRow()
        {
            DefaultStyleKey = typeof(DaisyTableRow);
            IsTabStop = false;

            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyTableRow),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(DaisyTableRow),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, this row is highlighted as selected/active.
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty HighlightOnHoverProperty =
            DependencyProperty.Register(
                nameof(HighlightOnHover),
                typeof(bool),
                typeof(DaisyTableRow),
                new PropertyMetadata(true));

        /// <summary>
        /// When true, this row highlights on hover.
        /// </summary>
        public bool HighlightOnHover
        {
            get => (bool)GetValue(HighlightOnHoverProperty);
            set => SetValue(HighlightOnHoverProperty, value);
        }

        public static readonly DependencyProperty IsHeaderProperty =
            DependencyProperty.Register(
                nameof(IsHeader),
                typeof(bool),
                typeof(DaisyTableRow),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, this row is styled as a header row.
        /// </summary>
        public bool IsHeader
        {
            get => (bool)GetValue(IsHeaderProperty);
            set => SetValue(IsHeaderProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTableRow row)
            {
                row.ApplyAll();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootBorder != null)
            {
                ApplyAll();
                return;
            }

            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (HighlightOnHover && !IsActive)
            {
                _isHovering = true;
                ApplyHoverState();
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isHovering = false;
            ApplyHoverState();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            var userContent = Content;
            Content = null;

            _rootBorder = new Border
            {
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            _cellsGrid = new Grid();

            // Collect cells from user content
            _cells.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);
                    _cells.Add(child);
                }
            }
            else if (userContent != null)
            {
                _cells.Add(userContent as UIElement ?? new Border());
            }

            // Create columns for each cell
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];

                ColumnDefinition colDef;
                if (cell is DaisyTableCell tableCell && tableCell.ColumnWidth.HasValue)
                {
                    colDef = new ColumnDefinition { Width = new GridLength(tableCell.ColumnWidth.Value) };
                }
                else
                {
                    colDef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                }

                _cellsGrid.ColumnDefinitions.Add(colDef);
                if (cell is FrameworkElement fe)
                {
                    Grid.SetColumn(fe, i);
                }
                _cellsGrid.Children.Add(cell);
            }

            _rootBorder.Child = _cellsGrid;
            Content = _rootBorder;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_rootBorder == null)
                return;

            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_rootBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            // Table rows use padding similar to menu items
            Thickness padding = resources == null
                ? DaisyResourceLookup.GetDefaultMenuPadding(Size)
                : DaisyResourceLookup.GetThickness(resources, $"DaisyMenu{sizeKey}Padding",
                    DaisyResourceLookup.GetDefaultMenuPadding(Size));
            double fontSize = resources == null
                ? DaisyResourceLookup.GetDefaultFontSize(Size)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}FontSize",
                    DaisyResourceLookup.GetDefaultFontSize(Size));

            _rootBorder.Padding = padding;
            FontSize = fontSize;

            if (IsHeader)
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            }
        }

        private void ApplyColors()
        {
            if (_rootBorder == null)
                return;

            // Check for lightweight styling overrides
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTableRow", "BorderBrush");
            var activeBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTableRow", "ActiveBackground");
            var activeFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTableRow", "ActiveForeground");
            var headerBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTableRow", "HeaderBackground");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTableRow", "Foreground");

            _rootBorder.BorderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");

            if (IsActive)
            {
                Background = activeBgOverride ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                Foreground = activeFgOverride ?? DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");
            }
            else if (IsHeader)
            {
                Background = headerBgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                Foreground = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            }
            else
            {
                ApplyHoverState();
                Foreground = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            }
        }

        private void ApplyHoverState()
        {
            if (_rootBorder == null || IsActive)
                return;

            // Check for lightweight styling overrides
            var hoverBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTableRow", "HoverBackground");

            Background = _isHovering
                ? hoverBgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush")
                : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        #endregion
    }

    /// <summary>
    /// A table cell (td/th equivalent).
    /// </summary>
    public partial class DaisyTableCell : DaisyBaseContentControl
    {
        public DaisyTableCell()
        {
            DefaultStyleKey = typeof(DaisyTableCell);
            VerticalAlignment = VerticalAlignment.Center;
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ColumnWidthProperty =
            DependencyProperty.Register(
                nameof(ColumnWidth),
                typeof(double?),
                typeof(DaisyTableCell),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets an optional fixed width for this column.
        /// </summary>
        public double? ColumnWidth
        {
            get => (double?)GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        #endregion
    }
}
