namespace Flowery.Helpers
{
    /// <summary>
    /// Attached properties to paint text with an ImageBrush and optionally animate the brush transform.
    /// Usage in XAML: services:FloweryTextFillEffects.ImageSource="ms-appx:///Assets/hero.jpg"
    /// </summary>
    public static class FloweryTextFillEffects
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextBlock, Storyboard> _storyboards = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextBlock, ImageBrush> _brushes = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextBlock, Brush> _originalForegrounds = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextBlock, CompositeTransform> _relativeTransforms = new();

        #region ImageSource Attached Property

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.RegisterAttached(
                "ImageSource",
                typeof(ImageSource),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(null, OnImageSourceChanged));

        public static ImageSource? GetImageSource(TextBlock element)
        {
            return element.GetValue(ImageSourceProperty) as ImageSource;
        }

        public static void SetImageSource(TextBlock element, ImageSource? value)
        {
            element.SetValue(ImageSourceProperty, value);
        }

        #endregion

        #region Animate Attached Property

        public static readonly DependencyProperty AnimateProperty =
            DependencyProperty.RegisterAttached(
                "Animate",
                typeof(bool),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(false, OnEffectSettingsChanged));

        public static bool GetAnimate(TextBlock element)
        {
            return (bool)element.GetValue(AnimateProperty);
        }

        public static void SetAnimate(TextBlock element, bool value)
        {
            element.SetValue(AnimateProperty, value);
        }

        #endregion

        #region AutoStart Attached Property

        public static readonly DependencyProperty AutoStartProperty =
            DependencyProperty.RegisterAttached(
                "AutoStart",
                typeof(bool),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(true, OnEffectSettingsChanged));

        public static bool GetAutoStart(TextBlock element)
        {
            return (bool)element.GetValue(AutoStartProperty);
        }

        public static void SetAutoStart(TextBlock element, bool value)
        {
            element.SetValue(AutoStartProperty, value);
        }

        #endregion

        #region Duration Attached Property

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(double),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(6.0, OnEffectParameterChanged));

        public static double GetDuration(TextBlock element)
        {
            return (double)element.GetValue(DurationProperty);
        }

        public static void SetDuration(TextBlock element, double value)
        {
            element.SetValue(DurationProperty, Math.Max(0.2, value));
        }

        #endregion

        #region PanX Attached Property

        public static readonly DependencyProperty PanXProperty =
            DependencyProperty.RegisterAttached(
                "PanX",
                typeof(double),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(0.2, OnEffectParameterChanged));

        public static double GetPanX(TextBlock element)
        {
            return (double)element.GetValue(PanXProperty);
        }

        public static void SetPanX(TextBlock element, double value)
        {
            element.SetValue(PanXProperty, value);
        }

        #endregion

        #region PanY Attached Property

        public static readonly DependencyProperty PanYProperty =
            DependencyProperty.RegisterAttached(
                "PanY",
                typeof(double),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(0.0, OnEffectParameterChanged));

        public static double GetPanY(TextBlock element)
        {
            return (double)element.GetValue(PanYProperty);
        }

        public static void SetPanY(TextBlock element, double value)
        {
            element.SetValue(PanYProperty, value);
        }

        #endregion

        #region AutoReverse Attached Property

        public static readonly DependencyProperty AutoReverseProperty =
            DependencyProperty.RegisterAttached(
                "AutoReverse",
                typeof(bool),
                typeof(FloweryTextFillEffects),
                new PropertyMetadata(true, OnEffectParameterChanged));

        public static bool GetAutoReverse(TextBlock element)
        {
            return (bool)element.GetValue(AutoReverseProperty);
        }

        public static void SetAutoReverse(TextBlock element, bool value)
        {
            element.SetValue(AutoReverseProperty, value);
        }

        #endregion

        #region Effect Management

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
            {
                return;
            }

            if (e.NewValue is not ImageSource source)
            {
                StopEffect(textBlock);
                RestoreOriginalForeground(textBlock);
                return;
            }

            if (ApplyImageBrush(textBlock, source))
            {
                StartIfConfigured(textBlock);
            }
            else
            {
                StopEffect(textBlock);
            }
        }

        private static void OnEffectSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
            {
                return;
            }

            StartIfConfigured(textBlock);
        }

        private static void OnEffectParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
            {
                return;
            }

            if (_storyboards.TryGetValue(textBlock, out _))
            {
                StartEffect(textBlock);
            }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBlock textBlock)
            {
                return;
            }

            textBlock.Loaded -= OnElementLoaded;
            StartEffect(textBlock);
            textBlock.Unloaded += OnElementUnloaded;
        }

        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBlock textBlock)
            {
                return;
            }

            textBlock.Unloaded -= OnElementUnloaded;
            StopEffect(textBlock);
        }

        private static void StartIfConfigured(TextBlock textBlock)
        {
            if (GetImageSource(textBlock) == null)
            {
                StopEffect(textBlock);
                return;
            }

            if (!GetAnimate(textBlock) || !GetAutoStart(textBlock))
            {
                StopEffect(textBlock);
                return;
            }

            if (textBlock.IsLoaded)
            {
                StartEffect(textBlock);
            }
            else
            {
                textBlock.Loaded -= OnElementLoaded;
                textBlock.Loaded += OnElementLoaded;
            }
        }

        private static bool ApplyImageBrush(TextBlock textBlock, ImageSource source)
        {
#if __SKIA__ || HAS_UNO
            _ = source;
            RestoreOriginalForeground(textBlock);
            return false;
#else
            EnsureOriginalForeground(textBlock);

            var brush = GetOrCreateBrush(textBlock);
            brush.ImageSource = source;
            brush.RelativeTransform = GetOrCreateRelativeTransform(textBlock);
            try
            {
                textBlock.Foreground = brush;
                return true;
            }
            catch (NotSupportedException)
            {
                RestoreOriginalForeground(textBlock);
                return false;
            }
#endif
        }

        private static ImageBrush GetOrCreateBrush(TextBlock textBlock)
        {
            if (_brushes.TryGetValue(textBlock, out ImageBrush? brush))
            {
                return brush;
            }

            brush = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            _brushes.Add(textBlock, brush);
            return brush;
        }

        private static CompositeTransform GetOrCreateRelativeTransform(TextBlock textBlock)
        {
            if (_relativeTransforms.TryGetValue(textBlock, out CompositeTransform? transform))
            {
                return transform;
            }

            transform = new CompositeTransform
            {
                CenterX = 0.5,
                CenterY = 0.5
            };
            _relativeTransforms.Add(textBlock, transform);
            return transform;
        }

        private static void EnsureOriginalForeground(TextBlock textBlock)
        {
            if (_originalForegrounds.TryGetValue(textBlock, out _))
            {
                return;
            }

            _originalForegrounds.Add(textBlock, textBlock.Foreground);
        }

        private static void RestoreOriginalForeground(TextBlock textBlock)
        {
            if (_originalForegrounds.TryGetValue(textBlock, out Brush? original))
            {
                textBlock.Foreground = original;
                _ = _originalForegrounds.Remove(textBlock);
            }
        }

        #endregion

        #region Public API

        public static void StartEffect(TextBlock textBlock)
        {
            StopEffect(textBlock);

            var source = GetImageSource(textBlock);
            if (source == null)
            {
                return;
            }

            textBlock.Loaded -= OnElementLoaded;
            textBlock.Unloaded -= OnElementUnloaded;

            if (!ApplyImageBrush(textBlock, source))
            {
                return;
            }

            if (!GetAnimate(textBlock))
            {
                return;
            }

            double panX = Math.Max(-1.0, Math.Min(1.0, GetPanX(textBlock)));
            double panY = Math.Max(-1.0, Math.Min(1.0, GetPanY(textBlock)));

            if (Math.Abs(panX) <= 0.0 && Math.Abs(panY) <= 0.0)
            {
                return;
            }

            var transform = GetOrCreateRelativeTransform(textBlock);
            transform.TranslateX = 0;
            transform.TranslateY = 0;
            transform.ScaleX = 1.0 + (2.0 * Math.Abs(panX));
            transform.ScaleY = 1.0 + (2.0 * Math.Abs(panY));
            var storyboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0.2, GetDuration(textBlock)));
            bool autoReverse = GetAutoReverse(textBlock);

            if (Math.Abs(panX) > 0.0)
            {
                var animX = new DoubleAnimation
                {
                    From = 0,
                    To = panX,
                    Duration = new Duration(duration),
                    AutoReverse = autoReverse,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(animX, transform);
                Storyboard.SetTargetProperty(animX, "TranslateX");
                storyboard.Children.Add(animX);
            }

            if (Math.Abs(panY) > 0.0)
            {
                var animY = new DoubleAnimation
                {
                    From = 0,
                    To = panY,
                    Duration = new Duration(duration),
                    AutoReverse = autoReverse,
                    EnableDependentAnimation = true
                };
                Storyboard.SetTarget(animY, transform);
                Storyboard.SetTargetProperty(animY, "TranslateY");
                storyboard.Children.Add(animY);
            }

            _storyboards.AddOrUpdate(textBlock, storyboard);
            storyboard.Begin();

            textBlock.Unloaded += OnElementUnloaded;
        }

        public static void StopEffect(TextBlock textBlock)
        {
            if (_storyboards.TryGetValue(textBlock, out Storyboard? storyboard))
            {
                storyboard.Stop();
                _ = _storyboards.Remove(textBlock);
            }
        }

        public static void TryStartEffect(UIElement element)
        {
            var textBlock = ResolveTextTarget(element);
            if (textBlock == null || GetImageSource(textBlock) == null || !GetAutoStart(textBlock))
            {
                return;
            }

            StartEffect(textBlock);
        }

        public static void TryStopEffect(UIElement element)
        {
            var textBlock = ResolveTextTarget(element);
            if (textBlock == null)
            {
                return;
            }

            StopEffect(textBlock);
        }

        #endregion

        private static TextBlock? ResolveTextTarget(UIElement element)
        {
            UIElement current = element;

            while (true)
            {
                switch (current)
                {
                    case TextBlock textBlock:
                        return textBlock;
                    case Border { Child: UIElement child }:
                        current = child;
                        continue;
                    case ContentControl { Content: UIElement child }:
                        current = child;
                        continue;
                    case ContentPresenter { Content: UIElement child }:
                        current = child;
                        continue;
                    case Panel { Children: [var onlyChild] }:
                        current = onlyChild;
                        continue;
                    default:
                        return null;
                }
            }
        }
    }
}
