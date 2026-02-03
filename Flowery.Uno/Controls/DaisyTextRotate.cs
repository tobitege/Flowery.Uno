using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Flowery.Controls
{
    /// <summary>
    /// A text rotation control that cycles through items with an animation.
    /// Similar to daisyUI's text-rotate component.
    /// </summary>
    [ContentProperty(Name = nameof(Items))]
    public partial class DaisyTextRotate : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private ContentPresenter? _currentPresenter;
        private ContentPresenter? _nextPresenter;
        private TranslateTransform? _currentTransform;
        private TranslateTransform? _nextTransform;

        private CancellationTokenSource? _loopCts;
        private bool _isPausedByHover;
        private bool _isPointerOverCurrent;
        private bool _isPointerOverNext;

        public DaisyTextRotate()
        {
            Items.CollectionChanged += OnItemsCollectionChanged;
            IsTabStop = false;
        }

        /// <summary>
        /// Gets the items to rotate through.
        /// </summary>
        public ObservableCollection<object> Items { get; } = [];

        #region Duration
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(double),
                typeof(DaisyTextRotate),
                new PropertyMetadata(10000.0, OnTimingChanged));

        /// <summary>
        /// Gets or sets the total duration of the animation loop in milliseconds.
        /// </summary>
        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }
        #endregion

        #region TransitionDuration
        public static readonly DependencyProperty TransitionDurationProperty =
            DependencyProperty.Register(
                nameof(TransitionDuration),
                typeof(double),
                typeof(DaisyTextRotate),
                new PropertyMetadata(500.0, OnTimingChanged));

        /// <summary>
        /// Gets or sets the transition duration for each item change in milliseconds.
        /// </summary>
        public double TransitionDuration
        {
            get => (double)GetValue(TransitionDurationProperty);
            set => SetValue(TransitionDurationProperty, value);
        }
        #endregion

        #region CurrentIndex
        public static readonly DependencyProperty CurrentIndexProperty =
            DependencyProperty.Register(
                nameof(CurrentIndex),
                typeof(int),
                typeof(DaisyTextRotate),
                new PropertyMetadata(0, OnCurrentIndexChanged));

        /// <summary>
        /// Gets or sets the index of the currently visible item.
        /// </summary>
        public int CurrentIndex
        {
            get => (int)GetValue(CurrentIndexProperty);
            set => SetValue(CurrentIndexProperty, value);
        }
        #endregion

        #region IsPaused
        public static readonly DependencyProperty IsPausedProperty =
            DependencyProperty.Register(
                nameof(IsPaused),
                typeof(bool),
                typeof(DaisyTextRotate),
                new PropertyMetadata(false, OnIsPausedChanged));

        /// <summary>
        /// Gets or sets whether the animation is paused.
        /// </summary>
        public bool IsPaused
        {
            get => (bool)GetValue(IsPausedProperty);
            set => SetValue(IsPausedProperty, value);
        }
        #endregion

        #region PauseOnHover
        public static readonly DependencyProperty PauseOnHoverProperty =
            DependencyProperty.Register(
                nameof(PauseOnHover),
                typeof(bool),
                typeof(DaisyTextRotate),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to pause the animation on hover.
        /// </summary>
        public bool PauseOnHover
        {
            get => (bool)GetValue(PauseOnHoverProperty);
            set => SetValue(PauseOnHoverProperty, value);
        }
        #endregion

        private static void OnTimingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTextRotate rotate)
                rotate.RestartLoop();
        }

        private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTextRotate rotate)
                rotate.RefreshPresenters();
        }

        private static void OnIsPausedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTextRotate rotate)
            {
                if ((bool)e.NewValue)
                    rotate.StopLoop();
                else if (!rotate._isPausedByHover)
                    rotate.StartLoop();
            }
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CurrentIndex = 0;
            RefreshPresenters();
            RestartLoop();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            RefreshPresenters();
            StartLoop();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopLoop();
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (ReferenceEquals(sender, _currentPresenter))
                _isPointerOverCurrent = true;
            else if (ReferenceEquals(sender, _nextPresenter))
                _isPointerOverNext = true;

            if (PauseOnHover)
            {
                _isPausedByHover = true;
                StopLoop();
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPausedByHover || IsPaused)
                return;

            if (ReferenceEquals(sender, _currentPresenter))
                _isPointerOverCurrent = false;
            else if (ReferenceEquals(sender, _nextPresenter))
                _isPointerOverNext = false;

            if (_isPointerOverCurrent || _isPointerOverNext)
                return;

            _isPausedByHover = false;
            StartLoop();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _currentTransform = new TranslateTransform();
            _currentPresenter = new ContentPresenter
            {
                // Use Left alignment to prevent horizontal jumping when text widths change.
                // The control itself can be centered by the parent if needed.
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = _currentTransform,
                Opacity = 1
            };

            _nextTransform = new TranslateTransform();
            _nextPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = _nextTransform,
                Opacity = 0
            };

            _currentPresenter.PointerEntered += OnPointerEntered;
            _currentPresenter.PointerExited += OnPointerExited;
            _nextPresenter.PointerEntered += OnPointerEntered;
            _nextPresenter.PointerExited += OnPointerExited;

            _rootGrid.Children.Add(_nextPresenter);
            _rootGrid.Children.Add(_currentPresenter);

            Content = _rootGrid;
        }

        private void RefreshPresenters()
        {
            if (_currentPresenter == null || _nextPresenter == null)
                return;

            // IMPORTANT: In Uno Platform, a UIElement can only have one parent.
            // We must clear both presenters before assigning to prevent ArgumentException.
            _nextPresenter.Content = null;
            _currentPresenter.Content = null;

            if (Items.Count == 0)
            {
                return;
            }

            var idx = ValueCoercion.Index(CurrentIndex, Items.Count);
            if (idx != CurrentIndex)
                CurrentIndex = idx;

            _currentPresenter.Content = Items[idx];
            _currentPresenter.Opacity = 1;
            _nextPresenter.Opacity = 0;
        }

        private void StartLoop()
        {
            StopLoop();

            if (IsPaused || Items.Count <= 1)
                return;

            _loopCts = new CancellationTokenSource();
            _ = RunLoopAsync(_loopCts.Token);
        }

        private void StopLoop()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
        }

        private void RestartLoop()
        {
            if (!IsLoaded)
                return;

            if (!IsPaused && !_isPausedByHover)
                StartLoop();
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var itemCount = Items.Count;
                if (itemCount <= 1)
                    return;

                var intervalPerItem = Duration / itemCount;
                var transitionMs = ValueCoercion.NonNegative(TransitionDuration);
                var waitTime = ValueCoercion.DurationMs(intervalPerItem - transitionMs, min: 100);

                try
                {
                    await Task.Delay((int)waitTime, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (ct.IsCancellationRequested)
                    return;

                await RunOnUIThreadAsync(() => AnimateNextAsync(ct), ct);
            }
        }

        private Task RunOnUIThreadAsync(Func<Task> action, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource();

            bool enqueued = DispatcherQueue != null && DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    if (!ct.IsCancellationRequested)
                        await action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            if (!enqueued)
            {
                // Fallback: best-effort synchronous run (should be rare)
                _ = action().ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        tcs.TrySetException(t.Exception);
                    else
                        tcs.TrySetResult();
                }, CancellationToken.None);
            }

            return tcs.Task;
        }

        private async Task AnimateNextAsync(CancellationToken ct)
        {
            if (_currentPresenter == null || _nextPresenter == null || _currentTransform == null || _nextTransform == null)
                return;

            var itemCount = Items.Count;
            if (itemCount <= 1)
                return;

            var nextIndex = (CurrentIndex + 1) % itemCount;

            // IMPORTANT: In Uno Platform, a UIElement can only have one parent.
            // Clear the next presenter before assigning new content.
            _nextPresenter.Content = null;
            _nextPresenter.Content = Items[nextIndex];

            // Prepare transforms
            var distance = ValueCoercion.PixelSize(ActualHeight > 0 ? ActualHeight / 4 : 24, min: 12, max: 32);
            _currentTransform.Y = 0;
            _nextTransform.Y = distance;
            _nextPresenter.Opacity = 0;

            var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            // Phase 1: Quick fade out of current item in place (200ms)
            var fadeOutDuration = new Duration(TimeSpan.FromMilliseconds(200));
            var fadeOutStoryboard = new Storyboard();

            var currentOpacity = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = fadeOutDuration,
                EasingFunction = easeIn,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(currentOpacity, _currentPresenter);
            Storyboard.SetTargetProperty(currentOpacity, "Opacity");
            fadeOutStoryboard.Children.Add(currentOpacity);

            var fadeOutTcs = new TaskCompletionSource<bool>();
            void OnFadeOutCompleted(object? s, object e)
            {
                fadeOutStoryboard.Completed -= OnFadeOutCompleted;
                fadeOutTcs.TrySetResult(true);
            }
            fadeOutStoryboard.Completed += OnFadeOutCompleted;
            fadeOutStoryboard.Begin();

            await fadeOutTcs.Task;
            fadeOutStoryboard.Stop();

            if (ct.IsCancellationRequested)
                return;

            // Set current to fully invisible
            _currentPresenter.Opacity = 0;
            _currentTransform.Y = 0;

            // Phase 2: Fade in new item with full transition duration
            var fadeInDuration = new Duration(TimeSpan.FromMilliseconds(ValueCoercion.DurationMs(TransitionDuration - 100, min: 80)));
            var fadeInStoryboard = new Storyboard();

            var nextOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = fadeInDuration,
                EasingFunction = easeOut,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(nextOpacity, _nextPresenter);
            Storyboard.SetTargetProperty(nextOpacity, "Opacity");
            fadeInStoryboard.Children.Add(nextOpacity);

            var nextY = new DoubleAnimation
            {
                From = distance,
                To = 0,
                Duration = fadeInDuration,
                EasingFunction = easeOut,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(nextY, _nextTransform);
            Storyboard.SetTargetProperty(nextY, "Y");
            fadeInStoryboard.Children.Add(nextY);

            var fadeInTcs = new TaskCompletionSource<bool>();
            void OnFadeInCompleted(object? s, object e)
            {
                fadeInStoryboard.Completed -= OnFadeInCompleted;
                fadeInTcs.TrySetResult(true);
            }
            fadeInStoryboard.Completed += OnFadeInCompleted;
            fadeInStoryboard.Begin();

            await fadeInTcs.Task;
            fadeInStoryboard.Stop();

            if (ct.IsCancellationRequested)
                return;

            // Explicitly set final values
            _nextPresenter.Opacity = 1;
            _nextTransform.Y = 0;

            // Update index
            CurrentIndex = nextIndex;

            // Swap references so _currentPresenter points to the visible one
            (_currentPresenter, _nextPresenter) = (_nextPresenter, _currentPresenter);
            (_currentTransform, _nextTransform) = (_nextTransform, _currentTransform);
        }
    }
}
