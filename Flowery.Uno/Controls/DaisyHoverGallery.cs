using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// A hover-activated gallery. Move the pointer horizontally to reveal different items.
    /// </summary>
    [ContentProperty(Name = nameof(Items))]
    public partial class DaisyHoverGallery : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Grid? _itemsHost;
        private Canvas? _dividersCanvas;
        private Border? _inputOverlay;

        public DaisyHoverGallery()
        {
            // Ensure content stretches properly
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            IsTabStop = true;
            UseSystemFocusVisuals = true;

            Items.CollectionChanged += OnItemsCollectionChanged;
            SizeChanged += OnSizeChanged;
        }

        /// <summary>
        /// Gets the items displayed by the gallery.
        /// </summary>
        public ObservableCollection<UIElement> Items { get; } = [];

        private bool _dividersApplied;

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();

            // Dividers need valid ActualWidth/Height which may not be available until after layout
            _dividersApplied = false;
            LayoutUpdated += OnLayoutUpdated;
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            if (!_dividersApplied && ActualWidth > 0 && ActualHeight > 0)
            {
                _dividersApplied = true;
                UpdateDividers();
                LayoutUpdated -= OnLayoutUpdated;
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            LayoutUpdated -= OnLayoutUpdated;
            if (_rootGrid != null)
                _rootGrid.PointerPressed -= OnPointerPressed;
            if (_inputOverlay != null)
                _inputOverlay.PointerPressed -= OnPointerPressed;
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            UpdateDividers();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDividers();
        }

        #region VisibleIndex
        public static readonly DependencyProperty VisibleIndexProperty =
            DependencyProperty.Register(
                nameof(VisibleIndex),
                typeof(int),
                typeof(DaisyHoverGallery),
                new PropertyMetadata(0, OnVisibleIndexChanged));

        /// <summary>
        /// Gets or sets which item index is currently visible.
        /// </summary>
        public int VisibleIndex
        {
            get => (int)GetValue(VisibleIndexProperty);
            set => SetValue(VisibleIndexProperty, value);
        }
        #endregion

        #region DividerBrush
        public static readonly DependencyProperty DividerBrushProperty =
            DependencyProperty.Register(
                nameof(DividerBrush),
                typeof(Brush),
                typeof(DaisyHoverGallery),
                new PropertyMetadata(null, OnDividerAppearanceChanged));

        public Brush? DividerBrush
        {
            get => (Brush?)GetValue(DividerBrushProperty);
            set => SetValue(DividerBrushProperty, value);
        }
        #endregion

        #region DividerThickness
        public static readonly DependencyProperty DividerThicknessProperty =
            DependencyProperty.Register(
                nameof(DividerThickness),
                typeof(double),
                typeof(DaisyHoverGallery),
                new PropertyMetadata(1.0, OnDividerAppearanceChanged));

        public double DividerThickness
        {
            get => (double)GetValue(DividerThicknessProperty);
            set => SetValue(DividerThicknessProperty, value);
        }
        #endregion

        #region ShowDividers
        public static readonly DependencyProperty ShowDividersProperty =
            DependencyProperty.Register(
                nameof(ShowDividers),
                typeof(bool),
                typeof(DaisyHoverGallery),
                new PropertyMetadata(true, OnDividerAppearanceChanged));

        public bool ShowDividers
        {
            get => (bool)GetValue(ShowDividersProperty);
            set => SetValue(ShowDividersProperty, value);
        }
        #endregion

        private static void OnVisibleIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyHoverGallery gallery)
                gallery.UpdateItemVisibility();
        }

        private static void OnDividerAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyHoverGallery gallery)
                gallery.UpdateDividers();
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildItemsHost();
            UpdateItemVisibility();
            UpdateDividers();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid
            {
                // Ensure the control reliably receives pointer events across Uno/WinUI targets.
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            _rootGrid.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
            _rootGrid.PointerExited += OnPointerExited;
            _rootGrid.PointerPressed += OnPointerPressed;

            _itemsHost = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_itemsHost);

            _dividersCanvas = new Canvas
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_dividersCanvas);

            // WinUI/Uno pointer events can behave like "direct" events depending on the platform
            // and the element under the pointer. Add a transparent overlay so the gallery
            // always receives PointerMoved/PointerExited without requiring users to set
            // Background/IsHitTestVisible on their item content.
            _inputOverlay = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _inputOverlay.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
            _inputOverlay.PointerExited += OnPointerExited;
            _inputOverlay.PointerPressed += OnPointerPressed;
            _rootGrid.Children.Add(_inputOverlay);

            Content = _rootGrid;

            RebuildItemsHost();
        }

        private void RebuildItemsHost()
        {
            if (_itemsHost == null)
                return;

            _itemsHost.Children.Clear();

            foreach (var item in Items)
            {
                // Ensure item is detached from any previous parent (Parent is on FrameworkElement)
                if (item is FrameworkElement fe)
                {
                    if (fe.Parent is Panel parentPanel)
                    {
                        parentPanel.Children.Remove(item);
                    }

                    // Force stretch alignment so items fill the container
                    fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                    fe.VerticalAlignment = VerticalAlignment.Stretch;
                }

                // All items go into the same Grid cell (row 0, col 0) to overlay
                _itemsHost.Children.Add(item);
            }
        }

        private void ApplyAll()
        {
            UpdateItemVisibility();
            UpdateDividers();
        }

        private void UpdateItemVisibility()
        {
            if (_itemsHost == null)
                return;

            var count = _itemsHost.Children.Count;
            if (count == 0)
                return;

            var idx = ValueCoercion.Index(VisibleIndex, count);
            if (idx != VisibleIndex)
                VisibleIndex = idx;

            for (int i = 0; i < count; i++)
            {
                _itemsHost.Children[i].Visibility = i == idx ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateDividers()
        {
            if (_dividersCanvas == null)
                return;

            _dividersCanvas.Children.Clear();

            if (!ShowDividers)
                return;

            var count = Items.Count;
            if (count <= 1)
                return;

            var width = _rootGrid?.ActualWidth ?? ActualWidth;
            var height = _rootGrid?.ActualHeight ?? ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            // N items => N equal hover regions => N-1 dividers.
            var columnCount = count;
            var columnWidth = width / columnCount;

            var brush = DividerBrush ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            var opacity = DividerBrush == null ? 0.25 : 1.0;
            var thickness = ValueCoercion.StrokeThickness(DividerThickness, min: 0.5);

            for (int i = 1; i < columnCount; i++)
            {
                var x = (columnWidth * i) - (thickness / 2);
                var line = new Rectangle
                {
                    Width = thickness,
                    Height = height,
                    Fill = brush,
                    Opacity = opacity
                };
                Canvas.SetLeft(line, x);
                Canvas.SetTop(line, 0);
                _dividersCanvas.Children.Add(line);
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var count = Items.Count;
            if (count <= 1)
            {
                VisibleIndex = 0;
                return;
            }

            var width = _rootGrid?.ActualWidth ?? ActualWidth;
            if (width <= 0)
            {
                VisibleIndex = 0;
                return;
            }

            var relativeTo = _rootGrid ?? (UIElement)this;
            var pointerX = e.GetCurrentPoint(relativeTo).Position.X;
            var columnCount = count;
            var columnWidth = width / columnCount;
            var columnIndex = ValueCoercion.Index((int)(pointerX / columnWidth), columnCount);
            if (VisibleIndex != columnIndex)
                VisibleIndex = columnIndex;
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Ignore PointerExited bubbling from child elements (causes flicker).
            if (!ReferenceEquals(e.OriginalSource, sender))
                return;

            if (VisibleIndex != 0)
                VisibleIndex = 0;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || Items.Count <= 1)
                return;

            var index = VisibleIndex;
            var lastIndex = Items.Count - 1;

            switch (e.Key)
            {
                case VirtualKey.Left:
                case VirtualKey.Up:
                    index = Math.Max(0, index - 1);
                    break;
                case VirtualKey.Right:
                case VirtualKey.Down:
                    index = Math.Min(lastIndex, index + 1);
                    break;
                case VirtualKey.Home:
                    index = 0;
                    break;
                case VirtualKey.End:
                    index = lastIndex;
                    break;
                default:
                    return;
            }

            if (VisibleIndex != index)
                VisibleIndex = index;

            e.Handled = true;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            DaisyAccessibility.FocusOnPointer(this);
        }
    }
}
