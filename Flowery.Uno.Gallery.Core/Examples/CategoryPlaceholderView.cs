using System;
using System.Collections.Generic;
using System.Globalization;
using Flowery.Controls;
using Flowery.Localization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class CategoryPlaceholderView : UserControl, IScrollableExample
    {
        private readonly SidebarCategory _category;
        private readonly Dictionary<string, FrameworkElement> _sections;
        private readonly List<(SidebarItem item, TextBlock title)> _sectionTitles;
        private readonly TextBlock _header;

        public CategoryPlaceholderView(SidebarCategory category)
        {
            _category = category ?? throw new ArgumentNullException(nameof(category));
            _sections = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase);
            _sectionTitles = [];

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsTabStop = true,
                // Transparent background required for hit-testing (mouse wheel input)
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            var stack = new StackPanel
            {
                Spacing = 12,
                Margin = new Thickness(24, 16, 24, 24),
                // Transparent background required for hit-testing (mouse wheel input)
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            _header = new TextBlock
            {
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = TryGetBrush("DaisyBaseContentBrush")
            };

            stack.Children.Add(_header);

            foreach (var item in category.Items)
            {
                var section = CreateSection(item);
                stack.Children.Add(section);
                _sections[item.Id] = section;
            }

            scrollViewer.Content = stack;
            Content = scrollViewer;

            UpdateLocalizedText();

            FloweryLocalization.CultureChanged += OnCultureChanged;
            Unloaded += OnUnloaded;
        }

        public void ScrollToSection(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return;

            if (_sections.TryGetValue(sectionName, out var element))
                element.StartBringIntoView();
        }

        private StackPanel CreateSection(SidebarItem item)
        {
            var container = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var title = new TextBlock
            {
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = TryGetBrush("DaisyBaseContentBrush")
            };

            _sectionTitles.Add((item, title));

            var body = new TextBlock
            {
                Text = $"Placeholder for {item.Id}",
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                Foreground = TryGetBrush("DaisyBaseContentBrush")
            };

            container.Children.Add(title);
            container.Children.Add(body);
            return container;
        }

        private void UpdateLocalizedText()
        {
            _header.Text = FloweryLocalization.GetString(_category.Name);

            foreach (var (item, title) in _sectionTitles)
                title.Text = FloweryLocalization.GetString(item.Name);
        }

        private static Brush? TryGetBrush(string key)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var resource) == true &&
                resource is Brush brush)
            {
                return brush;
            }

            return null;
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            UpdateLocalizedText();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            FloweryLocalization.CultureChanged -= OnCultureChanged;
            Unloaded -= OnUnloaded;
        }
    }
}
