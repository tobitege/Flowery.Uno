using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Flowery.Controls
{
    /// <summary>
    /// A join container control styled after DaisyUI's Join component.
    /// Groups child controls together with shared borders and rounded corners on the ends.
    /// </summary>
    public partial class DaisyJoin : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<UIElement> _joinItems = [];

        public DaisyJoin()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_itemsPanel != null)
            {
                ApplyTheme();
                return;
            }

            CollectAndDisplay();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme();
        }

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyJoin),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        #endregion

        #region ActiveIndex
        public static readonly DependencyProperty ActiveIndexProperty =
            DependencyProperty.Register(
                nameof(ActiveIndex),
                typeof(int),
                typeof(DaisyJoin),
                new PropertyMetadata(-1, OnActiveIndexChanged));

        /// <summary>
        /// Gets or sets the index of the active/selected item (0-based).
        /// Set to -1 for no selection. When set, applies ActiveBackground and ActiveForeground to that item.
        /// </summary>
        public int ActiveIndex
        {
            get => (int)GetValue(ActiveIndexProperty);
            set => SetValue(ActiveIndexProperty, value);
        }

        private static void OnActiveIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyJoin join)
                join.ApplyActiveHighlight();
        }
        #endregion

        #region ActiveBackground
        public static readonly DependencyProperty ActiveBackgroundProperty =
            DependencyProperty.Register(
                nameof(ActiveBackground),
                typeof(Brush),
                typeof(DaisyJoin),
                new PropertyMetadata(null, OnActiveStyleChanged));

        /// <summary>
        /// Gets or sets the background brush for the active item.
        /// </summary>
        public Brush? ActiveBackground
        {
            get => (Brush?)GetValue(ActiveBackgroundProperty);
            set => SetValue(ActiveBackgroundProperty, value);
        }
        #endregion

        #region ActiveForeground
        public static readonly DependencyProperty ActiveForegroundProperty =
            DependencyProperty.Register(
                nameof(ActiveForeground),
                typeof(Brush),
                typeof(DaisyJoin),
                new PropertyMetadata(null, OnActiveStyleChanged));

        /// <summary>
        /// Gets or sets the foreground brush for the active item.
        /// </summary>
        public Brush? ActiveForeground
        {
            get => (Brush?)GetValue(ActiveForegroundProperty);
            set => SetValue(ActiveForegroundProperty, value);
        }

        private static void OnActiveStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyJoin join)
                join.ApplyActiveHighlight();
        }
        #endregion

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyJoin join)
            {
                if (join._itemsPanel != null)
                {
                    join._itemsPanel.Orientation = join.Orientation;
                }
                join.ApplyJoinBorders();
            }
        }

        private void CollectAndDisplay()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            if (ReferenceEquals(Content, _itemsPanel) && _itemsPanel != null)
            {
                _joinItems.Clear();
                foreach (var child in _itemsPanel.Children)
                {
                    _joinItems.Add(child);
                }
                ApplyJoinBorders();
                ApplyTheme();
                return;
            }

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation,
                Spacing = 0, // No spacing - items are joined
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var originalContent = Content;
            _joinItems.Clear();

            // Collect children from Content - detach them BEFORE setting Content = null
            // to avoid WinUI/WinRT cleanup invalidating the children's state
            if (originalContent is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                // Clear the original panel's children first
                panel.Children.Clear();

                foreach (var child in children)
                {
                    // Common usage pattern is wrapping join items in a StackPanel/Grid in XAML.
                    // Flatten one level so joins work even with wrapper panels.
                    if (child is Panel nestedPanel)
                    {
                        var nestedChildren = new List<UIElement>();
                        foreach (var nested in nestedPanel.Children)
                            nestedChildren.Add(nested);

                        // Clear nested panel's children
                        nestedPanel.Children.Clear();

                        foreach (var nested in nestedChildren)
                        {
                            _joinItems.Add(nested);
                            _itemsPanel.Children.Add(nested);
                            DisableChildNeumorphic(nested);
                        }
                    }
                    else
                    {
                        _joinItems.Add(child);
                        _itemsPanel.Children.Add(child);
                        DisableChildNeumorphic(child);
                    }
                }
            }
            else if (originalContent is UIElement singleElement)
            {
                // Detach single element before nulling Content
                if (singleElement is FrameworkElement fe && fe.Parent is ContentControl cc)
                {
                    cc.Content = null;
                }

                _joinItems.Add(singleElement);
                _itemsPanel.Children.Add(singleElement);
                DisableChildNeumorphic(singleElement);
            }

            // Now safe to set Content to the new panel
            Content = _itemsPanel;
            ApplyJoinBorders();
            ApplyTheme();
        }

        private void ApplyJoinBorders()
        {
            if (_joinItems.Count == 0)
                return;

            bool isHorizontal = Orientation == Orientation.Horizontal;
            var resources = Application.Current?.Resources;
            var base300Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");

            for (int i = 0; i < _joinItems.Count; i++)
            {
                bool isFirst = i == 0;
                bool isLast = i == _joinItems.Count - 1;

                if (_joinItems[i] is FrameworkElement fe)
                {
                    // In vertical mode, joins should look like one cohesive "stack" with equal widths.
                    // Force stretch and clear any explicit width that would prevent uniform sizing.
                    if (!isHorizontal)
                    {
                        fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                        fe.ClearValue(FrameworkElement.WidthProperty);
                    }

                    // Overlap borders so dividers don't become "double thickness".
                    fe.Margin = isFirst
                        ? new Thickness(0)
                        : isHorizontal
                            ? new Thickness(-1, 0, 0, 0)
                            : new Thickness(0, -1, 0, 0);

                    // Apply corner radius based on position for common joinable controls.
                    var cr = GetJoinCornerRadius(isFirst, isLast, isHorizontal);

                    if (fe is Button button)
                    {
                        button.CornerRadius = cr;

                        // DaisyButton default/soft styles use Transparent border brush; joins look "separated" without dividers.
                        // Preserve outline/dash styling (they already have meaningful border colors).
                        if (button is DaisyButton daisyButton &&
                            (daisyButton.ButtonStyle == DaisyButtonStyle.Default || daisyButton.ButtonStyle == DaisyButtonStyle.Soft) &&
                            IsTransparent(daisyButton.BorderBrush))
                        {
                            daisyButton.BorderBrush = base300Brush;
                            daisyButton.BorderThickness = new Thickness(1);
                        }
                    }
                    else if (fe is ToggleButton toggleButton)
                    {
                        toggleButton.CornerRadius = cr;
                    }
                    else if (fe is TextBox textBox)
                    {
                        textBox.CornerRadius = cr;
                        // Ensure consistent border for seamless join appearance
                        if (IsTransparent(textBox.BorderBrush))
                        {
                            textBox.BorderBrush = base300Brush;
                        }
                        textBox.BorderThickness = new Thickness(1);
                    }
                    else if (fe is ComboBox comboBox)
                    {
                        comboBox.CornerRadius = cr;
                        // Ensure consistent border for seamless join appearance
                        if (IsTransparent(comboBox.BorderBrush))
                        {
                            comboBox.BorderBrush = base300Brush;
                        }
                        comboBox.BorderThickness = new Thickness(1);
                    }
                    else if (fe is Border border)
                    {
                        border.CornerRadius = cr;
                        // Ensure consistent border for seamless join appearance
                        if (IsTransparent(border.BorderBrush))
                        {
                            border.BorderBrush = base300Brush;
                        }
                        border.BorderThickness = new Thickness(1);
                    }
                }
            }
        }

        private static bool IsTransparent(Brush? brush)
        {
            if (brush == null)
                return true;

            if (brush is SolidColorBrush scb)
                return scb.Color.A == 0;

            return false;
        }


        private static CornerRadius GetJoinCornerRadius(bool isFirst, bool isLast, bool isHorizontal)
        {
            double radius = 8;

            if (isFirst && isLast)
            {
                return new CornerRadius(radius);
            }

            if (isHorizontal)
            {
                if (isFirst)
                {
                    return new CornerRadius(radius, 0, 0, radius);
                }
                else if (isLast)
                {
                    return new CornerRadius(0, radius, radius, 0);
                }
                else
                {
                    return new CornerRadius(0);
                }
            }
            else
            {
                // Vertical
                if (isFirst)
                {
                    return new CornerRadius(radius, radius, 0, 0);
                }
                else if (isLast)
                {
                    return new CornerRadius(0, 0, radius, radius);
                }
                else
                {
                    return new CornerRadius(0);
                }
            }
        }

        private void ApplyTheme()
        {
            ApplyJoinBorders();
            ApplyActiveHighlight();
        }

        private void ApplyActiveHighlight()
        {
            if (_joinItems.Count == 0 || ActiveIndex < 0)
                return;

            var resources = Application.Current?.Resources;
            var base200Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush");
            var baseContentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");
            var activeBg = ActiveBackground ?? DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush");
            var activeFg = ActiveForeground ?? DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryContentBrush");

            for (int i = 0; i < _joinItems.Count; i++)
            {
                bool isActive = i == ActiveIndex;

                if (_joinItems[i] is Control control)
                {
                    control.Background = isActive ? activeBg : base200Brush;
                    control.Foreground = isActive ? activeFg : baseContentBrush;
                }
            }
        }

        /// <summary>
        /// Re-collects children and re-applies join styling.
        /// Call this after dynamically adding or removing children.
        /// </summary>
        public void RefreshItems()
        {
            CollectAndDisplay();
        }

        /// <summary>
        /// Gets the list of joined items for external access (read-only).
        /// </summary>
        public IReadOnlyList<UIElement> JoinedItems => _joinItems;

        /// <summary>
        /// IMPORTANT: Disables neumorphic on child controls.
        ///
        /// The JOIN itself is the single neumorphic surface - it draws ONE unified shadow/border
        /// around all joined children. If we let each child have its own neumorphic effect, we'd
        /// get overlapping shadows and borders between adjacent items, which looks wrong.
        /// The children are flat visual segments inside the container's neumorphic shell.
        ///
        /// This applies to both DaisyButton (which has its own neumorphic implementation)
        /// and DaisyBaseContentControl (which is the base class for neumorphic-enabled controls).
        /// </summary>
        private static void DisableChildNeumorphic(UIElement element)
        {
            if (element is DaisyButton db)
            {
                DaisyNeumorphic.SetIsEnabled(db, false);
            }
            else if (element is DaisyBaseContentControl dbcc)
            {
                DaisyNeumorphic.SetIsEnabled(dbcc, false);
            }
        }

        private static bool DetachFromParent(UIElement element)
        {
            if (element is not FrameworkElement fe)
                return true;

            var parent = fe.Parent;
            if (parent == null)
                return true;

            if (parent is Panel panel)
            {
                panel.Children.Remove(element);
            }
            else if (parent is ContentPresenter contentPresenter && ReferenceEquals(contentPresenter.Content, element))
            {
                contentPresenter.Content = null;
            }
            else if (parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, element))
            {
                contentControl.Content = null;
            }
            else if (parent is Border border && ReferenceEquals(border.Child, element))
            {
                border.Child = null;
            }
            else if (parent is UserControl userControl && ReferenceEquals(userControl.Content, element))
            {
                userControl.Content = null;
            }

            if (fe.Parent != null)
            {
                FloweryDiagnostics.Log($"[DaisyJoin] Unable to detach child '{fe.GetType().Name}' from parent '{fe.Parent.GetType().Name}'.");
                return false;
            }

            return true;
        }
    }
}
