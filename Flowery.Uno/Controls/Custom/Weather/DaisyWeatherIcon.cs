namespace Flowery.Controls.Weather
{
    /// <summary>
    /// Animated weather condition icon with subtle animations for each weather type.
    /// </summary>
    public partial class DaisyWeatherIcon : DaisyBaseContentControl
    {
        private enum WeatherAnimationGroup
        {
            None,
            Sunny,
            Cloudy,
            Rainy,
            Snowy,
            Stormy,
            Windy,
            Foggy
        }

        private Path? _iconPath;
        private Canvas? _snowflakesCanvas;
        private Border? _flashOverlay;
        private Ellipse? _snow1;
        private Ellipse? _snow2;
        private Ellipse? _snow3;
        private Ellipse? _snow4;

        private Visual? _iconVisual;
        private Visual? _flashVisual;
        private Visual? _snow1Visual;
        private Visual? _snow2Visual;
        private Visual? _snow3Visual;
        private Visual? _snow4Visual;
        private Storyboard? _sunnyRotationStoryboard;

        private bool _isLoaded;
        private WeatherAnimationGroup _activeGroup;

        private static readonly float[] Snow1KeyTimes = [0f, 0.11f, 0.66f, 1f];
        private static readonly float[] Snow1XValues = [0f, 1f, 4f, 2f];
        private static readonly float[] Snow1YValues = [0f, 5f, 30f, 45f];
        private static readonly float[] Snow1OpacityValues = [0f, 0.7f, 0.5f, 0f];

        private static readonly float[] Snow2KeyTimes = [0f, 0.11f, 0.68f, 1f];
        private static readonly float[] Snow2XValues = [0f, -1f, -3f, -1f];
        private static readonly float[] Snow2YValues = [0f, 6f, 35f, 50f];
        private static readonly float[] Snow2OpacityValues = [0f, 0.6f, 0.4f, 0f];

        private static readonly float[] Snow3KeyTimes = [0f, 0.1f, 0.65f, 1f];
        private static readonly float[] Snow3XValues = [0f, 2f, 5f, 3f];
        private static readonly float[] Snow3YValues = [0f, 5f, 32f, 48f];
        private static readonly float[] Snow3OpacityValues = [0f, 0.6f, 0.4f, 0f];

        private static readonly float[] Snow4KeyTimes = [0f, 0.09f, 0.62f, 1f];
        private static readonly float[] Snow4XValues = [0f, -2f, -4f, -2f];
        private static readonly float[] Snow4YValues = [0f, 4f, 28f, 42f];
        private static readonly float[] Snow4OpacityValues = [0f, 0.5f, 0.35f, 0f];

        public DaisyWeatherIcon()
        {
            DefaultStyleKey = typeof(DaisyWeatherIcon);
            IsTabStop = false;
        }

        private void ApplyAll()
        {
            // Refresh Foreground from current theme (Background is transparent)
            var freshForeground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            if (freshForeground != null)
                Foreground = freshForeground;

            UpdateVisualState();
        }

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register(
                nameof(Condition),
                typeof(WeatherCondition),
                typeof(DaisyWeatherIcon),
                new PropertyMetadata(WeatherCondition.Unknown, OnVisualStateChanged));

        /// <summary>
        /// Weather condition to display.
        /// </summary>
        public WeatherCondition Condition
        {
            get => (WeatherCondition)GetValue(ConditionProperty);
            set => SetValue(ConditionProperty, value);
        }

        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.Register(
                nameof(IsAnimated),
                typeof(bool),
                typeof(DaisyWeatherIcon),
                new PropertyMetadata(true, OnVisualStateChanged));

        /// <summary>
        /// Whether animations are enabled. Default is true.
        /// </summary>
        public bool IsAnimated
        {
            get => (bool)GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                nameof(IconSize),
                typeof(double),
                typeof(DaisyWeatherIcon),
                new PropertyMetadata(64d, OnVisualStateChanged));

        /// <summary>
        /// Size of the icon in pixels. Default is 64.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherIcon icon)
            {
                icon.UpdateVisualState();
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_iconPath != null)
            {
                _iconPath.SizeChanged -= OnIconSizeChanged;
            }

            _iconPath = GetTemplateChild("PART_Icon") as Path;
            _snowflakesCanvas = GetTemplateChild("PART_Snowflakes") as Canvas;
            _flashOverlay = GetTemplateChild("PART_Flash") as Border;
            _snow1 = GetTemplateChild("PART_Snow1") as Ellipse;
            _snow2 = GetTemplateChild("PART_Snow2") as Ellipse;
            _snow3 = GetTemplateChild("PART_Snow3") as Ellipse;
            _snow4 = GetTemplateChild("PART_Snow4") as Ellipse;

            if (_iconPath != null)
            {
                _iconPath.SizeChanged += OnIconSizeChanged;
            }

            UpdateVisualState();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            _isLoaded = true;
            ApplyAll();
            UpdateVisualState();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            _isLoaded = false;
            StopAnimations();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        private void OnIconSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateIconCenterPoint();
        }

        private void UpdateVisualState()
        {
            if (!_isLoaded || _iconPath == null)
                return;

            UpdateIconData();
            EnsureVisuals();
            UpdateIconCenterPoint();

            var group = GetAnimationGroup(Condition);
            UpdateAuxVisibility(group);

            if (!IsAnimated || group == WeatherAnimationGroup.None)
            {
                StopAnimations();
                return;
            }

            if (_activeGroup == group)
                return;

            StopAnimations();
            StartAnimations(group);
        }

        private void UpdateIconData()
        {
            if (_iconPath == null)
                return;

            FloweryPathHelpers.TrySetPathData(
                _iconPath,
                () => GetIconGeometryForCondition(Condition),
                () => FloweryPathHelpers.CreateEllipseGeometry(12, 12, 10, 10));
        }

        private static Geometry GetIconGeometryForCondition(WeatherCondition condition)
        {
            return condition switch
            {
                _ => FloweryPathHelpers.GetWeatherConditionGeometry(condition)
            };
        }

        private void EnsureVisuals()
        {
            if (_iconPath != null && _iconVisual == null)
            {
                _iconVisual = ElementCompositionPreview.GetElementVisual(_iconPath);
            }

            if (_flashOverlay != null && _flashVisual == null)
            {
                _flashVisual = ElementCompositionPreview.GetElementVisual(_flashOverlay);
            }

            _snow1Visual ??= GetVisual(_snow1);
            _snow2Visual ??= GetVisual(_snow2);
            _snow3Visual ??= GetVisual(_snow3);
            _snow4Visual ??= GetVisual(_snow4);
        }

        private static Visual? GetVisual(UIElement? element)
        {
            return element == null ? null : ElementCompositionPreview.GetElementVisual(element);
        }

        private void UpdateIconCenterPoint()
        {
            if (_iconVisual == null || _iconPath == null)
                return;

            var width = _iconPath.ActualWidth;
            var height = _iconPath.ActualHeight;
            _iconVisual.CenterPoint = new Vector3((float)(width / 2), (float)(height / 2), 0);
        }

        private void UpdateAuxVisibility(WeatherAnimationGroup group)
        {
            if (_snowflakesCanvas != null)
            {
                _snowflakesCanvas.Visibility = group == WeatherAnimationGroup.Snowy
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (_flashOverlay != null)
            {
                _flashOverlay.Visibility = group == WeatherAnimationGroup.Stormy
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void StartAnimations(WeatherAnimationGroup group)
        {
            if (_iconVisual == null)
                return;

            _activeGroup = group;
            var compositor = _iconVisual.Compositor;

            switch (group)
            {
                case WeatherAnimationGroup.Sunny:
                    StartSunnyAnimation();
                    break;
                case WeatherAnimationGroup.Cloudy:
                    StartHorizontalDrift(compositor, _iconVisual, -2f, 2f, 4);
                    break;
                case WeatherAnimationGroup.Rainy:
                    StartVerticalBob(compositor, _iconVisual, 0f, 2f, 1.5);
                    break;
                case WeatherAnimationGroup.Snowy:
                    StartHorizontalDrift(compositor, _iconVisual, -1f, 1f, 3);
                    StartSnowflakeAnimations(compositor);
                    break;
                case WeatherAnimationGroup.Windy:
                    StartHorizontalDrift(compositor, _iconVisual, -3f, 3f, 2);
                    break;
                case WeatherAnimationGroup.Foggy:
                    StartOpacityPulse(compositor, _iconVisual, 0.7f, 1f, 4);
                    break;
                case WeatherAnimationGroup.Stormy:
                    StartStormyAnimations(compositor);
                    break;
            }
        }

        private void StopAnimations()
        {
            PlatformCompatibility.StopRotationAnimation(_iconPath, _iconVisual, _sunnyRotationStoryboard);
            _sunnyRotationStoryboard = null;

            if (_iconVisual != null)
            {
                _iconVisual.StopAnimation("Offset.X");
                _iconVisual.StopAnimation("Offset.Y");
                _iconVisual.StopAnimation("Opacity");
                _iconVisual.Offset = Vector3.Zero;
                _iconVisual.Opacity = 1f;
            }

            if (_flashVisual != null)
            {
                _flashVisual.StopAnimation("Opacity");
                _flashVisual.Opacity = 0f;
            }

            ResetSnowflake(_snow1Visual);
            ResetSnowflake(_snow2Visual);
            ResetSnowflake(_snow3Visual);
            ResetSnowflake(_snow4Visual);

            _activeGroup = WeatherAnimationGroup.None;
        }

        private static void ResetSnowflake(Visual? visual)
        {
            if (visual == null)
                return;

            visual.StopAnimation("Offset.X");
            visual.StopAnimation("Offset.Y");
            visual.StopAnimation("Opacity");
            visual.Offset = Vector3.Zero;
            visual.Opacity = 0f;
        }

        private void StartSunnyAnimation()
        {
            if (_iconVisual == null || _iconPath == null)
                return;

            _sunnyRotationStoryboard?.Stop();
            _sunnyRotationStoryboard = PlatformCompatibility.StartRotationAnimation(_iconPath, _iconVisual, 20);
        }

        private static void StartHorizontalDrift(Compositor compositor, Visual visual, float from, float to, double seconds)
        {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, from);
            animation.InsertKeyFrame(0.5f, to);
            animation.InsertKeyFrame(1f, from);
            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Offset.X", animation);
        }

        private static void StartVerticalBob(Compositor compositor, Visual visual, float from, float to, double seconds)
        {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, from);
            animation.InsertKeyFrame(0.5f, to);
            animation.InsertKeyFrame(1f, from);
            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Offset.Y", animation);
        }

        private static void StartOpacityPulse(Compositor compositor, Visual visual, float from, float to, double seconds)
        {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, from);
            animation.InsertKeyFrame(0.5f, to);
            animation.InsertKeyFrame(1f, from);
            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", animation);
        }

        private void StartStormyAnimations(Compositor compositor)
        {
            if (_iconVisual == null || _flashVisual == null)
                return;

            var iconAnimation = compositor.CreateScalarKeyFrameAnimation();
            iconAnimation.InsertKeyFrame(0f, 1f);
            iconAnimation.InsertKeyFrame(0.3f, 1f);
            iconAnimation.InsertKeyFrame(0.32f, 0.5f);
            iconAnimation.InsertKeyFrame(0.34f, 1f);
            iconAnimation.InsertKeyFrame(0.36f, 0.6f);
            iconAnimation.InsertKeyFrame(0.38f, 1f);
            iconAnimation.InsertKeyFrame(1f, 1f);
            iconAnimation.Duration = TimeSpan.FromSeconds(3);
            iconAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_iconVisual, "Opacity", iconAnimation);

            var flashAnimation = compositor.CreateScalarKeyFrameAnimation();
            flashAnimation.InsertKeyFrame(0f, 0f);
            flashAnimation.InsertKeyFrame(0.31f, 0f);
            flashAnimation.InsertKeyFrame(0.32f, 0.3f);
            flashAnimation.InsertKeyFrame(0.34f, 0f);
            flashAnimation.InsertKeyFrame(0.35f, 0.2f);
            flashAnimation.InsertKeyFrame(0.37f, 0f);
            flashAnimation.InsertKeyFrame(1f, 0f);
            flashAnimation.Duration = TimeSpan.FromSeconds(3);
            flashAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_flashVisual, "Opacity", flashAnimation);
        }

        private void StartSnowflakeAnimations(Compositor compositor)
        {
            StartSnowflakeAnimation(compositor, _snow1Visual, 1.8, 0.0,
                Snow1KeyTimes,
                Snow1XValues,
                Snow1YValues,
                Snow1OpacityValues);

            StartSnowflakeAnimation(compositor, _snow2Visual, 2.2, 0.4,
                Snow2KeyTimes,
                Snow2XValues,
                Snow2YValues,
                Snow2OpacityValues);

            StartSnowflakeAnimation(compositor, _snow3Visual, 2.0, 0.8,
                Snow3KeyTimes,
                Snow3XValues,
                Snow3YValues,
                Snow3OpacityValues);

            StartSnowflakeAnimation(compositor, _snow4Visual, 1.6, 1.2,
                Snow4KeyTimes,
                Snow4XValues,
                Snow4YValues,
                Snow4OpacityValues);
        }

        private static void StartSnowflakeAnimation(
            Compositor compositor,
            Visual? visual,
            double durationSeconds,
            double delaySeconds,
            float[] keyTimes,
            float[] xValues,
            float[] yValues,
            float[] opacityValues)
        {
            if (visual == null)
                return;

            var xAnimation = compositor.CreateScalarKeyFrameAnimation();
            var yAnimation = compositor.CreateScalarKeyFrameAnimation();
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();

            for (var i = 0; i < keyTimes.Length; i++)
            {
                xAnimation.InsertKeyFrame(keyTimes[i], xValues[i]);
                yAnimation.InsertKeyFrame(keyTimes[i], yValues[i]);
                opacityAnimation.InsertKeyFrame(keyTimes[i], opacityValues[i]);
            }

            var duration = TimeSpan.FromSeconds(durationSeconds);
            var delay = TimeSpan.FromSeconds(delaySeconds);

            xAnimation.Duration = duration;
            yAnimation.Duration = duration;
            opacityAnimation.Duration = duration;

            xAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            yAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Offset.X", xAnimation, delay);
            PlatformCompatibility.StartAnimation(visual, "Offset.Y", yAnimation, delay);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacityAnimation, delay);
        }

        private static WeatherAnimationGroup GetAnimationGroup(WeatherCondition condition)
        {
            return condition switch
            {
                WeatherCondition.Sunny or WeatherCondition.Clear => WeatherAnimationGroup.Sunny,
                WeatherCondition.PartlyCloudy or WeatherCondition.Cloudy or WeatherCondition.Overcast => WeatherAnimationGroup.Cloudy,
                WeatherCondition.LightRain or WeatherCondition.Rain or WeatherCondition.HeavyRain or WeatherCondition.Drizzle or WeatherCondition.Showers => WeatherAnimationGroup.Rainy,
                WeatherCondition.LightSnow or WeatherCondition.Snow or WeatherCondition.HeavySnow or WeatherCondition.Sleet or WeatherCondition.FreezingRain or WeatherCondition.Hail => WeatherAnimationGroup.Snowy,
                WeatherCondition.Thunderstorm => WeatherAnimationGroup.Stormy,
                WeatherCondition.Windy => WeatherAnimationGroup.Windy,
                WeatherCondition.Mist or WeatherCondition.Fog => WeatherAnimationGroup.Foggy,
                _ => WeatherAnimationGroup.None
            };
        }

    }
}
