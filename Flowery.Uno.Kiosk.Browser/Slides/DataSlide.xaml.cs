using System;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Flowery.Uno.Kiosk.Browser.Slides
{
    public sealed partial class DataSlide : UserControl
    {
        private CancellationTokenSource? _updateCts;
        private Random _random = new Random();

        public DataSlide()
        {
            this.InitializeComponent();
            SessionFlow.Value = 1280m;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RunEntranceAnimation();
            StartDataSimulation();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateCts?.Cancel();
        }

        private void RunEntranceAnimation()
        {
            var sb = FloweryAnimationHelpers.CreateStoryboard();
            
            // Staggered entrance for columns
            PrepareForEntrance(HealthProgress.Parent as FrameworkElement, -40);
            PrepareForEntrance(ResourceCard, 40);

            AddPopIn(sb, HealthProgress.Parent as FrameworkElement, 0.0);
            AddPopIn(sb, ResourceCard, 0.2);

            sb.Begin();
        }

        private void PrepareForEntrance(FrameworkElement? element, double offset)
        {
            if (element == null) return;
            element.Opacity = 0;
            var transform = FloweryAnimationHelpers.EnsureCompositeTransform(element);
            transform.TranslateX = offset;
        }

        private void AddPopIn(Storyboard sb, FrameworkElement? element, double delay)
        {
            if (element == null) return;
            
            var move = new DoubleAnimation { To = 0, Duration = new Duration(TimeSpan.FromSeconds(1.2)), EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 6 }, BeginTime = TimeSpan.FromSeconds(delay) };
            Storyboard.SetTarget(move, element);
            Storyboard.SetTargetProperty(move, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");

            var fade = new DoubleAnimation { To = 1, Duration = new Duration(TimeSpan.FromSeconds(0.8)), BeginTime = TimeSpan.FromSeconds(delay) };
            Storyboard.SetTarget(fade, element);
            Storyboard.SetTargetProperty(fade, "Opacity");

            sb.Children.Add(move);
            sb.Children.Add(fade);
        }

        private async void StartDataSimulation()
        {
            _updateCts?.Cancel();
            _updateCts = new CancellationTokenSource();
            var ct = _updateCts.Token;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(2000, ct);
                    
                    // Simulate random resource fluctuations
                    var cpu = _random.Next(35, 85);
                    var ram = _random.Next(60, 92);
                    var io = _random.Next(10, 45);
                    var sessionsIncrease = _random.Next(1, 15);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        CpuProgress.Value = cpu;
                        CpuText.Text = $"{cpu}%";
                        
                        RamProgress.Value = ram;
                        RamText.Text = $"{ram}%";
                        
                        IoProgress.Value = io;
                        IoText.Text = $"{io}%";

                        SessionFlow.Value += sessionsIncrease;
                        
                        // Update Health based on load
                        var health = 100 - ((cpu + ram) / 10);
                        HealthProgress.Value = health;
                    });
                }
            }
            catch (TaskCanceledException) { }
        }
    }
}
