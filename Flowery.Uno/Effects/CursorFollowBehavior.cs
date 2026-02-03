using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

#pragma warning disable IDE0051 // Remove unused private members (attached property setters are used from XAML)
#pragma warning disable IDE0060 // Remove unused parameter (attached property setters are used from XAML)
#pragma warning disable CA1822 // Mark members as static (SetStiffness/SetDamping are public API for attached properties)

namespace Flowery.Effects
{
    /// <summary>
    /// Creates a cursor-following element that smoothly tracks pointer position.
    /// Apply to a <see cref="Panel"/> (e.g. <see cref="Grid"/>) via attached properties.
    /// </summary>
    public static class CursorFollowBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty FollowerSizeProperty =
            DependencyProperty.RegisterAttached(
                "FollowerSize",
                typeof(double),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(20d, OnFollowerVisualChanged));

        public static readonly DependencyProperty FollowerBrushProperty =
            DependencyProperty.RegisterAttached(
                "FollowerBrush",
                typeof(Brush),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(null, OnFollowerVisualChanged));

        public static readonly DependencyProperty StiffnessProperty =
            DependencyProperty.RegisterAttached(
                "Stiffness",
                typeof(double),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(0.15d));

        public static readonly DependencyProperty DampingProperty =
            DependencyProperty.RegisterAttached(
                "Damping",
                typeof(double),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(0.85d));

        public static readonly DependencyProperty FollowerShapeProperty =
            DependencyProperty.RegisterAttached(
                "FollowerShape",
                typeof(FollowerShape),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(FollowerShape.Circle, OnFollowerVisualChanged));

        public static readonly DependencyProperty FollowerOpacityProperty =
            DependencyProperty.RegisterAttached(
                "FollowerOpacity",
                typeof(double),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(1.0d));

        private static readonly DependencyProperty FollowerElementProperty =
            DependencyProperty.RegisterAttached(
                "FollowerElement",
                typeof(FrameworkElement),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached(
                "Timer",
                typeof(DispatcherTimer),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty CurrentPosProperty =
            DependencyProperty.RegisterAttached(
                "CurrentPos",
                typeof(Point),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(default(Point)));

        private static readonly DependencyProperty TargetPosProperty =
            DependencyProperty.RegisterAttached(
                "TargetPos",
                typeof(Point),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(default(Point)));

        private static readonly DependencyProperty VelocityProperty =
            DependencyProperty.RegisterAttached(
                "Velocity",
                typeof(Point),
                typeof(CursorFollowBehavior),
                new PropertyMetadata(default(Point)));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static double GetFollowerSize(DependencyObject element) => (double)element.GetValue(FollowerSizeProperty);
        public static void SetFollowerSize(DependencyObject element, double value) => element.SetValue(FollowerSizeProperty, value);

        public static Brush GetFollowerBrush(DependencyObject element) => (Brush)element.GetValue(FollowerBrushProperty);
        public static void SetFollowerBrush(DependencyObject element, Brush value) => element.SetValue(FollowerBrushProperty, value);

        public static double GetStiffness(DependencyObject element) => (double)element.GetValue(StiffnessProperty);
        public static void SetStiffness(DependencyObject element, double value) => element.SetValue(StiffnessProperty, value);

        public static double GetDamping(DependencyObject element) => (double)element.GetValue(DampingProperty);
        public static void SetDamping(DependencyObject element, double value) => element.SetValue(DampingProperty, value);

        public static FollowerShape GetFollowerShape(DependencyObject element) => (FollowerShape)element.GetValue(FollowerShapeProperty);
        public static void SetFollowerShape(DependencyObject element, FollowerShape value) => element.SetValue(FollowerShapeProperty, value);

        public static double GetFollowerOpacity(DependencyObject element) => (double)element.GetValue(FollowerOpacityProperty);
        public static void SetFollowerOpacity(DependencyObject element, double value) => element.SetValue(FollowerOpacityProperty, value);

        /// <summary>
        /// Programmatically sets the target position for the follower.
        /// Coordinates are relative to the panel.
        /// </summary>
        public static void SetTargetPosition(Panel? panel, double x, double y)
        {
            if (panel is null)
                return;

            var size = GetFollowerSize(panel);
            panel.SetValue(TargetPosProperty, new Point(x - size / 2, y - size / 2));
        }

        public static void ShowFollower(Panel? panel)
        {
            if (panel is null)
                return;

            if (panel.GetValue(FollowerElementProperty) is FrameworkElement follower)
                follower.Opacity = GetFollowerOpacity(panel);
        }

        public static void HideFollower(Panel? panel)
        {
            if (panel is null)
                return;

            if (panel.GetValue(FollowerElementProperty) is FrameworkElement follower)
                follower.Opacity = 0;
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            var enabled = e.NewValue is true;
            if (enabled)
            {
                element.Loaded += OnLoaded;
                element.Unloaded += OnUnloaded;
                element.DispatcherQueue?.TryEnqueue(() => Setup(element));
            }
            else
            {
                element.Loaded -= OnLoaded;
                element.Unloaded -= OnUnloaded;
                Cleanup(element);
            }
        }

        private static void OnFollowerVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe && GetIsEnabled(fe))
            {
                Cleanup(fe);
                Setup(fe);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && GetIsEnabled(fe))
                Setup(fe);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
                Cleanup(fe);
        }

        private static void Setup(FrameworkElement element)
        {
            if (!GetIsEnabled(element))
                return;

            if (element.GetValue(TimerProperty) is DispatcherTimer)
                return;

            if (element is not Panel panel)
                return;

            var follower = CreateFollower(panel);
            panel.Children.Add(follower);
            panel.SetValue(FollowerElementProperty, follower);

            element.PointerMoved += OnPointerMoved;
            element.PointerEntered += OnPointerEntered;
            element.PointerExited += OnPointerExited;
            element.PointerPressed += OnPointerPressed;
            element.PointerReleased += OnPointerReleased;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) => UpdateFollower(panel);
            panel.SetValue(TimerProperty, timer);
            timer.Start();
        }

        private static void Cleanup(FrameworkElement element)
        {
            element.PointerMoved -= OnPointerMoved;
            element.PointerEntered -= OnPointerEntered;
            element.PointerExited -= OnPointerExited;
            element.PointerPressed -= OnPointerPressed;
            element.PointerReleased -= OnPointerReleased;

            if (element.GetValue(TimerProperty) is DispatcherTimer timer)
            {
                timer.Stop();
                element.ClearValue(TimerProperty);
            }

            if (element is Panel panel && panel.GetValue(FollowerElementProperty) is FrameworkElement follower)
            {
                panel.Children.Remove(follower);
                panel.ClearValue(FollowerElementProperty);
            }
        }

        private static FrameworkElement CreateFollower(Panel panel)
        {
            var size = GetFollowerSize(panel);
            var brush = GetFollowerBrush(panel);
            var shape = GetFollowerShape(panel);

            FrameworkElement follower = shape switch
            {
                FollowerShape.Square => new Rectangle
                {
                    Width = size,
                    Height = size,
                    Fill = brush,
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    RenderTransform = new TranslateTransform(),
                    Opacity = 0
                },
                FollowerShape.Ring => new Ellipse
                {
                    Width = size,
                    Height = size,
                    Stroke = brush,
                    StrokeThickness = Math.Max(2, size / 8),
                    Fill = null,
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    RenderTransform = new TranslateTransform(),
                    Opacity = 0
                },
                _ => new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = brush,
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    RenderTransform = new TranslateTransform(),
                    Opacity = 0
                }
            };

            return follower;
        }

        private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Panel panel)
                return;

            if (panel.GetValue(FollowerElementProperty) is not FrameworkElement follower)
                return;

            var pos = e.GetCurrentPoint(panel).Position;
            InitializeSpring(panel, pos.X, pos.Y);
            follower.Opacity = GetFollowerOpacity(panel);
        }

        private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Panel panel)
                HideFollower(panel);
        }

        private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Panel panel)
                return;

            var pos = e.GetCurrentPoint(panel).Position;
            InitializeSpring(panel, pos.X, pos.Y);
            ShowFollower(panel);
        }

        private static void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Panel panel)
                HideFollower(panel);
        }

        private static void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Panel panel)
                return;

            var pos = e.GetCurrentPoint(panel).Position;
            SetTargetPosition(panel, pos.X, pos.Y);
        }

        private static void InitializeSpring(Panel panel, double x, double y)
        {
            var size = GetFollowerSize(panel);
            var initial = new Point(x - size / 2, y - size / 2);
            panel.SetValue(CurrentPosProperty, initial);
            panel.SetValue(TargetPosProperty, initial);
            panel.SetValue(VelocityProperty, default(Point));

            if (panel.GetValue(FollowerElementProperty) is FrameworkElement { RenderTransform: TranslateTransform tt })
            {
                tt.X = initial.X;
                tt.Y = initial.Y;
            }
        }

        private static void UpdateFollower(Panel panel)
        {
            if (panel.GetValue(FollowerElementProperty) is not FrameworkElement follower)
                return;

            var current = (Point)panel.GetValue(CurrentPosProperty);
            var target = (Point)panel.GetValue(TargetPosProperty);
            var velocity = (Point)panel.GetValue(VelocityProperty);

            var stiffness = GetStiffness(panel);
            var damping = GetDamping(panel);

            var dx = target.X - current.X;
            var dy = target.Y - current.Y;

            var ax = dx * stiffness;
            var ay = dy * stiffness;

            var vx = (velocity.X + ax) * damping;
            var vy = (velocity.Y + ay) * damping;

            var newX = current.X + vx;
            var newY = current.Y + vy;

            panel.SetValue(CurrentPosProperty, new Point(newX, newY));
            panel.SetValue(VelocityProperty, new Point(vx, vy));

            if (follower.RenderTransform is TranslateTransform transform)
            {
                transform.X = newX;
                transform.Y = newY;
            }
        }
    }

    /// <summary>
    /// Shape options for the cursor follower element.
    /// </summary>
    public enum FollowerShape
    {
        Circle,
        Square,
        Ring
    }
}
