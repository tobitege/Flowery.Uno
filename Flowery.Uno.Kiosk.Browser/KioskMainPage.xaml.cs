using System.Collections.ObjectModel;
using Flowery.Controls;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace Flowery.Uno.Kiosk.Browser
{
    public sealed partial class KioskMainPage : Page
    {
        private readonly FlowerySlideshowController _controller;
        private readonly ObservableCollection<KioskSlideInfo> _slides = [];
        private int _currentIndex = -1;
        private bool _isUsingA = false;
        private DispatcherTimer? _overlayTimer;
        private DispatcherTimer? _slideTimer;

        public KioskMainPage()
        {
            this.InitializeComponent();
            _controller = new FlowerySlideshowController(
                OnNavigate,
                () => _slides.Count,
                () => _currentIndex
            )
            {
                Interval = 8.0, // 8 seconds per slide
                Mode = FlowerySlideshowMode.Slideshow,
                WrapAround = true
            };

            // Add keyboard navigation for debugging
            this.KeyDown += OnKeyDown;

            // Use dispatcher to initialize after the visual tree is set up
            // The Loaded event is unreliable in WASM environments
            DispatcherQueue.TryEnqueue(InitializeAndStart);
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Right:
                case VirtualKey.Space:
                case VirtualKey.N:
                    NextSlide();
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                case VirtualKey.P:
                    PreviousSlide();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Advances to the next slide. Can be called from browser console for debugging.
        /// </summary>
        public void NextSlide()
        {
            var nextIndex = (_currentIndex + 1) % _slides.Count;
            OnNavigate(nextIndex);
        }

        /// <summary>
        /// Goes to the previous slide. Can be called from browser console for debugging.
        /// </summary>
        public void PreviousSlide()
        {
            var prevIndex = (_currentIndex - 1 + _slides.Count) % _slides.Count;
            OnNavigate(prevIndex);
        }

        private void InitializeAndStart()
        {
            InitializeSlides();
            
            // Show first slide immediately
            if (_slides.Count > 0)
            {
                OnNavigate(0);
            }

            // Check for debug mode via URL
            // Navigate to http://127.0.0.1:5235/?debug or http://127.0.0.1:5235/#debug
            // to disable auto-timer and use keyboard navigation only
            var isDebugMode = CheckDebugMode();
            
            if (!isDebugMode)
            {
                _controller.Start();
            }
            // In debug mode, auto-timer is disabled - use keyboard navigation (Right/Space/N)
            
            // Ensure focus for keyboard navigation
            this.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Checks if debug mode is enabled via URL parameter or hash.
        /// </summary>
        private static bool CheckDebugMode()
        {
#if HAS_UNO_WASM
            try
            {
                var url = global::Uno.Foundation.WebAssemblyRuntime.InvokeJS("window.location.href");
                return url?.Contains("debug", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private void InitializeSlides()
        {
            _slides.Add(new KioskSlideInfo
            {
                Title = "Authentication",
                Keywords = "Login • Forms • Validation",
                ContentFactory = () => new Slides.LoginSlide(),
                Theme = "Halloween"
            });

            _slides.Add(new KioskSlideInfo
            {
                Title = "Data Visualization",
                Keywords = "Stats • Progress • Indicators",
                ContentFactory = () => new Slides.DataSlide(),
                Theme = "Synthwave"
            });
            
            _slides.Add(new KioskSlideInfo
            {
                Title = "Communication",
                Keywords = "Chat • Badges • Avatars",
                ContentFactory = () => new Slides.FeedbackSlide(),
                Theme = "Nord"
            });

            _slides.Add(new KioskSlideInfo
            {
                Title = "Enhanced Controls",
                Keywords = "Weather • Color Picker • Data",
                ContentFactory = () => new Slides.ShowcaseSlide(),
                Theme = "Dark"
            });
            
            _slides.Add(new KioskSlideInfo
            {
                Title = "Runtime Scaling",
                Keywords = "Zoom • Scale • Responsive",
                ContentFactory = () => new Slides.ScalingSlide(),
                Theme = "Dracula"
            });
            
            _slides.Add(new KioskSlideInfo
            {
                Title = "Background Patterns",
                Keywords = "Textures • Cards • Surfaces",
                ContentFactory = () => new Slides.PatternsSlide(),
                Theme = "CoffeeLight"
            });
        }

        private void OnNavigate(int index)
        {
            try
            {
                if (index == _currentIndex) return;

                // Stop timer during transition - will restart after fade-in completes
                _controller.Stop();

                var oldIndex = _currentIndex;
                _currentIndex = index;
                var slide = _slides[index];
                
                // Get containers
                var oldContainer = _isUsingA ? SlideContentA : SlideContentB;
                var newContainer = _isUsingA ? SlideContentB : SlideContentA;
                var oldPresenter = _isUsingA ? SlidePresenterA : SlidePresenterB;
                var newPresenter = _isUsingA ? SlidePresenterB : SlidePresenterA;
                _isUsingA = !_isUsingA;

                // For first slide, just show immediately
                if (oldIndex == -1)
                {
                    // Apply theme before showing first slide
                    if (!string.IsNullOrEmpty(slide.Theme))
                    {
                        DaisyThemeManager.ApplyTheme(slide.Theme);
                    }
                    
                    newPresenter.Content = slide.ContentFactory();
                    newContainer.Visibility = Visibility.Visible;
                    newContainer.Opacity = 1;
                    oldContainer.Visibility = Visibility.Collapsed;
                    
                    SlideTitle.Text = slide.Title.ToUpper();
                    KeywordsView.Text = slide.Keywords;
                    AnimateProgress();
                    this.Focus(FocusState.Programmatic);
                    return;
                }

                // For subsequent slides: fade out -> switch theme -> fade in
                // Use DispatcherTimer for reliable WASM opacity animation
                // Stop any existing slide animation first and reset to known state
                _slideTimer?.Stop();
                oldContainer.Opacity = 1; // Ensure we start from known state
                
                const int fadeOutMs = 250;
                const int fadeInMs = 400;
                const int stepMs = 20;
                
                double fadeOutSteps = fadeOutMs / (double)stepMs;
                double fadeInSteps = fadeInMs / (double)stepMs;
                int stepCount = 0;
                bool fadingOut = true;
                
                var slideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(stepMs) };
                _slideTimer = slideTimer; // Store reference
                
                slideTimer.Tick += (s, e) =>
                {
                    // If we've been superseded by a new timer, stop ourselves
                    if (_slideTimer != slideTimer)
                    {
                        slideTimer.Stop();
                        return;
                    }
                    
                    stepCount++;
                    
                    if (fadingOut)
                    {
                        // Fade out phase
                        double opacity = Math.Max(0, 1 - (stepCount / fadeOutSteps));
                        oldContainer.Opacity = opacity;
                        
                        if (opacity <= 0)
                        {
                            // Hide old container, apply theme during blackout
                            oldContainer.Visibility = Visibility.Collapsed;
                            oldContainer.Opacity = 1; // Reset for next use
                            oldPresenter.Content = null; // Clean up
                            
                            if (!string.IsNullOrEmpty(slide.Theme))
                            {
                                DaisyThemeManager.ApplyTheme(slide.Theme);
                            }
                            
                            // Prepare new slide (after theme is applied)
                            newPresenter.Content = slide.ContentFactory();
                            newContainer.Opacity = 0;
                            newContainer.Visibility = Visibility.Visible;
                            
                            // Switch to fade in
                            fadingOut = false;
                            stepCount = 0;
                        }
                    }
                    else
                    {
                        // Fade in phase
                        double opacity = Math.Min(1, stepCount / fadeInSteps);
                        newContainer.Opacity = opacity;
                        
                        if (opacity >= 1)
                        {
                            newContainer.Opacity = 1; // Ensure exactly 1
                            slideTimer.Stop();
                            _controller.Restart(); // Now start counting the slide's display time
                        }
                    }
                };
                
                slideTimer.Start();

                // Animate progress and overlay (these can happen independently)
                AnimateProgress();
                AnimateOverlay(slide.Title.ToUpper(), slide.Keywords);
                this.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kiosk Navigation error: {ex}");
            }
        }


        private void AnimateOverlay(string newTitle, string newKeywords)
        {
            // Stop any existing overlay animation and reset to known state
            _overlayTimer?.Stop();
            OverlayLayer.Opacity = 1; // Start from known state
            
            const int fadeOutMs = 200;
            const int fadeInMs = 400;
            const int stepMs = 20;
            
            double fadeOutSteps = fadeOutMs / (double)stepMs;
            double fadeInSteps = fadeInMs / (double)stepMs;
            bool fadingOut = true;
            int stepCount = 0;
            
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(stepMs) };
            _overlayTimer = timer; // Store reference
            
            timer.Tick += (s, e) =>
            {
                // If we've been superseded by a new timer, stop ourselves
                if (_overlayTimer != timer)
                {
                    timer.Stop();
                    return;
                }
                
                stepCount++;
                
                if (fadingOut)
                {
                    // Fade out phase
                    double opacity = Math.Max(0, 1 - (stepCount / fadeOutSteps));
                    OverlayLayer.Opacity = opacity;
                    
                    if (opacity <= 0)
                    {
                        // Update text and prepare for fade in
                        SlideTitle.Text = newTitle;
                        KeywordsView.Text = newKeywords;
                        stepCount = 0;
                        fadingOut = false;
                    }
                }
                else
                {
                    // Fade in phase
                    double opacity = Math.Min(1, stepCount / fadeInSteps);
                    OverlayLayer.Opacity = opacity;
                    
                    if (opacity >= 1)
                    {
                        OverlayLayer.Opacity = 1; // Ensure exactly 1
                        timer.Stop();
                    }
                }
            };
            
            timer.Start();
        }

        private void AnimateProgress()
        {
            SlideProgress.Value = 0;
            var sb = FloweryAnimationHelpers.CreateStoryboard();
            var anim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(_controller.Interval))
            };
            Storyboard.SetTarget(anim, SlideProgress);
            Storyboard.SetTargetProperty(anim, "Value");
            sb.Children.Add(anim);
            sb.Begin();
        }
    }

    public class KioskSlideInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public Func<FrameworkElement> ContentFactory { get; set; } = () => new Grid();
        public string Theme { get; set; } = string.Empty;
    }
}
