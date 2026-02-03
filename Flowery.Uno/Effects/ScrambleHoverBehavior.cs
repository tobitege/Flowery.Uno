using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Flowery.Effects
{
    /// <summary>
    /// Determines the scramble behavior direction.
    /// </summary>
    public enum ScrambleMode
    {
        /// <summary>
        /// Text starts scrambled and reveals on hover/click (default).
        /// </summary>
        RevealOnHover,

        /// <summary>
        /// Text starts readable and scrambles on hover/click.
        /// </summary>
        ScrambleOnHover
    }

    /// <summary>
    /// Determines how characters animate during reveal/scramble.
    /// </summary>
    public enum RevealStyle
    {
        /// <summary>
        /// Characters show random symbols until revealed (default).
        /// </summary>
        Random,

        /// <summary>
        /// Characters cycle through alphabet sequentially like split-flap displays.
        /// </summary>
        SplitFlap
    }

    /// <summary>
    /// Scrambles or reveals text characters on hover/click.
    /// Works on <see cref="TextBlock"/> via attached properties.
    /// </summary>
    public static class ScrambleHoverBehavior
    {
        private static readonly Random Random = new();
        private const string DefaultScrambleChars = "!@#$%^&*()[]{}|;:,.<>?/~`";
        private const string SplitFlapChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";

        private sealed class CharPositionBuffer(int[] positions)
        {
            public int[] Positions { get; } = positions;
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(ScrambleMode),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(ScrambleMode.RevealOnHover));

        public static readonly DependencyProperty ScrambleCharsProperty =
            DependencyProperty.RegisterAttached(
                "ScrambleChars",
                typeof(string),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(DefaultScrambleChars));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(TimeSpan),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(TimeSpan.FromMilliseconds(500)));

        public static readonly DependencyProperty FrameRateProperty =
            DependencyProperty.RegisterAttached(
                "FrameRate",
                typeof(int),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(30));

        public static readonly DependencyProperty RevealStyleProperty =
            DependencyProperty.RegisterAttached(
                "RevealStyle",
                typeof(RevealStyle),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(RevealStyle.Random));

        private static readonly DependencyProperty OriginalTextProperty =
            DependencyProperty.RegisterAttached(
                "OriginalText",
                typeof(string),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached(
                "Timer",
                typeof(DispatcherTimer),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty FrameCountProperty =
            DependencyProperty.RegisterAttached(
                "FrameCount",
                typeof(int),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(0));

        private static readonly DependencyProperty CharPositionsProperty =
            DependencyProperty.RegisterAttached(
                "CharPositions",
                typeof(CharPositionBuffer),
                typeof(ScrambleHoverBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static ScrambleMode GetMode(DependencyObject element) => (ScrambleMode)element.GetValue(ModeProperty);
        public static void SetMode(DependencyObject element, ScrambleMode value) => element.SetValue(ModeProperty, value);

        public static string GetScrambleChars(DependencyObject element) => (string)element.GetValue(ScrambleCharsProperty);
        public static void SetScrambleChars(DependencyObject element, string value) => element.SetValue(ScrambleCharsProperty, value);

        public static TimeSpan GetDuration(DependencyObject element) => (TimeSpan)element.GetValue(DurationProperty);
        public static void SetDuration(DependencyObject element, TimeSpan value) => element.SetValue(DurationProperty, value);

        public static int GetFrameRate(DependencyObject element) => (int)element.GetValue(FrameRateProperty);
        public static void SetFrameRate(DependencyObject element, int value) => element.SetValue(FrameRateProperty, value);

        public static RevealStyle GetRevealStyle(DependencyObject element) => (RevealStyle)element.GetValue(RevealStyleProperty);
        public static void SetRevealStyle(DependencyObject element, RevealStyle value) => element.SetValue(RevealStyleProperty, value);

        /// <summary>
        /// Programmatically triggers the animation (reveal or scramble based on mode).
        /// </summary>
        public static void TriggerScramble(TextBlock textBlock)
        {
            if (textBlock == null)
                return;

            StopAnimation(textBlock);
            StartAnimation(textBlock);
        }

        /// <summary>
        /// Stops any running animation and restores the original text.
        /// </summary>
        public static void ResetScramble(TextBlock textBlock)
        {
            if (textBlock == null)
                return;

            StopAnimation(textBlock);
            RestoreOriginalText(textBlock);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
                return;

            var enabled = e.NewValue is true;
            if (enabled)
            {
                textBlock.PointerEntered += OnPointerEntered;
                textBlock.PointerExited += OnPointerExited;
                textBlock.PointerPressed += OnPointerPressed;
                textBlock.Loaded += OnLoaded;
                textBlock.Unloaded += OnUnloaded;

                textBlock.DispatcherQueue?.TryEnqueue(() => InitializeText(textBlock));
            }
            else
            {
                textBlock.PointerEntered -= OnPointerEntered;
                textBlock.PointerExited -= OnPointerExited;
                textBlock.PointerPressed -= OnPointerPressed;
                textBlock.Loaded -= OnLoaded;
                textBlock.Unloaded -= OnUnloaded;
                StopAnimation(textBlock);
                RestoreOriginalText(textBlock);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                InitializeText(tb);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                StopAnimation(tb);
        }

        private static void InitializeText(TextBlock textBlock)
        {
            var currentText = textBlock.Text ?? string.Empty;
            if (string.IsNullOrEmpty(currentText))
                return;

            if (textBlock.GetValue(OriginalTextProperty) is not string storedOriginal || string.IsNullOrEmpty(storedOriginal))
            {
                textBlock.SetValue(OriginalTextProperty, currentText);
                storedOriginal = currentText;
            }

            if (GetMode(textBlock) == ScrambleMode.RevealOnHover)
            {
                textBlock.Text = ScrambleAllChars(storedOriginal, GetScrambleChars(textBlock), GetRevealStyle(textBlock));
            }
        }

        private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                StartAnimation(tb);
        }

        private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is TextBlock tb)
                StartAnimation(tb);
        }

        private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not TextBlock tb)
                return;

            var mode = GetMode(tb);
            StopAnimation(tb);

            if (mode == ScrambleMode.RevealOnHover)
            {
                var original = tb.GetValue(OriginalTextProperty) as string;
                if (!string.IsNullOrEmpty(original))
                    tb.Text = ScrambleAllChars(original, GetScrambleChars(tb), GetRevealStyle(tb));
            }
            else
            {
                RestoreOriginalText(tb);
            }
        }

        private static void StartAnimation(TextBlock textBlock)
        {
            if (textBlock.GetValue(TimerProperty) is DispatcherTimer)
                return;

            var original = textBlock.GetValue(OriginalTextProperty) as string;
            if (string.IsNullOrEmpty(original))
            {
                original = textBlock.Text ?? string.Empty;
                if (string.IsNullOrEmpty(original))
                    return;

                textBlock.SetValue(OriginalTextProperty, original);
            }

            textBlock.SetValue(FrameCountProperty, 0);

            var mode = GetMode(textBlock);
            var revealStyle = GetRevealStyle(textBlock);
            var duration = GetDuration(textBlock);
            var frameRate = Math.Max(1, GetFrameRate(textBlock));
            var scrambleChars = GetScrambleChars(textBlock);

            var totalFrames = (int)(duration.TotalMilliseconds / (1000.0 / frameRate));
            if (totalFrames < 1)
                totalFrames = 1;

            var interval = TimeSpan.FromMilliseconds(1000.0 / frameRate);

            if (revealStyle == RevealStyle.SplitFlap)
            {
                var positions = new int[original.Length];
                for (var i = 0; i < positions.Length; i++)
                    positions[i] = Random.Next(SplitFlapChars.Length);
                textBlock.SetValue(CharPositionsProperty, new CharPositionBuffer(positions));
            }

            var timer = new DispatcherTimer { Interval = interval };
            timer.Tick += (_, _) =>
            {
                var frameCount = (int)textBlock.GetValue(FrameCountProperty);
                var storedOriginal = textBlock.GetValue(OriginalTextProperty) as string ?? string.Empty;
                if (string.IsNullOrEmpty(storedOriginal))
                {
                    StopAnimation(textBlock);
                    return;
                }

                if (frameCount >= totalFrames)
                {
                    textBlock.Text = mode == ScrambleMode.RevealOnHover
                        ? storedOriginal
                        : ScrambleAllChars(storedOriginal, scrambleChars, revealStyle);
                    StopAnimation(textBlock);
                    return;
                }

                var progress = (double)frameCount / totalFrames;

                if (revealStyle == RevealStyle.SplitFlap)
                {
                    var positions = (textBlock.GetValue(CharPositionsProperty) as CharPositionBuffer)?.Positions;
                    var (text, allSettled) = BuildSplitFlapText(storedOriginal, positions, mode);
                    textBlock.Text = text;

                    if (allSettled)
                    {
                        textBlock.Text = storedOriginal;
                        StopAnimation(textBlock);
                        return;
                    }

                    if (positions != null)
                    {
                        for (int i = 0; i < storedOriginal.Length; i++)
                        {
                            if (char.IsWhiteSpace(storedOriginal[i])) continue;

                            var targetChar = char.ToUpperInvariant(storedOriginal[i]);
                            var targetIndex = SplitFlapChars.IndexOf(targetChar);
                            if (targetIndex < 0) targetIndex = SplitFlapChars.Length - 1;

                            if (positions[i] != targetIndex)
                                positions[i] = (positions[i] + 1) % SplitFlapChars.Length;
                        }
                    }
                }
                else
                {
                    textBlock.Text = BuildAnimatedText(storedOriginal, progress, mode, scrambleChars);
                }

                textBlock.SetValue(FrameCountProperty, frameCount + 1);
            };

            textBlock.SetValue(TimerProperty, timer);
            timer.Start();
        }

        private static string BuildAnimatedText(string original, double progress, ScrambleMode mode, string scrambleChars)
        {
            var resolvedCount = (int)(original.Length * progress);
            var chars = new char[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                bool isResolved = mode == ScrambleMode.RevealOnHover
                    ? i < resolvedCount
                    : i >= resolvedCount;

                if (isResolved)
                {
                    chars[i] = original[i];
                }
                else if (char.IsWhiteSpace(original[i]))
                {
                    chars[i] = original[i];
                }
                else
                {
                    chars[i] = scrambleChars[Random.Next(scrambleChars.Length)];
                }
            }

            return new string(chars);
        }

        private static (string text, bool allSettled) BuildSplitFlapText(string original, int[]? positions, ScrambleMode mode)
        {
            var chars = new char[original.Length];
            var isRevealing = mode == ScrambleMode.RevealOnHover;
            var allSettled = true;

            for (int i = 0; i < original.Length; i++)
            {
                if (char.IsWhiteSpace(original[i]))
                {
                    chars[i] = original[i];
                    continue;
                }

                var targetChar = char.ToUpperInvariant(original[i]);
                var targetIndex = SplitFlapChars.IndexOf(targetChar);
                if (targetIndex < 0) targetIndex = SplitFlapChars.Length - 1;

                var currentPos = positions != null && i < positions.Length ? positions[i] : 0;

                if (isRevealing)
                {
                    if (currentPos == targetIndex)
                    {
                        chars[i] = original[i];
                    }
                    else
                    {
                        chars[i] = SplitFlapChars[currentPos];
                        allSettled = false;
                    }
                }
                else
                {
                    chars[i] = SplitFlapChars[currentPos];
                    allSettled = false;
                }
            }

            return (new string(chars), allSettled);
        }

        private static string ScrambleAllChars(string original, string scrambleChars, RevealStyle style)
        {
            var chars = new char[original.Length];
            var charSet = style == RevealStyle.SplitFlap ? SplitFlapChars : scrambleChars;

            for (int i = 0; i < original.Length; i++)
            {
                if (char.IsWhiteSpace(original[i]))
                {
                    chars[i] = original[i];
                }
                else
                {
                    chars[i] = charSet[Random.Next(charSet.Length)];
                }
            }

            return new string(chars);
        }

        private static void StopAnimation(TextBlock textBlock)
        {
            if (textBlock.GetValue(TimerProperty) is DispatcherTimer timer)
            {
                timer.Stop();
                textBlock.ClearValue(TimerProperty);
            }
        }

        private static void RestoreOriginalText(TextBlock textBlock)
        {
            var original = textBlock.GetValue(OriginalTextProperty) as string;
            if (!string.IsNullOrEmpty(original))
                textBlock.Text = original;
        }
    }
}
