using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Flowery.Helpers;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// Layout mode for the FAB action items.
    /// </summary>
    public enum FabLayout
    {
        Vertical,
        Horizontal,
        Flower
    }

    /// <summary>
    /// A floating action button control styled after material design FAB.
    /// Displays expandable action items when the trigger is clicked.
    /// </summary>
    public partial class DaisyFab : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private StackPanel? _actionsPanel;
        private DaisyButton? _triggerButton;
        private DaisyIconText? _triggerIconText;
        private readonly List<DaisyButton> _actionButtons = [];
        private object? _capturedTriggerContent;
        private Storyboard? _actionsStoryboard;

        public DaisyFab()
        {
            DefaultStyleKey = typeof(DaisyFab);

            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Bottom;

            // Prevent the FAB container itself from getting neumorphic shadows.
            DaisyNeumorphic.SetIsEnabled(this, false);
        }

        /// <summary>
        /// DaisyFab disables neumorphic effects on the container to avoid square shadows
        /// around the action stack; the action buttons handle the effect instead.
        /// </summary>
        protected override bool IsNeumorphicEffectivelyEnabled() => false;

        #region Dependency Properties
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyFab),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFab fab)
            {
                fab.UpdateAutomationProperties();
            }
        }

        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register(
                nameof(Layout),
                typeof(FabLayout),
                typeof(DaisyFab),
                new PropertyMetadata(FabLayout.Vertical, OnLayoutChanged));

#if __ANDROID__
        public new FabLayout Layout
#else
        public FabLayout Layout
#endif
        {
            get => (FabLayout)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(DaisyFab),
                new PropertyMetadata(false, OnIsOpenChanged));

        /// <summary>
        /// Gets or sets whether the FAB action items are visible.
        /// </summary>
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty TriggerVariantProperty =
            DependencyProperty.Register(
                nameof(TriggerVariant),
                typeof(DaisyButtonVariant),
                typeof(DaisyFab),
                new PropertyMetadata(DaisyButtonVariant.Primary, OnAppearanceChanged));

        public DaisyButtonVariant TriggerVariant
        {
            get => (DaisyButtonVariant)GetValue(TriggerVariantProperty);
            set => SetValue(TriggerVariantProperty, value);
        }

        public static readonly DependencyProperty TriggerContentProperty =
            DependencyProperty.Register(
                nameof(TriggerContent),
                typeof(object),
                typeof(DaisyFab),
                new PropertyMetadata("+", OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the content displayed on the trigger button.
        /// For icon-only buttons, prefer using TriggerIcon instead.
        /// </summary>
        public object? TriggerContent
        {
            get => GetValue(TriggerContentProperty);
            set => SetValue(TriggerContentProperty, value);
        }

        public static readonly DependencyProperty TriggerIconDataProperty =
            DependencyProperty.Register(
                nameof(TriggerIconData),
                typeof(string),
                typeof(DaisyFab),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the path data string for the trigger button icon.
        /// This is the safest way to set an icon (avoids UIElement parentage issues).
        /// The PathIcon is created internally from this data.
        /// When set, this takes precedence over TriggerContent.
        /// </summary>
        public string? TriggerIconData
        {
            get => (string?)GetValue(TriggerIconDataProperty);
            set => SetValue(TriggerIconDataProperty, value);
        }

        public static readonly DependencyProperty TriggerIconSizeProperty =
            DependencyProperty.Register(
                nameof(TriggerIconSize),
                typeof(double),
                typeof(DaisyFab),
                new PropertyMetadata(double.NaN, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the trigger icon.
        /// If not set (NaN), automatically sized based on the FAB's Size property.
        /// </summary>
        public double TriggerIconSize
        {
            get => (double)GetValue(TriggerIconSizeProperty);
            set => SetValue(TriggerIconSizeProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyFab),
                new PropertyMetadata(DaisySize.Large, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty AutoCloseProperty =
            DependencyProperty.Register(
                nameof(AutoClose),
                typeof(bool),
                typeof(DaisyFab),
                new PropertyMetadata(true));

        /// <summary>
        /// When true (default), clicking an action item automatically closes the FAB.
        /// </summary>
        public bool AutoClose
        {
            get => (bool)GetValue(AutoCloseProperty);
            set => SetValue(AutoCloseProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFab fab)
            {
                fab.UpdateActionsLayout();
            }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFab fab)
            {
                fab.UpdateActionsVisibility();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFab fab)
            {
                fab.UpdateTriggerButton();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                UpdateTriggerButton();
                return;
            }

            BuildVisualTree();
            UpdateTriggerButton();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            UpdateTriggerButton();
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

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user content (action buttons)
            var userContent = Content;
            Content = null;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            // Layout depends on orientation
            var isHorizontal = Layout == FabLayout.Horizontal;
            if (isHorizontal)
            {
                // Column 0: Trigger button, Column 1: Actions panel (expands to right in LTR, left in RTL)
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }
            else
            {
                // Row 0: Actions panel, Row 1: Trigger button (actions above trigger)
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Actions panel
            _actionsPanel = new StackPanel
            {
                Spacing = 8,
                Margin = isHorizontal ? new Thickness(8, 0, 0, 0) : new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            // Collect action buttons from user content
            _actionButtons.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);

                    if (child is DaisyButton btn)
                    {
                        _actionButtons.Add(btn);
                        btn.Click += OnActionButtonClick;
                        btn.Shape = DaisyButtonShape.Circle;
                        btn.Size = Size;
                        if (!DaisyNeumorphic.GetIsEnabled(btn).HasValue)
                        {
                            DaisyNeumorphic.SetIsEnabled(btn, true);
                        }
                        _actionsPanel.Children.Add(btn);
                    }
                }
            }
            else if (userContent is DaisyButton singleBtn)
            {
                _actionButtons.Add(singleBtn);
                singleBtn.Click += OnActionButtonClick;
                singleBtn.Shape = DaisyButtonShape.Circle;
                singleBtn.Size = Size;
                if (!DaisyNeumorphic.GetIsEnabled(singleBtn).HasValue)
                {
                    DaisyNeumorphic.SetIsEnabled(singleBtn, true);
                }
                _actionsPanel.Children.Add(singleBtn);
            }

            if (isHorizontal)
            {
                Grid.SetColumn(_actionsPanel, 1);
            }
            else
            {
                Grid.SetRow(_actionsPanel, 0);
            }
            _rootGrid.Children.Add(_actionsPanel);

            // Trigger button
            _triggerButton = new DaisyButton
            {
                Shape = DaisyButtonShape.Circle,
                Size = Size,
                Variant = TriggerVariant,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (!DaisyNeumorphic.GetIsEnabled(_triggerButton).HasValue)
            {
                DaisyNeumorphic.SetIsEnabled(_triggerButton, true);
            }

            // Use TriggerIconData if set - now using DaisyIconText for proper Viewbox scaling
            var iconData = TriggerIconData;
            if (!string.IsNullOrEmpty(iconData))
            {
                // DaisyIconText handles all the Viewbox wrapping and sizing internally
                _triggerIconText = new DaisyIconText
                {
                    IconData = iconData,
                    Size = Size,
                    IconSize = double.IsNaN(TriggerIconSize) ? double.NaN : TriggerIconSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                _triggerButton.Content = _triggerIconText;
            }
            else if (TriggerContent != null)
            {
                // Legacy pattern: TriggerContent (may have UIElement parentage issues)
                _capturedTriggerContent = TriggerContent;
                if (_capturedTriggerContent is UIElement)
                {
                    ClearValue(TriggerContentProperty);
                }
                _triggerButton.Content = _capturedTriggerContent;
            }
            else
            {
                // Default: use plus icon via DaisyIconText
                _triggerIconText = new DaisyIconText
                {
                    IconData = "M12 5v14M5 12h14", // Plus icon path
                    Size = Size,
                    IconSize = double.IsNaN(TriggerIconSize) ? double.NaN : TriggerIconSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                _triggerButton.Content = _triggerIconText;
            }

            _triggerButton.Click += OnTriggerClick;

            if (isHorizontal)
            {
                Grid.SetColumn(_triggerButton, 0);
            }
            else
            {
                Grid.SetRow(_triggerButton, 1);
            }
            _rootGrid.Children.Add(_triggerButton);

            Content = _rootGrid;

            UpdateActionsLayout();
            UpdateAutomationProperties();
        }

        private void UpdateActionsLayout()
        {
            if (_actionsPanel == null)
                return;

            _actionsPanel.Orientation = Layout == FabLayout.Horizontal
                ? Orientation.Horizontal
                : Orientation.Vertical;

            // For Flower layout, we could potentially arrange in a radial pattern
            // For simplicity, treat it as vertical with larger spacing
            if (Layout == FabLayout.Flower)
            {
                _actionsPanel.Spacing = 12;
            }
            else
            {
                _actionsPanel.Spacing = 8;
            }

            _actionsPanel.RenderTransformOrigin = GetActionsPanelOrigin();
        }

        private void UpdateActionsVisibility()
        {
            if (_actionsPanel == null)
                return;

            StopActionsStoryboard();

            if (IsOpen)
            {
                StartActionsOpenAnimation();
            }
            else
            {
                StartActionsCloseAnimation();
            }
        }

        private void UpdateTriggerButton()
        {
            if (_triggerButton == null)
                return;

            _triggerButton.Size = Size;
            _triggerButton.Variant = TriggerVariant;

            // Propagate size to all action buttons
            foreach (var btn in _actionButtons)
            {
                btn.Size = Size;
            }

            // Update trigger icon size
            UpdateTriggerIconSize();

            // If we captured a UIElement, don't try to read from TriggerContent (it was cleared).
            // Only update content if a new non-UIElement value was set.
            var currentContent = TriggerContent;
            if (currentContent != null && currentContent is not UIElement && !ReferenceEquals(currentContent, "+"))
            {
                _triggerButton.Content = currentContent;
            }
            // If TriggerContent is default ("+") but we have captured content, leave it alone.
            UpdateAutomationProperties();
        }

        /// <summary>
        /// Updates the size of the trigger button's icon based on the current Size.
        /// </summary>
        private void UpdateTriggerIconSize()
        {
            if (_triggerIconText == null)
                return;

            // DaisyIconText handles all sizing internally
            _triggerIconText.Size = Size;
            _triggerIconText.IconSize = double.IsNaN(TriggerIconSize) ? double.NaN : TriggerIconSize;
        }

        private void OnTriggerClick(object sender, RoutedEventArgs e)
        {
            IsOpen = !IsOpen;
        }

        private void OnActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (AutoClose && IsOpen)
            {
                IsOpen = false;
            }
        }

        #endregion

        private void UpdateAutomationProperties()
        {
            if (_triggerButton == null)
                return;

            var name = string.IsNullOrWhiteSpace(AccessibleText) ? "Floating action button" : AccessibleText;
            AutomationProperties.SetName(_triggerButton, name);
        }

        private Windows.Foundation.Point GetActionsPanelOrigin()
        {
            if (Layout == FabLayout.Horizontal)
            {
                var isRtl = FlowDirection == FlowDirection.RightToLeft;
                return isRtl ? new Windows.Foundation.Point(1, 0.5) : new Windows.Foundation.Point(0, 0.5);
            }

            return new Windows.Foundation.Point(0.5, 1.0);
        }

        private void GetActionsPanelOffsets(out double offsetX, out double offsetY)
        {
            var offset = GetActionsPanelOffset();
            if (Layout == FabLayout.Horizontal)
            {
                var isRtl = FlowDirection == FlowDirection.RightToLeft;
                offsetX = isRtl ? offset : -offset;
                offsetY = 0;
            }
            else
            {
                offsetX = 0;
                offsetY = offset;
            }
        }

        private double GetActionsPanelOffset()
        {
            return Size switch
            {
                DaisySize.ExtraSmall => 8,
                DaisySize.Small => 10,
                DaisySize.Medium => 12,
                DaisySize.Large => 14,
                DaisySize.ExtraLarge => 16,
                _ => 12
            };
        }

        private void StartActionsOpenAnimation()
        {
            if (_actionsPanel == null)
                return;

            if (_actionButtons.Count == 0)
            {
                _actionsPanel.Visibility = Visibility.Visible;
                return;
            }

            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            GetActionsPanelOffsets(out var offsetX, out var offsetY);
            var origin = GetActionsPanelOrigin();
            const int staggerStepMs = 50;

            _actionsPanel.Visibility = Visibility.Visible;

            var storyboard = FloweryAnimationHelpers.CreateStoryboard();

            var count = _actionButtons.Count;
            var isVertical = Layout != FabLayout.Horizontal;
            for (var i = 0; i < count; i++)
            {
                var button = _actionButtons[i];
                var transform = FloweryAnimationHelpers.EnsureCompositeTransform(button);
                button.RenderTransformOrigin = origin;

                button.Opacity = 0;
                transform.ScaleX = 0.94;
                transform.ScaleY = 0.94;
                transform.TranslateX = offsetX;
                transform.TranslateY = offsetY;

                var visualIndex = isVertical
                    ? (count - 1 - i)
                    : (FlowDirection == FlowDirection.RightToLeft ? (count - 1 - i) : i);
                var beginTime = TimeSpan.FromMilliseconds(staggerStepMs * visualIndex);

                FloweryAnimationHelpers.AddPopAnimation(
                    storyboard,
                    button,
                    transform,
                    0,
                    1,
                    0.94,
                    1.0,
                    offsetX,
                    0,
                    offsetY,
                    0,
                    duration.TimeSpan,
                    easing,
                    beginTime);
            }

            _actionsStoryboard = storyboard;
            storyboard.Begin();
        }

        private void StartActionsCloseAnimation()
        {
            if (_actionsPanel == null)
                return;

            if (_actionsPanel.Visibility != Visibility.Visible)
            {
                _actionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var storyboard = FloweryAnimationHelpers.CreateStoryboard();
            var duration = new Duration(TimeSpan.FromMilliseconds(160));
            var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
            GetActionsPanelOffsets(out var offsetX, out var offsetY);
            var origin = GetActionsPanelOrigin();

            if (_actionButtons.Count == 0)
            {
                _actionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            foreach (var button in _actionButtons)
            {
                var transform = FloweryAnimationHelpers.EnsureCompositeTransform(button);
                button.RenderTransformOrigin = origin;
                var currentOpacity = button.Opacity;
                var currentScale = transform.ScaleX;
                var currentTranslateX = transform.TranslateX;
                var currentTranslateY = transform.TranslateY;

                FloweryAnimationHelpers.AddPopAnimation(
                    storyboard,
                    button,
                    transform,
                    currentOpacity,
                    0,
                    currentScale,
                    0.94,
                    currentTranslateX,
                    offsetX,
                    currentTranslateY,
                    offsetY,
                    duration.TimeSpan,
                    easing);
            }

            storyboard.Completed += OnActionsCloseCompleted;
            _actionsStoryboard = storyboard;
            storyboard.Begin();
        }

        private void StopActionsStoryboard()
        {
            if (_actionsStoryboard == null)
                return;

            _actionsStoryboard.Completed -= OnActionsCloseCompleted;
            _actionsStoryboard.Stop();
            _actionsStoryboard = null;
        }

        private void OnActionsCloseCompleted(object? sender, object e)
        {
            if (_actionsPanel != null)
            {
                _actionsPanel.Visibility = Visibility.Collapsed;
            }

            if (_actionsStoryboard != null)
            {
                _actionsStoryboard.Completed -= OnActionsCloseCompleted;
                _actionsStoryboard = null;
            }
        }
    }
}
