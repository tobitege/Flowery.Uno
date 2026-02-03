using Microsoft.UI.Xaml;

namespace Flowery.Uno.Gallery.Examples
{
    public static class SectionProps
    {
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.RegisterAttached(
                "Id",
                typeof(string),
                typeof(SectionProps),
                new PropertyMetadata(null));

        public static string GetId(DependencyObject obj) => (string)obj.GetValue(IdProperty);
        public static void SetId(DependencyObject obj, string value) => obj.SetValue(IdProperty, value);
    }
}
