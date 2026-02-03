using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Effects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class EffectsExamples : ScrollableExamplePage
    {
        private CancellationTokenSource? _showcaseCts;
        private bool _showcaseRunning;
        private bool _mouseOverCursorPanel;

        public EffectsExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void ReplayTypewriter_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            TypewriterBehavior.Restart(TypewriterDemo);
        }

        private void ReplayReveal_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            RevealBehavior.TriggerReveal(RevealDemo);
            RevealBehavior.TriggerReveal(SlideInDemo);
            RevealBehavior.TriggerReveal(FadeOnlyDemo);
            RevealBehavior.TriggerReveal(ScaleDemo);
            RevealBehavior.TriggerReveal(ScaleSlideDemo);

            foreach (var child in RevealDirections.Children.OfType<Border>())
            {
                RevealBehavior.TriggerReveal(child);
            }
        }

        private async void StartShowcase_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            if (_showcaseRunning)
            {
                _showcaseCts?.Cancel();
                _showcaseRunning = false;

                StartShowcaseBtn.Content = "Start showcase";
                RevealShowcaseLabel.Text = "Reveal: FadeReveal (Bottom)";
                CursorFollowShowcaseLabel.Text = "Cursor Follow: Ring ∞";
                ScrambleShowcaseLabel.Text = "Scramble: (also hover)";

                CursorFollowBehavior.HideFollower(CursorFollowShowcasePanel);
                ScrambleHoverBehavior.ResetScramble(ScrambleShowcaseDemo);
                DetachCursorPanelHoverEvents();
                return;
            }

            _showcaseRunning = true;
            _showcaseCts = new CancellationTokenSource();
            StartShowcaseBtn.Content = "⏹ Stop";

            CursorFollowBehavior.ShowFollower(CursorFollowShowcasePanel);
            CursorFollowShowcaseLabel.Text = "Cursor Follow: ∞ Path";
            AttachCursorPanelHoverEvents();

            _ = AnimateInfinityPath(CursorFollowShowcasePanel, _showcaseCts.Token);

            var modes = new (RevealMode Mode, RevealDirection Dir, double Dist, string Name)[]
            {
                (RevealMode.FadeReveal, RevealDirection.Bottom, 40, "Reveal: FadeReveal (Bottom)"),
                (RevealMode.FadeReveal, RevealDirection.Left, 40, "Reveal: FadeReveal (Left)"),
                (RevealMode.SlideIn, RevealDirection.Right, 80, "Reveal: SlideIn (Right)"),
                (RevealMode.SlideIn, RevealDirection.Top, 60, "Reveal: SlideIn (Top)"),
                (RevealMode.FadeOnly, RevealDirection.Bottom, 0, "Reveal: FadeOnly"),
                (RevealMode.Scale, RevealDirection.Bottom, 0, "Reveal: Scale"),
                (RevealMode.ScaleSlide, RevealDirection.Bottom, 50, "Reveal: ScaleSlide"),
            };

            try
            {
                while (!_showcaseCts.Token.IsCancellationRequested)
                {
                    foreach (var (mode, dir, dist, name) in modes)
                    {
                        if (_showcaseCts.Token.IsCancellationRequested)
                            break;

                        RevealShowcaseLabel.Text = name;

                        RevealBehavior.SetMode(RevealShowcaseDemo, mode);
                        RevealBehavior.SetDirection(RevealShowcaseDemo, dir);
                        RevealBehavior.SetDistance(RevealShowcaseDemo, dist);
                        RevealBehavior.SetDuration(RevealShowcaseDemo, TimeSpan.FromMilliseconds(600));
                        RevealBehavior.TriggerReveal(RevealShowcaseDemo);

                        ScrambleShowcaseLabel.Text = "Scramble: Running...";
                        ScrambleHoverBehavior.TriggerScramble(ScrambleShowcaseDemo);

                        await Task.Delay(1200, _showcaseCts.Token);
                        ScrambleShowcaseLabel.Text = "Scramble: (also hover)";
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }

            _showcaseRunning = false;
            StartShowcaseBtn.Content = "Start showcase";
            RevealShowcaseLabel.Text = "Reveal: FadeReveal (Bottom)";
            CursorFollowShowcaseLabel.Text = "Cursor Follow: Ring ∞";
            ScrambleShowcaseLabel.Text = "Scramble: (also hover)";

            CursorFollowBehavior.HideFollower(CursorFollowShowcasePanel);
            DetachCursorPanelHoverEvents();
        }

        private void AttachCursorPanelHoverEvents()
        {
            CursorFollowShowcasePanel.PointerEntered += CursorFollowShowcasePanel_PointerEntered;
            CursorFollowShowcasePanel.PointerExited += CursorFollowShowcasePanel_PointerExited;
        }

        private void DetachCursorPanelHoverEvents()
        {
            CursorFollowShowcasePanel.PointerEntered -= CursorFollowShowcasePanel_PointerEntered;
            CursorFollowShowcasePanel.PointerExited -= CursorFollowShowcasePanel_PointerExited;
            _mouseOverCursorPanel = false;
        }

        private void CursorFollowShowcasePanel_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _mouseOverCursorPanel = true;
        }

        private void CursorFollowShowcasePanel_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _mouseOverCursorPanel = false;
        }

        /// <summary>
        /// Animates the cursor follower along an infinity (lemniscate) path.
        /// </summary>
        private async Task AnimateInfinityPath(Panel panel, CancellationToken cancellationToken)
        {
            const double speed = 0.03; // radians per frame
            double t = 0;
            int shapeIndex = 0;
            var shapes = new[] { FollowerShape.Circle, FollowerShape.Square, FollowerShape.Ring };

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!_mouseOverCursorPanel)
                    {
                        var width = panel.ActualWidth;
                        var height = panel.ActualHeight;

                        if (width > 0 && height > 0)
                        {
                            var sinT = Math.Sin(t);
                            var cosT = Math.Cos(t);
                            var denom = 1 + sinT * sinT;

                            var scaleX = (width - 20) / 2.5;
                            var scaleY = (height - 16) / 1.5;

                            var x = (cosT / denom) * scaleX + width / 2;
                            var y = (sinT * cosT / denom) * scaleY + height / 2;

                            CursorFollowBehavior.SetTargetPosition(panel, x, y);
                        }

                        t += speed;
                        if (t > Math.PI * 2)
                        {
                            t -= Math.PI * 2;
                            shapeIndex = (shapeIndex + 1) % shapes.Length;
                            CursorFollowBehavior.SetFollowerShape(panel, shapes[shapeIndex]);
                            CursorFollowShowcaseLabel.Text = $"Cursor Follow: {shapes[shapeIndex]} ∞";
                        }
                    }

                    await Task.Delay(16, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
