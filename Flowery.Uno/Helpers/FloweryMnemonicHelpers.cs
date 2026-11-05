using System;
using System.Text;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;

namespace Flowery.Helpers
{
    /// <summary>
    /// Parsed WinForms-style mnemonic label information.
    /// </summary>
    public readonly struct FloweryMnemonicInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloweryMnemonicInfo"/> struct.
        /// </summary>
        /// <param name="displayText">Text with mnemonic markers removed.</param>
        /// <param name="mnemonicIndex">Index of the mnemonic character in <paramref name="displayText"/>, or -1.</param>
        /// <param name="mnemonicChar">Mnemonic character, or null when none.</param>
        public FloweryMnemonicInfo(string displayText, int mnemonicIndex, char? mnemonicChar)
        {
            DisplayText = displayText;
            MnemonicIndex = mnemonicIndex;
            MnemonicChar = mnemonicChar;
        }

        /// <summary>
        /// Gets the display text with <c>&amp;</c> markers removed (<c>&amp;&amp;</c> becomes a literal <c>&amp;</c>).
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// Gets the zero-based index of the mnemonic character in <see cref="DisplayText"/>, or -1 when none.
        /// </summary>
        public int MnemonicIndex { get; }

        /// <summary>
        /// Gets the mnemonic character, or null when the label has no mnemonic.
        /// </summary>
        public char? MnemonicChar { get; }
    }

    /// <summary>
    /// WinForms-style mnemonic parsing helpers (<c>&amp;</c> marks the access character, <c>&amp;&amp;</c> is a literal <c>&amp;</c>,
    /// and an <c>&amp;</c> before whitespace stays literal, so "Crop &amp; rotate" needs no escaping).
    /// Display-only: applications must implement their own Alt+key handling.
    /// </summary>
    public static class FloweryMnemonicHelpers
    {
        private const string StringContentTemplateKey = "DaisyMnemonicStringTemplate";
        private const string StringContentTemplateSource = "ms-appx:///Flowery.Uno/Themes/DaisyButtons.xaml";

        private static DataTemplate? s_stringContentTemplate;
        private static bool s_stringContentTemplateResolved;

        /// <summary>
        /// Gets the data template that renders string content as a <see cref="Controls.DaisyMnemonicText"/>
        /// (resource key <c>DaisyMnemonicStringTemplate</c>), or null when the Flowery theme resources are unavailable.
        /// Assign it as <c>ContentTemplate</c> of a content control whose string content contains <c>&amp;</c> markers.
        /// </summary>
        public static DataTemplate? StringContentTemplate
        {
            get
            {
                if (!s_stringContentTemplateResolved)
                {
                    s_stringContentTemplate = ResolveStringContentTemplate();
                    s_stringContentTemplateResolved = s_stringContentTemplate != null;
                }

                return s_stringContentTemplate;
            }
        }

        /// <summary>
        /// Parses a WinForms-style mnemonic label.
        /// </summary>
        /// <param name="text">Source text that may contain <c>&amp;</c> markers.</param>
        /// <returns>Display text and optional mnemonic metadata.</returns>
        public static FloweryMnemonicInfo Parse(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new FloweryMnemonicInfo(string.Empty, -1, null);
            }

            var builder = new StringBuilder(text.Length);
            var mnemonicIndex = -1;
            char? mnemonicChar = null;

            for (var i = 0; i < text.Length; i++)
            {
                var current = text[i];
                if (current != '&')
                {
                    builder.Append(current);
                    continue;
                }

                if (i + 1 >= text.Length)
                {
                    // Trailing '&' is ignored (WinForms behavior).
                    break;
                }

                var next = text[i + 1];
                if (next == '&')
                {
                    builder.Append('&');
                    i++;
                    continue;
                }

                if (char.IsWhiteSpace(next))
                {
                    // "Crop & rotate": an '&' before whitespace cannot mark an access key, so it stays literal.
                    builder.Append('&');
                    continue;
                }

                if (mnemonicIndex < 0)
                {
                    mnemonicIndex = builder.Length;
                    mnemonicChar = next;
                }

                builder.Append(next);
                i++;
            }

            return new FloweryMnemonicInfo(builder.ToString(), mnemonicIndex, mnemonicChar);
        }

        /// <summary>
        /// Gets the display text with mnemonic markers removed.
        /// </summary>
        /// <param name="text">Source text that may contain <c>&amp;</c> markers.</param>
        /// <returns>Display text.</returns>
        public static string GetDisplayText(string? text) => Parse(text).DisplayText;

        /// <summary>
        /// Gets the mnemonic character from a WinForms-style label, if present.
        /// </summary>
        /// <param name="text">Source text that may contain <c>&amp;</c> markers.</param>
        /// <returns>Mnemonic character, or null.</returns>
        public static char? GetMnemonicChar(string? text) => Parse(text).MnemonicChar;

        /// <summary>
        /// Shows a WinForms-style mnemonic label in a <see cref="TextBlock"/>. Without a mnemonic to underline,
        /// the display text goes into <see cref="TextBlock.Text"/>; otherwise the text is split into runs and the
        /// access character is underlined. Font and foreground stay inherited from the <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="textBlock">Text block that shows the label.</param>
        /// <param name="text">Source text that may contain <c>&amp;</c> markers.</param>
        /// <param name="showMnemonic">Whether the access character is underlined.</param>
        /// <returns>The parsed label.</returns>
        public static FloweryMnemonicInfo ApplyMnemonicText(TextBlock textBlock, string? text, bool showMnemonic = true)
        {
            ArgumentNullException.ThrowIfNull(textBlock);

            var info = Parse(text);
            var display = info.DisplayText;

            if (!showMnemonic || info.MnemonicIndex < 0 || info.MnemonicIndex >= display.Length)
            {
                textBlock.Text = display;

                // Uno keeps Text in sync with the runs, so assigning the same display text again
                // leaves an earlier underlined run in place.
                if (textBlock.Inlines.OfType<Run>().Any(run => run.TextDecorations != TextDecorations.None))
                {
                    textBlock.Inlines.Clear();
                    textBlock.Inlines.Add(new Run { Text = display });
                }

                if (PlatformCompatibility.IsSkiaBackend)
                {
                    FloweryMnemonicUnderline.Update(textBlock, display, -1);
                }

                return info;
            }

            textBlock.Inlines.Clear();

            if (info.MnemonicIndex > 0)
            {
                textBlock.Inlines.Add(new Run { Text = display[..info.MnemonicIndex] });
            }

            textBlock.Inlines.Add(new Run
            {
                Text = display.Substring(info.MnemonicIndex, 1),
                TextDecorations = TextDecorations.Underline
            });

            if (info.MnemonicIndex + 1 < display.Length)
            {
                textBlock.Inlines.Add(new Run { Text = display[(info.MnemonicIndex + 1)..] });
            }

            // Uno's Skia text renderer ignores TextDecorations; draw the line separately there.
            if (PlatformCompatibility.IsSkiaBackend)
            {
                FloweryMnemonicUnderline.Update(textBlock, display, info.MnemonicIndex);
            }

            return info;
        }

        /// <summary>
        /// Returns true for string content that contains <c>&amp;</c> markers.
        /// </summary>
        internal static bool IsMnemonicLabel(object? content) =>
            content is string text && text.Contains('&', StringComparison.Ordinal);

        /// <summary>
        /// Returns true when <paramref name="template"/> is the shared <see cref="StringContentTemplate"/>.
        /// </summary>
        internal static bool IsStringContentTemplate(DataTemplate? template) =>
            template != null && ReferenceEquals(template, s_stringContentTemplate);

        /// <summary>
        /// Shows content in a code-built presenter. Strings with <c>&amp;</c> markers use <see cref="StringContentTemplate"/>;
        /// other content gets the default presentation. WinUI did not apply a <c>ContentTemplateSelector</c> to string
        /// content in these presenters, so the template is set directly.
        /// </summary>
        internal static void SetPresenterContent(ContentPresenter presenter, object? content)
        {
            if (IsMnemonicLabel(content) && StringContentTemplate is { } template)
            {
                presenter.ContentTemplate = template;
            }
            else if (IsStringContentTemplate(presenter.ContentTemplate))
            {
                presenter.ClearValue(ContentPresenter.ContentTemplateProperty);
            }

            presenter.Content = content;
        }

        private static DataTemplate? ResolveStringContentTemplate()
        {
            if (Application.Current?.Resources is { } appResources
                && DaisyResourceLookup.TryGetResource(appResources, StringContentTemplateKey, out var appValue)
                && appValue is DataTemplate appTemplate)
            {
                return appTemplate;
            }

            try
            {
                var buttonResources = new ResourceDictionary { Source = new Uri(StringContentTemplateSource) };
                if (DaisyResourceLookup.TryGetResource(buttonResources, StringContentTemplateKey, out var value)
                    && value is DataTemplate template)
                {
                    return template;
                }
            }
            catch (Exception ex)
            {
                FloweryDiagnostics.Log($"[FloweryMnemonicHelpers] Could not load {StringContentTemplateSource}: {ex.Message}");
            }

            return null;
        }
    }
}
