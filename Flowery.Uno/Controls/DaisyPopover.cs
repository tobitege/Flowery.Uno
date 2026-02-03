using System;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Placement options for the popover.
    /// </summary>
    public enum DaisyPopoverPlacement
    {
        Top,
        Bottom,
        Left,
        Right
    }

    /// <summary>
    /// A lightweight popover control that displays content in a Popup anchored to a trigger.
    /// </summary>
    public partial class DaisyPopover : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _triggerContainer;
        private ContentPresenter? _triggerPresenter;
        private Popup? _popup;
        private Border? _popupBorder;
        private ContentPresenter? _popupPresenter;
        public DaisyPopover()
        {
            DefaultStyleKey = typeof(DaisyPopover);
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        #region Dependency Properties

        public static readonly DependencyProperty TriggerContentProperty =
            DependencyProperty.Register(
                nameof(TriggerContent),
                typeof(object),
                typeof(DaisyPopover),
                new PropertyMetadata(null, OnTriggerContentChanged));

        /// <summary>
        /// Gets or sets the trigger content.
        /// </summary>
        public object? TriggerContent
        {
            get => GetValue(TriggerContentProperty);
            set => SetValue(TriggerContentProperty, value);
        }

        public static readonly DependencyProperty PopoverContentProperty =
            DependencyProperty.Register(
                nameof(PopoverContent),
                typeof(object),
                typeof(DaisyPopover),
                new PropertyMetadata(null, OnPopoverContentChanged));

        /// <summary>
        /// Gets or sets the popover content.
        /// </summary>
        public object? PopoverContent
        {
            get => GetValue(PopoverContentProperty);
            set => SetValue(PopoverContentProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(DaisyPopover),
                new PropertyMetadata(false, OnIsOpenChanged));

        /// <summary>
        /// Gets or sets whether the popover is open.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(DaisyPopoverPlacement),
                typeof(DaisyPopover),
                new PropertyMetadata(DaisyPopoverPlacement.Bottom));

        /// <summary>
        /// Gets or sets the placement of the popover relative to the trigger.
        /// </summary>
        public DaisyPopoverPlacement Placement
        {
            get => (DaisyPopoverPlacement)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalOffset),
                typeof(double),
                typeof(DaisyPopover),
                new PropertyMetadata(0.0));

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                nameof(VerticalOffset),
                typeof(double),
                typeof(DaisyPopover),
                new PropertyMetadata(8.0));

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

        public static readonly DependencyProperty IsLightDismissEnabledProperty =
            DependencyProperty.Register(
                nameof(IsLightDismissEnabled),
                typeof(bool),
                typeof(DaisyPopover),
                new PropertyMetadata(true, OnLightDismissChanged));

        /// <summary>
        /// Gets or sets whether the popover closes when clicking outside.
        /// </summary>
        public bool IsLightDismissEnabled
        {
            get => (bool)GetValue(IsLightDismissEnabledProperty);
            set => SetValue(IsLightDismissEnabledProperty, value);
        }

        public static readonly DependencyProperty ToggleOnClickProperty =
            DependencyProperty.Register(
                nameof(ToggleOnClick),
                typeof(bool),
                typeof(DaisyPopover),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether clicking the trigger toggles the popover.
        /// </summary>
        public bool ToggleOnClick
        {
            get => (bool)GetValue(ToggleOnClickProperty);
            set => SetValue(ToggleOnClickProperty, value);
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyPopover),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnTriggerContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPopover popover && popover._triggerPresenter != null)
            {
                popover._triggerPresenter.Content = e.NewValue;
                popover.UpdateAutomationProperties();
            }
        }

        private static void OnPopoverContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPopover popover && popover._popupPresenter != null)
            {
                popover._popupPresenter.Content = e.NewValue;
            }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPopover popover)
            {
                popover.UpdatePopupState();
            }
        }

        private static void OnLightDismissChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPopover popover && popover._popup != null)
            {
                popover._popup.IsLightDismissEnabled = (bool)e.NewValue;
            }
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPopover popover)
            {
                popover.UpdateAutomationProperties();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyColors();
                UpdateAutomationProperties();
                return;
            }

            BuildVisualTree();
            ApplyColors();
            UpdateAutomationProperties();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyColors();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _popupBorder ?? base.GetNeumorphicHostElement();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            DaisyAccessibility.TryHandleEnterOrSpace(e, ToggleFromKeyboard);
        }

        private void ToggleFromKeyboard()
        {
            if (ToggleOnClick)
            {
                IsOpen = !IsOpen;
            }
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            _rootGrid = new Grid();

            // Trigger container
            _triggerContainer = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            _triggerContainer.Tapped += OnTriggerTapped;

            _triggerPresenter = new ContentPresenter
            {
                Content = TriggerContent
            };
            _triggerContainer.Child = _triggerPresenter;

            _rootGrid.Children.Add(_triggerContainer);

            // Popup
            _popup = new Popup
            {
                IsLightDismissEnabled = IsLightDismissEnabled
            };
            _popup.Closed += OnPopupClosed;

            _popupBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                MinWidth = 100
            };

            _popupPresenter = new ContentPresenter
            {
                Content = PopoverContent
            };
            _popupBorder.Child = _popupPresenter;

            _popup.Child = _popupBorder;
            _rootGrid.Children.Add(_popup);

            Content = _rootGrid;
        }

        private void OnTriggerTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ToggleOnClick)
            {
                IsOpen = !IsOpen;
            }
            DaisyAccessibility.FocusOnPointer(this);
        }

        private void OnPopupClosed(object? sender, object e)
        {
            IsOpen = false;
        }

        private void UpdatePopupState()
        {
            if (_popup == null || _triggerContainer == null)
                return;

            if (IsOpen)
            {
                try
                {
                    if (_popup.XamlRoot == null && XamlRoot != null)
                    {
                        _popup.XamlRoot = XamlRoot;
                    }
                }
                catch
                {
                    // Ignore if XamlRoot is not accessible on this backend.
                }
            }

            _popup.IsOpen = IsOpen;

            if (IsOpen)
            {
                // Position the popup based on Placement
                UpdatePopupPlacement();
            }
        }

        private void UpdatePopupPlacement()
        {
            if (_popup == null || _triggerContainer == null)
                return;

            // WinUI popups use HorizontalOffset and VerticalOffset
            // Position relative to the trigger
            var triggerHeight = _triggerContainer.ActualHeight;
            var triggerWidth = _triggerContainer.ActualWidth;

            switch (Placement)
            {
                case DaisyPopoverPlacement.Top:
                    _popup.HorizontalOffset = HorizontalOffset;
                    _popup.VerticalOffset = -VerticalOffset - 50; // Approximate popup height
                    break;

                case DaisyPopoverPlacement.Bottom:
                    _popup.HorizontalOffset = HorizontalOffset;
                    _popup.VerticalOffset = triggerHeight + VerticalOffset;
                    break;

                case DaisyPopoverPlacement.Left:
                    _popup.HorizontalOffset = -HorizontalOffset - 100; // Approximate popup width
                    _popup.VerticalOffset = VerticalOffset;
                    break;

                case DaisyPopoverPlacement.Right:
                    _popup.HorizontalOffset = triggerWidth + HorizontalOffset;
                    _popup.VerticalOffset = VerticalOffset;
                    break;
            }
        }

        #endregion

        #region Apply Styling

        private void ApplyColors()
        {
            if (_popupBorder == null)
                return;

            // Check for lightweight styling overrides
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyPopover", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyPopover", "BorderBrush");

            _popupBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _popupBorder.BorderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            _popupBorder.BorderThickness = new Thickness(1);
        }


        #endregion

        private void UpdateAutomationProperties()
        {
            var name = AccessibleText;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = DaisyAccessibility.GetAccessibleNameFromContent(TriggerContent) ?? "Popover";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

    }
}
