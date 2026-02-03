using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Flowery.Controls
{
    [ContentProperty(Name = nameof(Text))]
    public sealed partial class DaisySelectItem : DependencyObject
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(DaisySelectItem), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty LocalizationKeyProperty =
            DependencyProperty.Register(nameof(LocalizationKey), typeof(string), typeof(DaisySelectItem), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TagProperty =
            DependencyProperty.Register(nameof(Tag), typeof(object), typeof(DaisySelectItem), new PropertyMetadata(null));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string LocalizationKey
        {
            get => (string)GetValue(LocalizationKeyProperty);
            set => SetValue(LocalizationKeyProperty, value);
        }

        public object? Tag
        {
            get => GetValue(TagProperty);
            set => SetValue(TagProperty, value);
        }

        public override string ToString() => Text ?? string.Empty;
    }
}
