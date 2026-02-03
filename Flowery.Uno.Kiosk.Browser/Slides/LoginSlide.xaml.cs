using System;
using Flowery.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Flowery.Uno.Kiosk.Browser.Slides
{
    public sealed partial class LoginSlide : UserControl
    {
        private DispatcherTimer? _featureTimer;
        private int _currentFeatureIndex = 0;
        private TextBlock[] _features = null!;

        public LoginSlide()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize features array
            _features = [Feature1, Feature2, Feature3, Feature4, Feature5, Feature6, Feature7];
            
            // Hide features initially
            foreach (var feature in _features)
                feature.Opacity = 0;

            // Start stacked feature reveal
            _currentFeatureIndex = 0;
            _featureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _featureTimer.Tick += OnFeatureTimerTick;
            _featureTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _featureTimer?.Stop();
            _featureTimer = null;
        }

        private void OnFeatureTimerTick(object? sender, object e)
        {
            if (_currentFeatureIndex < _features.Length)
            {
                FadeIn(_features[_currentFeatureIndex]);
                if (_currentFeatureIndex == _features.Length - 1)
                    _featureTimer?.Stop();
            }
            _currentFeatureIndex++;
        }

        private void FadeIn(FrameworkElement element)
        {
            var sb = FloweryAnimationHelpers.CreateStoryboard();
            var fade = new DoubleAnimation 
            { 
                From = 0,
                To = 1, 
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            Storyboard.SetTarget(fade, element);
            Storyboard.SetTargetProperty(fade, "Opacity");
            sb.Children.Add(fade);
            sb.Begin();
        }
    }
}
