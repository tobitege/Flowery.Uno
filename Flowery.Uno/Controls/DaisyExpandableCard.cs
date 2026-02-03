using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Flowery.Helpers;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A Card control that expands to reveal additional content with animation.
    /// Uses template parts (PART_SolidExpandedWrapper, PART_GlassExpandedWrapper) for animation.
    /// </summary>
    public partial class DaisyExpandableCard : DaisyCard
    {
        private Border? _solidExpandedWrapper;
        private ContentPresenter? _solidExpandedContent;
        private CancellationTokenSource? _animationCts;
        private DaisyButton? _actionButton;

        public DaisyExpandableCard()
        {
            DefaultStyleKey = typeof(DaisyExpandableCard);
            ToggleCommand = new SimpleCommand(_ => IsExpanded = !IsExpanded);
        }

        #region Dependency Properties

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                nameof(IsExpanded),
                typeof(bool),
                typeof(DaisyExpandableCard),
                new PropertyMetadata(false, OnIsExpandedChanged));

        /// <summary>
        /// Gets or sets whether the card is currently expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public static readonly DependencyProperty ExpandedContentProperty =
            DependencyProperty.Register(
                nameof(ExpandedContent),
                typeof(object),
                typeof(DaisyExpandableCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content to display in the expanded area.
        /// </summary>
        public object? ExpandedContent
        {
            get => GetValue(ExpandedContentProperty);
            set => SetValue(ExpandedContentProperty, value);
        }

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(
                nameof(AnimationDuration),
                typeof(TimeSpan),
                typeof(DaisyExpandableCard),
                new PropertyMetadata(TimeSpan.FromMilliseconds(300)));

        /// <summary>
        /// Gets or sets the duration of the expand/collapse animation.
        /// </summary>
        public TimeSpan AnimationDuration
        {
            get => (TimeSpan)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Command to toggle the expanded state.
        /// </summary>
        public ICommand ToggleCommand { get; }

        // ============ Batteries-Included Convenience Properties ============

        public new static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata(null));

        /// <summary>
        /// The main title displayed on the card.
        /// </summary>
        public new string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata(null));

        /// <summary>
        /// The subtitle displayed below the title.
        /// </summary>
        public string? Subtitle
        {
            get => (string?)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public static readonly DependencyProperty GradientStartProperty =
            DependencyProperty.Register(nameof(GradientStart), typeof(Windows.UI.Color), typeof(DaisyExpandableCard),
                new PropertyMetadata(Windows.UI.Color.FromArgb(255, 15, 23, 42))); // #0f172a

        /// <summary>
        /// The starting color of the card's background gradient.
        /// </summary>
        public Windows.UI.Color GradientStart
        {
            get => (Windows.UI.Color)GetValue(GradientStartProperty);
            set => SetValue(GradientStartProperty, value);
        }

        public static readonly DependencyProperty GradientEndProperty =
            DependencyProperty.Register(nameof(GradientEnd), typeof(Windows.UI.Color), typeof(DaisyExpandableCard),
                new PropertyMetadata(Windows.UI.Color.FromArgb(255, 51, 65, 85))); // #334155

        /// <summary>
        /// The ending color of the card's background gradient.
        /// </summary>
        public Windows.UI.Color GradientEnd
        {
            get => (Windows.UI.Color)GetValue(GradientEndProperty);
            set => SetValue(GradientEndProperty, value);
        }

        public static readonly DependencyProperty CardWidthProperty =
            DependencyProperty.Register(nameof(CardWidth), typeof(double), typeof(DaisyExpandableCard),
                new PropertyMetadata(150.0));

        /// <summary>
        /// The width of the main card content area.
        /// </summary>
        public double CardWidth
        {
            get => (double)GetValue(CardWidthProperty);
            set => SetValue(CardWidthProperty, value);
        }

        public static readonly DependencyProperty CardHeightProperty =
            DependencyProperty.Register(nameof(CardHeight), typeof(double), typeof(DaisyExpandableCard),
                new PropertyMetadata(225.0));

        /// <summary>
        /// The height of the card.
        /// </summary>
        public double CardHeight
        {
            get => (double)GetValue(CardHeightProperty);
            set => SetValue(CardHeightProperty, value);
        }

        public static readonly DependencyProperty ExpandedTextProperty =
            DependencyProperty.Register(nameof(ExpandedText), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata(null));

        /// <summary>
        /// The main text displayed in the expanded panel.
        /// </summary>
        public string? ExpandedText
        {
            get => (string?)GetValue(ExpandedTextProperty);
            set => SetValue(ExpandedTextProperty, value);
        }

        public static readonly DependencyProperty ExpandedSubtitleProperty =
            DependencyProperty.Register(nameof(ExpandedSubtitle), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata(null));

        /// <summary>
        /// The subtitle displayed in the expanded panel.
        /// </summary>
        public string? ExpandedSubtitle
        {
            get => (string?)GetValue(ExpandedSubtitleProperty);
            set => SetValue(ExpandedSubtitleProperty, value);
        }

        public static readonly DependencyProperty ExpandedBackgroundProperty =
            DependencyProperty.Register(nameof(ExpandedBackground), typeof(Windows.UI.Color), typeof(DaisyExpandableCard),
                new PropertyMetadata(Windows.UI.Color.FromArgb(255, 17, 24, 39))); // #111827

        /// <summary>
        /// The background color of the expanded panel.
        /// </summary>
        public Windows.UI.Color ExpandedBackground
        {
            get => (Windows.UI.Color)GetValue(ExpandedBackgroundProperty);
            set => SetValue(ExpandedBackgroundProperty, value);
        }

        public static readonly DependencyProperty ActionButtonTextProperty =
            DependencyProperty.Register(nameof(ActionButtonText), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata("Play", OnActionButtonTextChanged));

        /// <summary>
        /// The text displayed on the action button.
        /// </summary>
        public string ActionButtonText
        {
            get => (string)GetValue(ActionButtonTextProperty);
            set => SetValue(ActionButtonTextProperty, value);
        }

        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(nameof(AccessibleText), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata(null, OnActionButtonTextChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        public static readonly DependencyProperty UseBatteriesIncludedModeProperty =
            DependencyProperty.Register(nameof(UseBatteriesIncludedMode), typeof(bool), typeof(DaisyExpandableCard),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, the control generates its visual content from convenience properties (Title, Subtitle, etc.).
        /// When false (default), use the Content and ExpandedContent properties directly.
        /// </summary>
        public bool UseBatteriesIncludedMode
        {
            get => (bool)GetValue(UseBatteriesIncludedModeProperty);
            set => SetValue(UseBatteriesIncludedModeProperty, value);
        }

        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register(nameof(IconData), typeof(string), typeof(DaisyExpandableCard),
                new PropertyMetadata(null));

        /// <summary>
        /// The path data for an icon displayed in the top-right corner of the main card.
        /// </summary>
        public string? IconData
        {
            get => (string?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public new static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(DaisyExpandableCard),
                new PropertyMetadata(96.0));

        /// <summary>
        /// The size (width and height) of the icon. Default is 96.
        /// </summary>
        public new double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpandableCard card)
            {
                card.UpdateExpandedState(true);
            }
        }

        private static void OnActionButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpandableCard card)
            {
                if (card._actionButton != null)
                {
                    card._actionButton.Content = card.ActionButtonText;
                }
                card.UpdateAutomationProperties();
            }
        }

        #endregion

        #region Template Handling

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Find template parts
            _solidExpandedWrapper = GetTemplateChild("PART_SolidExpandedWrapper") as Border;
            _solidExpandedContent = GetTemplateChild("PART_SolidExpandedContent") as ContentPresenter;

            // Build batteries-included content if enabled
            if (UseBatteriesIncludedMode)
            {
                BuildBatteriesIncludedContent();
            }

            // Initial state (no animation)
            UpdateExpandedState(false);
        }

        /// <summary>
        /// Builds the visual content from the convenience properties.
        /// </summary>
        private void BuildBatteriesIncludedContent()
        {
            // Build main card content
            var mainGrid = new Grid
            {
                Height = CardHeight,
                Width = CardWidth
            };

            // Background gradient border
            var gradientBorder = new Border
            {
                CornerRadius = new CornerRadius(16),
                Background = new Microsoft.UI.Xaml.Media.LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1),
                    GradientStops =
                    {
                        new Microsoft.UI.Xaml.Media.GradientStop { Color = GradientStart, Offset = 0 },
                        new Microsoft.UI.Xaml.Media.GradientStop { Color = GradientEnd, Offset = 1 }
                    }
                }
            };
            mainGrid.Children.Add(gradientBorder);

            // Add icon in top-right if IconData is provided
            if (!string.IsNullOrEmpty(IconData))
            {
                try
                {
                    var iconPath = new Microsoft.UI.Xaml.Shapes.Path
                    {
                        Data = FloweryPathHelpers.ParseGeometry(IconData),
                        Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)),
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        Width = IconSize,
                        Height = IconSize,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 8, 8, 0)
                    };
                    mainGrid.Children.Add(iconPath);
                }
                catch
                {
                    // Silently ignore invalid icon data
                }
            }

            // Content stack
            var contentStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(12)
            };

            if (!string.IsNullOrEmpty(Title))
            {
                contentStack.Children.Add(new TextBlock
                {
                    Text = Title,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                });
            }

            if (!string.IsNullOrEmpty(Subtitle))
            {
                contentStack.Children.Add(new TextBlock
                {
                    Text = Subtitle,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // Action button
            _actionButton = new DaisyButton
            {
                Content = ActionButtonText,
                Variant = DaisyButtonVariant.Primary,
                Padding = new Thickness(8, 4, 8, 4),
                MinHeight = 32,
                Command = ToggleCommand
            };
            contentStack.Children.Add(_actionButton);

            mainGrid.Children.Add(contentStack);
            Content = mainGrid;
            UpdateAutomationProperties();

            // Build expanded content
            var expandedBorder = new Border
            {
                Width = CardWidth,
                Height = CardHeight,
                Background = ReadLocalValue(ExpandedBackgroundProperty) == DependencyProperty.UnsetValue 
                    ? DaisyResourceLookup.GetBrush("DaisyBase200Brush") 
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(ExpandedBackground),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(0, 16, 16, 0)
            };

            var expandedStack = new StackPanel
            {
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!string.IsNullOrEmpty(ExpandedText))
            {
                expandedStack.Children.Add(new TextBlock
                {
                    Text = ExpandedText,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 10
                });
            }

            if (!string.IsNullOrEmpty(ExpandedSubtitle))
            {
                expandedStack.Children.Add(new TextBlock
                {
                    Text = ExpandedSubtitle,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    Opacity = 0.7,
                    FontSize = 9
                });
            }

            expandedBorder.Child = expandedStack;
            ExpandedContent = expandedBorder;
        }

        private void UpdateExpandedState(bool animate)
        {
            // Use solid wrapper (glass mode now handled by background changes on DaisyCard)
            var wrapper = _solidExpandedWrapper;
            var content = _solidExpandedContent;

            if (wrapper == null || content == null)
                return;

            var isExpanded = IsExpanded;

            // Update corner radius of the main content's first Border
            // Collapsed: all corners rounded, Expanded: right corners sharp
            UpdateContentCornerRadius(isExpanded);

            // Cancel any running animation
            _animationCts?.Cancel();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            if (!animate)
            {
                // Instant update without animation
                if (isExpanded)
                {
                    // Measure the content to get its desired width
                    content.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    var measuredWidth = content.DesiredSize.Width;
                    wrapper.Width = measuredWidth > 0 ? measuredWidth : 150; // Fallback
                    wrapper.Opacity = 1;
                }
                else
                {
                    wrapper.Width = 0;
                    wrapper.Opacity = 0;
                }
                return;
            }

            // Animate
            double startWidth = wrapper.ActualWidth;
            double targetWidth = 0;
            double startOpacity = wrapper.Opacity;
            double targetOpacity = isExpanded ? 1 : 0;

            if (isExpanded)
            {
                // Measure desired width
                content.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                targetWidth = content.DesiredSize.Width;

                // Fallback if measurement failed
                if (targetWidth <= 0 && ExpandedContent is FrameworkElement fe && fe.Width > 0)
                    targetWidth = fe.Width;
                if (targetWidth <= 0)
                    targetWidth = 150;
            }

            // If start width is NaN (Auto), treat as 0
            if (double.IsNaN(startWidth))
                startWidth = 0;

            // Run animation
            _ = AnimateExpandAsync(wrapper, startWidth, targetWidth, startOpacity, targetOpacity, token);
        }

        private async Task AnimateExpandAsync(Border wrapper, double startWidth, double targetWidth, double startOpacity, double targetOpacity, CancellationToken token)
        {
            var duration = AnimationDuration;
            var startTime = DateTime.Now;

            while (DateTime.Now - startTime < duration)
            {
                if (token.IsCancellationRequested)
                    return;

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var t = Math.Min(1.0, elapsed / duration.TotalMilliseconds);

                // Cubic ease out
                var easedT = 1 - Math.Pow(1 - t, 3);

                var currentWidth = startWidth + (targetWidth - startWidth) * easedT;
                var currentOpacity = startOpacity + (targetOpacity - startOpacity) * easedT;

                wrapper.Width = currentWidth;
                wrapper.Opacity = currentOpacity;

                await Task.Delay(16, token); // ~60fps
            }

            // Final state
            if (!token.IsCancellationRequested)
            {
                wrapper.Width = targetWidth;
                wrapper.Opacity = targetOpacity;
            }
        }

        /// <summary>
        /// Updates the corner radius of the first Border in the Content.
        /// When collapsed: all corners rounded. When expanded: right corners sharp.
        /// </summary>
        private void UpdateContentCornerRadius(bool isExpanded)
        {
            // Find the first Border in the Content
            Border? contentBorder = null;

            if (Content is Border border)
            {
                contentBorder = border;
            }
            else if (Content is Panel panel && panel.Children.Count > 0)
            {
                // Look for first Border child (common pattern: Grid containing a Border for the background)
                foreach (var child in panel.Children)
                {
                    if (child is Border b)
                    {
                        contentBorder = b;
                        break;
                    }
                }
            }

            if (contentBorder == null)
                return;

            // Get the base corner radius value from the current corner radius or default to 16
            var currentRadius = contentBorder.CornerRadius;
            var baseRadius = Math.Max(
                Math.Max(currentRadius.TopLeft, currentRadius.TopRight),
                Math.Max(currentRadius.BottomLeft, currentRadius.BottomRight));

            if (baseRadius <= 0)
                baseRadius = 16;

            if (isExpanded)
            {
                // Expanded: left corners rounded, right corners sharp (to connect with expanded panel)
                contentBorder.CornerRadius = new CornerRadius(baseRadius, 0, 0, baseRadius);
            }
            else
            {
                // Collapsed: all corners rounded
                contentBorder.CornerRadius = new CornerRadius(baseRadius);
            }
        }

        private void UpdateAutomationProperties()
        {
            if (_actionButton == null)
                return;

            var name = !string.IsNullOrWhiteSpace(AccessibleText) ? AccessibleText : ActionButtonText;
            DaisyAccessibility.SetAutomationNameOrClear(_actionButton, name);
        }

        #endregion
    }

    /// <summary>
    /// Simple ICommand implementation for toggle functionality.
    /// </summary>
    internal partial class SimpleCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<object?, bool>? _canExecute = canExecute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
