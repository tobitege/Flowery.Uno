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
    /// A vertical list layout to display information in rows.
    /// Similar to daisyUI's list component.
    /// </summary>
    public partial class DaisyList : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private StackPanel? _itemsPanel;
        private readonly List<DaisyListRow> _rows = [];

        public DaisyList()
        {
            DefaultStyleKey = typeof(DaisyList);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyList),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyList list)
            {
                list.ApplyAll();
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
            // Capture user content
            var userContent = Content;
            Content = null;

            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(16)
            };

            _itemsPanel = new StackPanel
            {
                Spacing = 0
            };

            // Collect rows from user content
            _rows.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);

                    if (child is DaisyListRow row)
                    {
                        _rows.Add(row);
                        _itemsPanel.Children.Add(row);
                    }
                    else
                    {
                        // Wrap non-row content
                        _itemsPanel.Children.Add(child);
                    }
                }
            }
            else if (userContent != null)
            {
                _itemsPanel.Children.Add(userContent as UIElement);
            }

            _rootBorder.Child = _itemsPanel;
            Content = _rootBorder;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_rootBorder == null)
                return;

            // Check for lightweight styling overrides
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyList", "Background");

            _rootBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase100Brush");

            // Propagate size to rows
            foreach (var row in _rows)
            {
                row.Size = Size;
            }
        }

        #endregion
    }

    /// <summary>
    /// A row item inside DaisyList. Uses a horizontal layout.
    /// </summary>
    public partial class DaisyListRow : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private Grid? _columnsGrid;
        private readonly List<UIElement> _columns = [];
        private bool _isHovering;

        public DaisyListRow()
        {
            DefaultStyleKey = typeof(DaisyListRow);
            IsTabStop = false;

            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyListRow),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty GrowColumnProperty =
            DependencyProperty.Register(
                nameof(GrowColumn),
                typeof(int),
                typeof(DaisyListRow),
                new PropertyMetadata(1, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the index of the column that should grow to fill remaining space.
        /// Default is 1 (second column). Set to -1 to disable auto-grow.
        /// </summary>
        public int GrowColumn
        {
            get => (int)GetValue(GrowColumnProperty);
            set => SetValue(GrowColumnProperty, value);
        }

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(DaisyListRow),
                new PropertyMetadata(12.0, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the spacing between columns.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyListRow row)
            {
                row.ApplyAll();
            }
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyListRow row)
            {
                row.RebuildColumns();
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

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isHovering = true;
            ApplyHoverState();
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isHovering = false;
            ApplyHoverState();
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
            // Capture user content
            var userContent = Content;
            Content = null;

            _rootBorder = new Border
            {
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            _columnsGrid = new Grid();

            // Collect columns from user content
            _columns.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);
                    _columns.Add(child);
                }
            }
            else if (userContent != null)
            {
                _columns.Add(userContent as UIElement ?? new Border());
            }

            RebuildColumns();

            _rootBorder.Child = _columnsGrid;
            Content = _rootBorder;
        }

        private void RebuildColumns()
        {
            if (_columnsGrid == null) return;

            _columnsGrid.ColumnDefinitions.Clear();
            _columnsGrid.Children.Clear();

            for (int i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];

                // Check if this column should grow
                bool isGrowColumn = false;
                if (column is DaisyListColumn listColumn && listColumn.Grow)
                {
                    isGrowColumn = true;
                }
                else if (i == GrowColumn)
                {
                    isGrowColumn = true;
                }

                var colDef = new ColumnDefinition
                {
                    Width = isGrowColumn
                        ? new GridLength(1, GridUnitType.Star)
                        : GridLength.Auto
                };
                _columnsGrid.ColumnDefinitions.Add(colDef);

                // Add spacing margin (except for first column)
                if (column is FrameworkElement fe && i > 0)
                {
                    var currentMargin = fe.Margin;
                    fe.Margin = new Thickness(Spacing, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);
                }

                if (column is FrameworkElement fe2)
                {
                    Grid.SetColumn(fe2, i);
                }
                _columnsGrid.Children.Add(column);
            }
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

            // List rows use menu-style padding
            Thickness padding = resources == null
                ? DaisyResourceLookup.GetDefaultMenuPadding(Size)
                : DaisyResourceLookup.GetThickness(resources, $"DaisyMenu{sizeKey}Padding",
                    DaisyResourceLookup.GetDefaultMenuPadding(Size));
            _rootBorder.Padding = padding;
        }

        private void ApplyColors()
        {
            if (_rootBorder == null)
                return;

            // Check for lightweight styling overrides
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyListRow", "BorderBrush");

            _rootBorder.BorderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            ApplyHoverState();
        }

        private void ApplyHoverState()
        {
            if (_rootBorder == null)
                return;

            // Check for lightweight styling overrides
            var hoverBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyListRow", "HoverBackground");

            _rootBorder.Background = _isHovering
                ? hoverBgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush")
                : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        #endregion
    }

    /// <summary>
    /// A column item within a DaisyListRow.
    /// </summary>
    public partial class DaisyListColumn : DaisyBaseContentControl
    {
        public DaisyListColumn()
        {
            DefaultStyleKey = typeof(DaisyListColumn);
            VerticalAlignment = VerticalAlignment.Center;
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty GrowProperty =
            DependencyProperty.Register(
                nameof(Grow),
                typeof(bool),
                typeof(DaisyListColumn),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, this column will grow to fill available space.
        /// </summary>
        public bool Grow
        {
            get => (bool)GetValue(GrowProperty);
            set => SetValue(GrowProperty, value);
        }

        public static readonly DependencyProperty WrapProperty =
            DependencyProperty.Register(
                nameof(Wrap),
                typeof(bool),
                typeof(DaisyListColumn),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, this column will wrap to a new line.
        /// </summary>
        public bool Wrap
        {
            get => (bool)GetValue(WrapProperty);
            set => SetValue(WrapProperty, value);
        }

        #endregion
    }
}
