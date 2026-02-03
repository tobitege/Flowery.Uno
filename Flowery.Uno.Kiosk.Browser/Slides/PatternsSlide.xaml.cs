using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Uno.Kiosk.Browser.Slides
{
    public sealed partial class PatternsSlide : UserControl
    {
        private DispatcherTimer? _animTimer;
        private int _animTick;
        
        // Cards and transforms for animation
        private DaisyPatternedCard? _row0Left, _row0Right;
        private DaisyPatternedCard? _row1Left, _row1Right;
        private DaisyPatternedCard? _row2Left, _row2Right;
        private TranslateTransform? _row0LeftTransform, _row0RightTransform;
        private TranslateTransform? _row1LeftTransform, _row1RightTransform;
        private TranslateTransform? _row2LeftTransform, _row2RightTransform;
        
        // Track which rows have been created
        private bool _row0Created, _row1Created, _row2Created;
        
        public PatternsSlide()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _row0Created = _row1Created = _row2Created = false;
            _animTick = 0;
            
            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _animTimer?.Stop();
            _animTimer = null;
        }

        private void OnAnimTick(object? sender, object e)
        {
            _animTick++;
            
            // Timing constants
            const int fadeInDuration = 10; // ~160ms fade in
            const int moveDuration = 25;   // ~400ms movement
            const int rowDelay = 35;       // ~560ms between row starts
            const int animDone = fadeInDuration + moveDuration;
            
            // Row 0: create immediately (fast with SvgAsset mode)
            if (!_row0Created)
            {
                CreateRow0();
                _row0Created = true;
            }
            AnimateRowWithFade(_row0Left, _row0Right, _row0LeftTransform, _row0RightTransform, 
                               _animTick, fadeInDuration, moveDuration);
            
            // Row 1: create early
            if (!_row1Created && _animTick >= 10)
            {
                CreateRow1();
                _row1Created = true;
            }
            if (_row1Created)
                AnimateRowWithFade(_row1Left, _row1Right, _row1LeftTransform, _row1RightTransform, 
                                   _animTick - rowDelay, fadeInDuration, moveDuration);
            
            // Row 2: create when row 1 starts
            if (!_row2Created && _animTick >= rowDelay + 10)
            {
                CreateRow2();
                _row2Created = true;
            }
            if (_row2Created)
                AnimateRowWithFade(_row2Left, _row2Right, _row2LeftTransform, _row2RightTransform, 
                                   _animTick - rowDelay * 2, fadeInDuration, moveDuration);
            
            // Stop when all done
            if (_animTick >= rowDelay * 2 + animDone)
            {
                _animTimer?.Stop();
            }
        }

        private void CreateRow0()
        {
            (_row0Left, _row0Right, _row0LeftTransform, _row0RightTransform) = CreateCardPair(
                Row0Left, Row0Right,
                DaisyCardPattern.CarbonFiber, DaisyCardPattern.Honeycomb,
                DaisyColor.Neutral, DaisyColor.Primary,
                "Carbon Fiber", "Honeycomb");
        }

        private void CreateRow1()
        {
            (_row1Left, _row1Right, _row1LeftTransform, _row1RightTransform) = CreateCardPair(
                Row1Left, Row1Right,
                DaisyCardPattern.Circuit, DaisyCardPattern.DiamondPlate,
                DaisyColor.Secondary, DaisyColor.Neutral,
                "Circuit Board", "Diamond Plate");
        }

        private void CreateRow2()
        {
            (_row2Left, _row2Right, _row2LeftTransform, _row2RightTransform) = CreateCardPair(
                Row2Left, Row2Right,
                DaisyCardPattern.Dots, DaisyCardPattern.Stripes,
                DaisyColor.Accent, DaisyColor.Default,
                "Subtle Dots", "Dynamic Stripes");
        }

        private (DaisyPatternedCard left, DaisyPatternedCard right, TranslateTransform leftT, TranslateTransform rightT) CreateCardPair(
            Border leftContainer, Border rightContainer,
            DaisyCardPattern leftPattern, DaisyCardPattern rightPattern,
            DaisyColor leftColor, DaisyColor rightColor,
            string leftTitle, string rightTitle)
        {
            var leftTransform = new TranslateTransform { X = -400 };
            var rightTransform = new TranslateTransform { X = 400 };
            
            var leftCard = new DaisyPatternedCard
            {
                Pattern = leftPattern,
                ColorVariant = leftColor,
                Width = 200,
                Height = 100,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                RenderTransform = leftTransform,
                Opacity = 0, // Start invisible
                Content = new TextBlock
                {
                    Text = leftTitle,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            
            var rightCard = new DaisyPatternedCard
            {
                Pattern = rightPattern,
                ColorVariant = rightColor,
                Width = 200,
                Height = 100,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                RenderTransform = rightTransform,
                Opacity = 0, // Start invisible
                Content = new TextBlock
                {
                    Text = rightTitle,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            
            // Add to tree - fast, no pattern generation yet
            leftContainer.Child = leftCard;
            rightContainer.Child = rightCard;
            
            // Don't enable patterns yet - done in animation loop after cards are visible

            return (leftCard, rightCard, leftTransform, rightTransform);
        }

        private static void AnimateRowWithFade(
            DaisyPatternedCard? leftCard, DaisyPatternedCard? rightCard,
            TranslateTransform? leftT, TranslateTransform? rightT, 
            int tick, int fadeDuration, int moveDuration)
        {
            if (leftCard == null || rightCard == null || leftT == null || rightT == null) 
                return;
            
            if (tick < 0)
            {
                // Not started - stay hidden and off-screen
                leftCard.Opacity = 0;
                rightCard.Opacity = 0;
                leftT.X = -400;
                rightT.X = 400;
                return;
            }
            
            // Phase 1: Fade in (still off-screen)
            if (tick < fadeDuration)
            {
                double fadeT = tick / (double)fadeDuration;
                leftCard.Opacity = fadeT;
                rightCard.Opacity = fadeT;
                leftT.X = -400;
                rightT.X = 400;
                return;
            }
            
            // Ensure fully visible
            leftCard.Opacity = 1;
            rightCard.Opacity = 1;
            
            // Phase 2: Move in
            int moveTick = tick - fadeDuration;
            if (moveTick >= moveDuration)
            {
                leftT.X = 0;
                rightT.X = 0;
                return;
            }
            
            // Ease-out cubic
            double t = moveTick / (double)moveDuration;
            double eased = 1 - Math.Pow(1 - t, 3);
            
            leftT.X = -400 * (1 - eased);
            rightT.X = 400 * (1 - eased);
        }
    }
}
