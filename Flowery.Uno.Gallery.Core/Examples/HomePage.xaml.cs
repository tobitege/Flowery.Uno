using System;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class HomePage : UserControl
    {
        public event EventHandler? BrowseComponentsRequested;
        public event EventHandler? OpenGitHubRequested;
        public event EventHandler? OpenUnoRepoRequested;

        public HomePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public void AppendDiagnostics(string message)
        {
            if (DiagnosticsTextBox == null || string.IsNullOrEmpty(message))
                return;

            if (!string.IsNullOrEmpty(DiagnosticsTextBox.Text))
            {
                DiagnosticsTextBox.Text += Environment.NewLine;
                DiagnosticsTextBox.Text += new string('#', 50);
                DiagnosticsTextBox.Text += Environment.NewLine;
            }

            DiagnosticsTextBox.Text += message;
        }

        public void SetDiagnosticsText(string? text)
        {
            if (DiagnosticsTextBox == null)
                return;

            DiagnosticsTextBox.Text = text ?? string.Empty;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            ApplyTheme();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, string themeName)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var baseContentBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            if (baseContentBrush == null)
                return;

            if (TitleText != null)
                TitleText.Foreground = baseContentBrush;
            if (SubtitleText != null)
                SubtitleText.Foreground = baseContentBrush;
            if (CreditsTitleText != null)
                CreditsTitleText.Foreground = baseContentBrush;
            if (CreditsBodyText != null)
                CreditsBodyText.Foreground = baseContentBrush;
            if (DisclaimerText != null)
                DisclaimerText.Foreground = baseContentBrush;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            BrowseComponentsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            OpenGitHubRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UnoRepoButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            OpenUnoRepoRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
