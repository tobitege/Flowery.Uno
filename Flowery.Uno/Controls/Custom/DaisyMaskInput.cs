using System;
using System.Collections.Generic;
using System.Globalization;
using Flowery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Controls
{
    /// <summary>
    /// Preset mask modes for <see cref="DaisyMaskInput"/>.
    /// </summary>
    public enum DaisyMaskInputMode
    {
        Custom,
        AlphaNumericCode,
        Timer,
        ExpiryDate,
        CreditCardNumber,
        Cvc
    }

    /// <summary>
    /// A masked input control styled like <see cref="DaisyInput"/>.
    /// Supports simple mask tokens: '0' (digit) and 'A' (letter). Any other character is treated as a literal.
    /// </summary>
    public partial class DaisyMaskInput : DaisyInput
    {
        private const string AlphaNumericCodeMask = "AA00 AAA";
        private const string TimerMask = "00:00:00";
        private const string ExpiryDateShortYearMask = "00/00";
        private const string ExpiryDateLongYearMask = "00/0000";
        private const string CreditCardNumberMask = "0000 0000 0000 0000";
        private const string CvcMask = "000";

        private bool _isAutoPlaceholder;
        private bool _isApplyingMode;
        private bool _isUpdatingText;
        private long _placeholderCallbackToken;

        public DaisyMaskInput()
        {
            TextChanged += OnTextChanged;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            AttachBeforeTextChangingHandler();
            FloweryLocalization.CultureChanged += OnCultureChanged;

            // Track user changes to PlaceholderText to disable auto placeholder.
            if (_placeholderCallbackToken == 0)
            {
                _placeholderCallbackToken = RegisterPropertyChangedCallback(PlaceholderTextProperty, (_, _) =>
                {
                    if (!_isApplyingMode)
                        _isAutoPlaceholder = false;
                });
            }

            ApplyMode(forcePlaceholderUpdate: _isAutoPlaceholder);
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            DetachBeforeTextChangingHandler();
            FloweryLocalization.CultureChanged -= OnCultureChanged;

            if (_placeholderCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(PlaceholderTextProperty, _placeholderCallbackToken);
                _placeholderCallbackToken = 0;
            }
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            if (Mode == DaisyMaskInputMode.ExpiryDate && _isAutoPlaceholder)
                ApplyMode(forcePlaceholderUpdate: true);
        }

        #region Mode
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(DaisyMaskInputMode),
                typeof(DaisyMaskInput),
                new PropertyMetadata(DaisyMaskInputMode.Custom, OnModeChanged));

        public DaisyMaskInputMode Mode
        {
            get => (DaisyMaskInputMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        #endregion

        #region ExpiryYearDigits
        public static readonly DependencyProperty ExpiryYearDigitsProperty =
            DependencyProperty.Register(
                nameof(ExpiryYearDigits),
                typeof(int),
                typeof(DaisyMaskInput),
                new PropertyMetadata(2, OnModeChanged));

        public int ExpiryYearDigits
        {
            get => (int)GetValue(ExpiryYearDigitsProperty);
            set => SetValue(ExpiryYearDigitsProperty, value);
        }
        #endregion

        #region Mask
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(
                nameof(Mask),
                typeof(string),
                typeof(DaisyMaskInput),
                new PropertyMetadata(string.Empty, OnMaskChanged));

        /// <summary>
        /// Gets or sets the input mask. Used when <see cref="Mode"/> is <see cref="DaisyMaskInputMode.Custom"/>.
        /// </summary>
        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }
        #endregion

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMaskInput input)
                input.ApplyMode(forcePlaceholderUpdate: input._isAutoPlaceholder);
        }

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMaskInput input)
                input.ReformatFromCurrentText();
        }

        private void ApplyMode(bool forcePlaceholderUpdate)
        {
            if (Mode == DaisyMaskInputMode.Custom)
            {
                ReformatFromCurrentText();
                return;
            }

            Mask = Mode switch
            {
                DaisyMaskInputMode.AlphaNumericCode => AlphaNumericCodeMask,
                DaisyMaskInputMode.Timer => TimerMask,
                DaisyMaskInputMode.ExpiryDate => ExpiryYearDigits >= 4 ? ExpiryDateLongYearMask : ExpiryDateShortYearMask,
                DaisyMaskInputMode.CreditCardNumber => CreditCardNumberMask,
                DaisyMaskInputMode.Cvc => CvcMask,
                _ => Mask
            };

            if (!forcePlaceholderUpdate && !string.IsNullOrEmpty(PlaceholderText))
            {
                ReformatFromCurrentText();
                return;
            }

            _isApplyingMode = true;
            try
            {
                PlaceholderText = Mode switch
                {
                    DaisyMaskInputMode.AlphaNumericCode => FloweryLocalization.GetStringInternal("MaskInput_Watermark_AlphaNumericCode", "AB12 CDE"),
                    DaisyMaskInputMode.Timer => FloweryLocalization.GetStringInternal("MaskInput_Watermark_Timer", "00:00:00"),
                    DaisyMaskInputMode.ExpiryDate => GetExpiryPlaceholder(ExpiryYearDigits),
                    DaisyMaskInputMode.CreditCardNumber => FloweryLocalization.GetStringInternal("MaskInput_Watermark_CreditCardNumber", "Card number"),
                    DaisyMaskInputMode.Cvc => FloweryLocalization.GetStringInternal("MaskInput_Watermark_Cvc", "CVC"),
                    _ => PlaceholderText
                };
                _isAutoPlaceholder = true;
            }
            finally
            {
                _isApplyingMode = false;
            }

            ReformatFromCurrentText();
        }

        private static string GetExpiryPlaceholder(int yearDigits)
        {
            if (yearDigits >= 4)
                return FloweryLocalization.GetStringInternal("MaskInput_Watermark_ExpiryLong", "MM/YYYY");

            return FloweryLocalization.GetStringInternal("MaskInput_Watermark_ExpiryShort", "MM/YY");
        }

        private void OnTextChanged(DaisyInput sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText)
                return;

            if (string.IsNullOrEmpty(Mask))
                return;

            ReformatFromCurrentText();
        }

        private void AttachBeforeTextChangingHandler()
        {
            if (InnerTextBox == null)
                return;

            InnerTextBox.BeforeTextChanging -= OnInnerBeforeTextChanging;
            InnerTextBox.BeforeTextChanging += OnInnerBeforeTextChanging;
        }

        private void DetachBeforeTextChangingHandler()
        {
            if (InnerTextBox == null)
                return;

            InnerTextBox.BeforeTextChanging -= OnInnerBeforeTextChanging;
        }

        private void OnInnerBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (_isUpdatingText)
                return;

            var mask = Mask ?? string.Empty;
            if (mask.Length == 0)
                return;

            var newText = args.NewText ?? string.Empty;
            if (newText.Length == 0)
                return;

            var raw = ExtractRawInput(newText);
            var (formatted, _) = ApplyMaskWithCaretMap(raw, mask);

            if (!IsSubsequenceNormalized(newText, formatted))
                args.Cancel = true;
        }

        private static bool IsSubsequenceNormalized(string text, string formatted)
        {
            if (text.Length == 0)
                return true;

            int fi = 0;
            for (int ti = 0; ti < text.Length; ti++)
            {
                char tc = text[ti];
                bool matched = false;
                while (fi < formatted.Length)
                {
                    if (CharsEquivalent(tc, formatted[fi]))
                    {
                        matched = true;
                        fi++;
                        break;
                    }
                    fi++;
                }

                if (!matched)
                    return false;
            }

            return true;
        }

        private static bool CharsEquivalent(char a, char b)
        {
            return char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
        }

        private void ReformatFromCurrentText()
        {
            if (_isUpdatingText)
                return;

            var mask = Mask ?? string.Empty;
            if (mask.Length == 0)
                return;

            var currentText = InnerTextBox?.Text ?? Text ?? string.Empty;
            var currentCaret = InnerTextBox?.SelectionStart ?? SelectionStart;

            // Map caret to raw index (count of input chars before caret).
            var rawIndex = CountInputCharsBeforeCaret(currentText, currentCaret);
            var raw = ExtractRawInput(currentText);

            var (formatted, caretMap) = ApplyMaskWithCaretMap(raw, mask);

            _isUpdatingText = true;
            try
            {
                if (!string.Equals(currentText, formatted, StringComparison.Ordinal))
                    Text = formatted;

                // Clamp rawIndex to map range.
                if (rawIndex < 0) rawIndex = 0;
                if (rawIndex >= caretMap.Count) rawIndex = caretMap.Count - 1;
                SelectionStart = caretMap[rawIndex];
                SelectionLength = 0;
            }
            finally
            {
                _isUpdatingText = false;
            }
        }

        private static int CountInputCharsBeforeCaret(string text, int caret)
        {
            if (caret <= 0) return 0;
            if (caret > text.Length) caret = text.Length;

            int count = 0;
            for (int i = 0; i < caret; i++)
            {
                if (IsPotentialInputChar(text[i]))
                    count++;
            }

            return count;
        }

        private static bool IsPotentialInputChar(char c)
        {
            // Treat alphanumerics as potential input; literals are typically punctuation/spaces.
            // This matches the limited token set we support.
            return char.IsLetterOrDigit(c);
        }

        private static string ExtractRawInput(string text)
        {
            var raw = new List<char>(text.Length);
            foreach (var c in text)
            {
                if (IsPotentialInputChar(c))
                    raw.Add(c);
            }
            return new string([.. raw]);
        }

        private static (string formatted, List<int> caretMap) ApplyMaskWithCaretMap(string raw, string mask)
        {
            int rawIndex = 0;
            var output = new List<char>(mask.Length);

            // caretMap[rawCount] = text index after placing rawCount chars
            var caretMap = new List<int> { 0 };

            bool hasPlacedAnyToken = false;

            for (int mi = 0; mi < mask.Length; mi++)
            {
                char m = mask[mi];
                bool isToken = m == '0' || m == 'A';

                if (!isToken)
                {
                    // Only insert literals when we have placed at least one token and there are more raw chars remaining.
                    if (hasPlacedAnyToken && rawIndex < raw.Length)
                        output.Add(m);
                    continue;
                }

                // Find next matching raw character for this token.
                while (rawIndex < raw.Length && !IsCharAllowedForToken(raw[rawIndex], m))
                    rawIndex++;

                if (rawIndex >= raw.Length)
                    break;

                output.Add(NormalizeTokenChar(raw[rawIndex], m));
                rawIndex++;
                hasPlacedAnyToken = true;

                // Record caret mapping for this rawCount.
                caretMap.Add(output.Count);
            }

            // Ensure caretMap has at least one entry for empty raw.
            if (caretMap.Count == 0)
                caretMap.Add(0);

            return (new string([.. output]), caretMap);
        }

        private static bool IsCharAllowedForToken(char c, char token)
        {
            return token switch
            {
                '0' => c >= '0' && c <= '9',
                'A' => char.IsLetter(c),
                _ => true
            };
        }

        private static char NormalizeTokenChar(char c, char token)
        {
            return token switch
            {
                'A' => char.ToUpperInvariant(c),
                _ => c
            };
        }
    }
}
