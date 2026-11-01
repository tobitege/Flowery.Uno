using Flowery.Uno.Photos.Localization;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// A localized option for <see cref="DaisySelect"/>; ToString provides the display text.
    /// </summary>
    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class SelectOption(object key, string text)
    {
        public object Key { get; } = key;
        public string Text { get; } = text;
        public override string ToString() => Text;
    }

    /// <summary>
    /// Small factory helpers for code-built, localized Daisy UI fragments.
    /// </summary>
    internal static class UiHelpers
    {
        public static string L(string key) => PhotosLocalization.GetString(key);

        /// <summary>Scale factor for app-specific dimensions that should follow the global Daisy size.</summary>
        public static double SizeFactor => FlowerySizeManager.CurrentSize switch
        {
            DaisySize.ExtraSmall => 0.85,
            DaisySize.Small => 0.93,
            DaisySize.Large => 1.15,
            DaisySize.ExtraLarge => 1.3,
            _ => 1.0
        };

        public static TextBlock Responsive(TextBlock textBlock, ResponsiveFontTier tier)
        {
            FlowerySizeManager.SetResponsiveFont(textBlock, tier);
            return textBlock;
        }

        public static TextBlock Caption(string key, Thickness? margin = null) => Responsive(new TextBlock
        {
            Text = L(key),
            Style = (Style)Application.Current.Resources["PhotosCaptionTextStyle"],
            Margin = margin ?? new Thickness(0, 4, 0, 0)
        }, ResponsiveFontTier.Secondary);

        /// <summary>
        /// Flyout content is one size step below the global size, so captions, selects, toggles and menu items
        /// share one font size (the caption size from Small to Large).
        /// </summary>
        public static DaisySize FlyoutSize => FlowerySizeManager.CurrentSize switch
        {
            DaisySize.ExtraLarge => DaisySize.Large,
            DaisySize.Large => DaisySize.Medium,
            DaisySize.Medium => DaisySize.Small,
            _ => DaisySize.ExtraSmall
        };

        public static double FlyoutFontSize => FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, FlyoutSize);

        public static TextBlock FlyoutCaption(string key, Thickness? margin = null) => new()
        {
            Text = L(key),
            Style = (Style)Application.Current.Resources["PhotosCaptionTextStyle"],
            FontSize = FlyoutFontSize,
            Margin = margin ?? new Thickness(0, 4, 0, 0)
        };

        public static void ApplyFlyoutFont(IList<MenuFlyoutItemBase> items)
        {
            var fontSize = FlyoutFontSize;
            foreach (var item in items)
            {
                item.FontSize = fontSize;
                if (item is MenuFlyoutSubItem sub)
                    ApplyFlyoutFont(sub.Items);
            }
        }

        public static MenuFlyoutItem MenuItem(string key, Symbol? symbol, Action onClick, string? iconData = null) =>
            MenuItemText(L(key), symbol, onClick, iconData);

        public static MenuFlyoutItem MenuItemText(string text, Symbol? symbol, Action onClick, string? iconData = null)
        {
            var item = new MenuFlyoutItem { Text = text };
            if (symbol is { } s)
            {
                item.Icon = new SymbolIcon(s);
            }
            else if (iconData != null)
            {
                item.Icon = new PathIcon
                {
                    Data = (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), iconData)
                };
            }
            item.Click += (_, _) => onClick();
            return item;
        }

        /// <summary>Sets tooltip and accessible name (icon-only buttons need both).</summary>
        public static void Tip(FrameworkElement element, string text)
        {
            ToolTipService.SetToolTip(element, text);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(element, text);
        }

        public static string? Icon(string resourceKey) => Application.Current.Resources[resourceKey] as string;

        public static DaisySelect Select<T>(IEnumerable<(T Key, string TextKey)> options, T selected, Action<T> onChanged,
            double width = double.NaN, DaisySize? size = null)
            where T : notnull
        {
            var items = options.Select(o => new SelectOption(o.Key, L(o.TextKey))).ToList();
            var select = new DaisySelect
            {
                ItemsSource = items,
                SelectedItem = items.FirstOrDefault(i => Equals(i.Key, selected)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = width
            };
            if (size is { } fixedSize)
            {
                FlowerySizeManager.SetIgnoreGlobalSize(select, true);
                select.Size = fixedSize;
            }
            select.SelectionChanged += (_, _) =>
            {
                if (select.SelectedItem is SelectOption { Key: T key })
                    onChanged(key);
            };
            return select;
        }

        public static Grid ToggleRow(string key, bool isOn, Action<bool> onChanged, DaisySize? size = null)
        {
            var grid = new Grid { ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var label = new TextBlock
            {
                Text = L(key),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
            if (size is { } fixedSize)
                label.FontSize = FlowerySizeManager.GetFontSizeForTier(ResponsiveFontTier.Primary, fixedSize);
            else
                Responsive(label, ResponsiveFontTier.Primary);
            var toggle = new DaisyToggle { IsOn = isOn, Variant = DaisyToggleVariant.Primary, VerticalAlignment = VerticalAlignment.Center, Size = size ?? FlowerySizeManager.CurrentSize };
            if (size != null)
                FlowerySizeManager.SetIgnoreGlobalSize(toggle, true);
            // DaisyToggle also raises Toggled for programmatic IsOn changes; only forward real changes.
            var current = isOn;
            toggle.Toggled += (_, _) =>
            {
                if (toggle.IsOn == current)
                    return;
                current = toggle.IsOn;
                onChanged(current);
            };
            Grid.SetColumn(toggle, 1);
            grid.Children.Add(label);
            grid.Children.Add(toggle);
            return grid;
        }

        public static DaisyRange Range(double min, double max, double value, double step, Action<double> onChanged)
        {
            var range = new DaisyRange
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                StepFrequency = step,
                SmallChange = step,
                LargeChange = step * 5,
                Variant = DaisyRangeVariant.Primary,
                ShowProgress = true,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            range.RegisterPropertyChangedCallback(DaisyRange.ValueProperty, (_, _) => onChanged(range.Value));
            return range;
        }
    }
}
