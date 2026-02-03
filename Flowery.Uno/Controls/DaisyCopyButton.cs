using System;
using System.Threading.Tasks;
using Flowery.Localization;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace Flowery.Controls
{
    /// <summary>
    /// A DaisyButton that copies text to the clipboard and briefly shows a success state.
    /// </summary>
    public partial class DaisyCopyButton : DaisyButton
    {
        private bool _isBusy;
        private object? _idleContent;

        public DaisyCopyButton()
        {
            Content ??= FloweryLocalization.GetStringInternal("CopyButton_Copy", "Copy");

            Click += OnButtonClick;
        }

        #region Dependency Properties

        public static readonly DependencyProperty CopyTextProperty =
            DependencyProperty.Register(
                nameof(CopyText),
                typeof(string),
                typeof(DaisyCopyButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text to copy to clipboard when clicked.
        /// </summary>
        public string? CopyText
        {
            get => (string?)GetValue(CopyTextProperty);
            set => SetValue(CopyTextProperty, value);
        }

        public static readonly DependencyProperty SuccessDurationProperty =
            DependencyProperty.Register(
                nameof(SuccessDuration),
                typeof(TimeSpan),
                typeof(DaisyCopyButton),
                new PropertyMetadata(TimeSpan.FromSeconds(2)));

        /// <summary>
        /// Gets or sets how long the success state is shown.
        /// </summary>
        public TimeSpan SuccessDuration
        {
            get => (TimeSpan)GetValue(SuccessDurationProperty);
            set => SetValue(SuccessDurationProperty, value);
        }

        public static readonly DependencyProperty SuccessContentProperty =
            DependencyProperty.Register(
                nameof(SuccessContent),
                typeof(object),
                typeof(DaisyCopyButton),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("CopyButton_Copied", "Copied")));

        /// <summary>
        /// Gets or sets the content displayed while in success state.
        /// </summary>
        public object? SuccessContent
        {
            get => GetValue(SuccessContentProperty);
            set => SetValue(SuccessContentProperty, value);
        }

        /// <summary>
        /// Event raised after text is successfully copied to clipboard.
        /// </summary>
        public event EventHandler? TextCopied;

        #endregion

        #region Click Handling

        private async void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (_isBusy) return;

            _isBusy = true;

            // Save idle state
            _idleContent ??= Content;
            var idleEnabled = IsEnabled;

            try
            {
                // Try to copy text to clipboard. This may be unsupported on some Uno targets.
                try
                {
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(CopyText ?? string.Empty);
                    Clipboard.SetContent(dataPackage);
                    Clipboard.Flush();
                    TextCopied?.Invoke(this, EventArgs.Empty);
                }
                catch
                {
                    // Even if clipboard fails, keep UX consistent with Avalonia demo: show success state briefly.
                }

                Content = SuccessContent;
                IsEnabled = false;

                // Wait for success duration
                await Task.Delay(SuccessDuration);
            }
            finally
            {
                // Restore idle state
                Content = _idleContent;
                IsEnabled = idleEnabled;
                _isBusy = false;
            }
        }

        #endregion
    }
}
