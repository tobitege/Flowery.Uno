using System;
using Flowery.Theming;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// A Skeleton control styled after DaisyUI's Skeleton component.
    /// Used to show a loading state placeholder with a pulsing animation.
    /// </summary>
    public partial class DaisySkeleton : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _backgroundBorder;
        private ContentPresenter? _contentPresenter;
        private UIElement? _animationElement;
        private ScalarKeyFrameAnimation? _pulseAnimation;
        private Visual? _pulseVisual;
        private bool _isAnimating;
        private FrameworkElement? _pendingAnimationTarget;
        private object? _userContent;
        private bool _isUpdatingContent;
        public DaisySkeleton()
        {
            // Default CornerRadius
            CornerRadius = new CornerRadius(4);

            // Ensure content stretches to fill Width/Height
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            IsTabStop = false;

        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();
            StopAnimation();
            StartAnimation();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopAnimation();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _backgroundBorder ?? base.GetNeumorphicHostElement();
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (_isUpdatingContent || ReferenceEquals(newContent, _rootGrid))
                return;

            _userContent = newContent;
            UpdateContentPresenter();
            EnsureRootContent();
        }

        #region IsTextMode
        public static readonly DependencyProperty IsTextModeProperty =
            DependencyProperty.Register(
                nameof(IsTextMode),
                typeof(bool),
                typeof(DaisySkeleton),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, animates text content instead of background.
        /// </summary>
        public bool IsTextMode
        {
            get => (bool)GetValue(IsTextModeProperty);
            set => SetValue(IsTextModeProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySkeleton skeleton)
            {
                skeleton.StopAnimation();
                skeleton.ApplyAll();
                skeleton.StartAnimation();
            }
        }

        private void BuildVisualTree()
        {
            if (_backgroundBorder != null)
                return;

            _userContent = Content;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Background skeleton border - must stretch to fill size
            _backgroundBorder = new Border
            {
                CornerRadius = CornerRadius,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _rootGrid.Children.Add(_backgroundBorder);

            // Content presenter for text mode
            _contentPresenter = new ContentPresenter
            {
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _rootGrid.Children.Add(_contentPresenter);

            _isUpdatingContent = true;
            Content = _rootGrid;
            _isUpdatingContent = false;
            UpdateContentPresenter();
        }

        private void ApplyAll()
        {
            if (_backgroundBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _backgroundBorder.CornerRadius = CornerRadius;

            if (IsTextMode)
            {
                // Text mode: transparent background, show content
                _backgroundBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                _backgroundBorder.Visibility = Visibility.Collapsed;

                if (_contentPresenter != null)
                {
                    _contentPresenter.Content = _userContent;
                    _contentPresenter.Visibility = Visibility.Visible;
                    _contentPresenter.Foreground = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");
                }
            }
            else
            {
                // Normal mode: show background, hide content
                _backgroundBorder.Background = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
                _backgroundBorder.Visibility = Visibility.Visible;

                if (_contentPresenter != null)
                {
                    _contentPresenter.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void StartAnimation()
        {
            if (_isAnimating && _pulseVisual != null)
                return;

            var target = ResolveAnimationTarget();
            if (target == null)
                return;

            if (TryStartPulseAnimation(target))
                return;

            if (_rootGrid != null && !ReferenceEquals(target, _rootGrid) && TryStartPulseAnimation(_rootGrid))
                return;

            if (!ReferenceEquals(target, this))
            {
                _ = TryStartPulseAnimation(this);
            }
        }

        private void UpdateContentPresenter()
        {
            if (_contentPresenter == null)
                return;

            _contentPresenter.Content = _userContent;
        }

        private void EnsureRootContent()
        {
            if (_rootGrid == null || ReferenceEquals(Content, _rootGrid))
                return;

            _isUpdatingContent = true;
            Content = _rootGrid;
            _isUpdatingContent = false;
        }

        private void StopAnimation()
        {
            _isAnimating = false;
            ClearPendingAnimationTarget();

            try
            {
                if (_pulseVisual != null)
                {
                    _pulseVisual.StopAnimation("Opacity");
                    _pulseVisual.Opacity = 1f;
                    _pulseVisual = null;
                }

                _pulseAnimation = null;
                _animationElement = null;

                if (_backgroundBorder != null)
                {
                    var visual = ElementCompositionPreview.GetElementVisual(_backgroundBorder);
                    visual.StopAnimation("Opacity");
                }

                if (_contentPresenter != null)
                {
                    var visual = ElementCompositionPreview.GetElementVisual(_contentPresenter);
                    visual.StopAnimation("Opacity");
                }
            }
            catch
            {
                // Ignore errors when stopping animation
            }
        }

        private UIElement? ResolveAnimationTarget()
        {
            if (IsTextMode && _contentPresenter != null)
                return _contentPresenter;

            if (!IsTextMode && _backgroundBorder != null)
                return _backgroundBorder;

            return (UIElement?)_rootGrid ?? this;
        }

        private bool TryStartPulseAnimation(UIElement target)
        {
            if (target is FrameworkElement element && !element.IsLoaded)
            {
                QueueStartWhenLoaded(element);
                return false;
            }

            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                var compositor = visual.Compositor;

                _pulseVisual?.StopAnimation("Opacity");

                // Create opacity pulse animation
                _pulseAnimation = compositor.CreateScalarKeyFrameAnimation();
                _pulseAnimation.InsertKeyFrame(0f, 1f);
                _pulseAnimation.InsertKeyFrame(0.5f, 0.4f);
                _pulseAnimation.InsertKeyFrame(1f, 1f);
                _pulseAnimation.Duration = TimeSpan.FromMilliseconds(1500);
                _pulseAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

                PlatformCompatibility.StartAnimation(visual, "Opacity", _pulseAnimation);
                _pulseVisual = visual;
                _animationElement = target;
                _isAnimating = true;
                ClearPendingAnimationTarget();
                return true;
            }
            catch
            {
                _isAnimating = false;
                return false;
            }
        }

        private void QueueStartWhenLoaded(FrameworkElement target)
        {
            if (ReferenceEquals(_pendingAnimationTarget, target))
                return;

            ClearPendingAnimationTarget();
            _pendingAnimationTarget = target;
            target.Loaded += OnAnimationTargetLoaded;
        }

        private void ClearPendingAnimationTarget()
        {
            if (_pendingAnimationTarget == null)
                return;

            _pendingAnimationTarget.Loaded -= OnAnimationTargetLoaded;
            _pendingAnimationTarget = null;
        }

        private void OnAnimationTargetLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Loaded -= OnAnimationTargetLoaded;
            }

            if (!ReferenceEquals(sender, _pendingAnimationTarget))
                return;

            _pendingAnimationTarget = null;
            StartAnimation();
        }

    }
}
