using System.Diagnostics.CodeAnalysis;

namespace Flowery.Controls
{
    /// <summary>
    /// Provides attached properties for Daisy controls to support icons AND content together.
    /// This follows the Uno.Themes ControlExtensions pattern - the gold standard for icon handling.
    ///
    /// ## Why this pattern?
    /// - UIElements can only have ONE parent. Passing PathIcon as Content causes silent failures.
    /// - Using an attached property allows the TEMPLATE to instantiate a ContentPresenter that
    ///   binds to the icon, avoiding parentage issues entirely.
    /// - Supports icon + text on the same control (not mutually exclusive).
    ///
    /// &lt;para&gt;&lt;b&gt;Usage in XAML:&lt;/b&gt;&lt;/para&gt;
    /// &lt;code&gt;
    /// &amp;lt;daisy:DaisyButton Content="Save"&amp;gt;
    ///     &amp;lt;daisy:DaisyControlExtensions.Icon&amp;gt;
    ///         &amp;lt;PathIcon Data="M..." /&amp;gt;
    ///     &amp;lt;/daisy:DaisyControlExtensions.Icon&amp;gt;
    /// &amp;lt;/daisy:DaisyButton&amp;gt;
    /// &lt;/code&gt;
    ///
    /// &lt;para&gt;&lt;b&gt;Usage for icon-only buttons:&lt;/b&gt;&lt;/para&gt;
    /// &lt;code&gt;
    /// &amp;lt;daisy:DaisyButton Shape="Circle"&amp;gt;
    ///     &amp;lt;daisy:DaisyControlExtensions.Icon&amp;gt;
    ///         &amp;lt;PathIcon Data="M..." /&amp;gt;
    ///     &amp;lt;/daisy:DaisyControlExtensions.Icon&amp;gt;
    /// &amp;lt;/daisy:DaisyButton&amp;gt;
    /// &lt;/code&gt;
    /// </summary>
    [Bindable]
    public static class DaisyControlExtensions
    {
        #region Icon Attached Property

        /// <summary>
        /// Identifies the Icon attached property.
        /// Used to set an icon on controls like DaisyButton, DaisyFab, etc.
        /// </summary>
        public static DependencyProperty IconProperty { [DynamicDependency(nameof(GetIcon))] get; } =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(IconElement),
                typeof(DaisyControlExtensions),
                new PropertyMetadata(null, OnIconChanged));

        /// <summary>
        /// Gets the icon for the specified element.
        /// </summary>
        [DynamicDependency(nameof(SetIcon))]
        public static IconElement? GetIcon(DependencyObject obj)
        {
            return (IconElement?)obj.GetValue(IconProperty);
        }

        /// <summary>
        /// Sets the icon for the specified element.
        /// </summary>
        [DynamicDependency(nameof(GetIcon))]
        public static void SetIcon(DependencyObject obj, IconElement? value)
        {
            obj.SetValue(IconProperty, value);
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Controls that support this property should listen for changes
            // or read the property in their template/code.
            // The change callback is here for future extensibility (e.g., triggering visual updates).
        }

        #endregion

        #region IconWidth Attached Property

        /// <summary>
        /// Identifies the IconWidth attached property.
        /// Controls the width of the icon. Default is NaN (auto-sized).
        /// </summary>
        public static DependencyProperty IconWidthProperty { [DynamicDependency(nameof(GetIconWidth))] get; } =
            DependencyProperty.RegisterAttached(
                "IconWidth",
                typeof(double),
                typeof(DaisyControlExtensions),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// Gets the icon width for the specified element.
        /// </summary>
        [DynamicDependency(nameof(SetIconWidth))]
        public static double GetIconWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(IconWidthProperty);
        }

        /// <summary>
        /// Sets the icon width for the specified element.
        /// </summary>
        [DynamicDependency(nameof(GetIconWidth))]
        public static void SetIconWidth(DependencyObject obj, double value)
        {
            obj.SetValue(IconWidthProperty, value);
        }

        #endregion

        #region IconHeight Attached Property

        /// <summary>
        /// Identifies the IconHeight attached property.
        /// Controls the height of the icon. Default is NaN (auto-sized).
        /// </summary>
        public static DependencyProperty IconHeightProperty { [DynamicDependency(nameof(GetIconHeight))] get; } =
            DependencyProperty.RegisterAttached(
                "IconHeight",
                typeof(double),
                typeof(DaisyControlExtensions),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// Gets the icon height for the specified element.
        /// </summary>
        [DynamicDependency(nameof(SetIconHeight))]
        public static double GetIconHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(IconHeightProperty);
        }

        /// <summary>
        /// Sets the icon height for the specified element.
        /// </summary>
        [DynamicDependency(nameof(GetIconHeight))]
        public static void SetIconHeight(DependencyObject obj, double value)
        {
            obj.SetValue(IconHeightProperty, value);
        }

        #endregion

        #region IconPlacement Attached Property

        /// <summary>
        /// Identifies the IconPlacement attached property.
        /// Controls whether the icon appears before (Left) or after (Right) the content.
        /// </summary>
        public static DependencyProperty IconPlacementProperty { [DynamicDependency(nameof(GetIconPlacement))] get; } =
            DependencyProperty.RegisterAttached(
                "IconPlacement",
                typeof(IconPlacement),
                typeof(DaisyControlExtensions),
                new PropertyMetadata(IconPlacement.Left));

        /// <summary>
        /// Gets the icon placement for the specified element.
        /// </summary>
        [DynamicDependency(nameof(SetIconPlacement))]
        public static IconPlacement GetIconPlacement(DependencyObject obj)
        {
            return (IconPlacement)obj.GetValue(IconPlacementProperty);
        }

        /// <summary>
        /// Sets the icon placement for the specified element.
        /// </summary>
        [DynamicDependency(nameof(GetIconPlacement))]
        public static void SetIconPlacement(DependencyObject obj, IconPlacement value)
        {
            obj.SetValue(IconPlacementProperty, value);
        }

        #endregion

        #region IconSpacing Attached Property

        /// <summary>
        /// Identifies the IconSpacing attached property.
        /// Controls the spacing between the icon and content.
        /// </summary>
        public static DependencyProperty IconSpacingProperty { [DynamicDependency(nameof(GetIconSpacing))] get; } =
            DependencyProperty.RegisterAttached(
                "IconSpacing",
                typeof(double),
                typeof(DaisyControlExtensions),
                new PropertyMetadata(8.0));

        /// <summary>
        /// Gets the icon spacing for the specified element.
        /// </summary>
        [DynamicDependency(nameof(SetIconSpacing))]
        public static double GetIconSpacing(DependencyObject obj)
        {
            return (double)obj.GetValue(IconSpacingProperty);
        }

        /// <summary>
        /// Sets the icon spacing for the specified element.
        /// </summary>
        [DynamicDependency(nameof(GetIconSpacing))]
        public static void SetIconSpacing(DependencyObject obj, double value)
        {
            obj.SetValue(IconSpacingProperty, value);
        }

        #endregion

        #region AlternateContent Attached Property

        /// <summary>
        /// Identifies the AlternateContent attached property.
        /// Used for toggleable controls to show different content when checked vs unchecked.
        /// Useful for DaisyToggle, ToggleButton, etc.
        /// </summary>
        public static DependencyProperty AlternateContentProperty { [DynamicDependency(nameof(GetAlternateContent))] get; } =
            DependencyProperty.RegisterAttached(
                "AlternateContent",
                typeof(object),
                typeof(DaisyControlExtensions),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets the alternate content for the specified element.
        /// </summary>
        [DynamicDependency(nameof(SetAlternateContent))]
        public static object? GetAlternateContent(DependencyObject obj)
        {
            return obj.GetValue(AlternateContentProperty);
        }

        /// <summary>
        /// Sets the alternate content for the specified element.
        /// </summary>
        [DynamicDependency(nameof(GetAlternateContent))]
        public static void SetAlternateContent(DependencyObject obj, object? value)
        {
            obj.SetValue(AlternateContentProperty, value);
        }

        #endregion
    }

    /// <summary>
    /// Specifies where the icon should appear relative to the content.
    /// </summary>
    public enum IconPlacement
    {
        /// <summary>Icon appears to the left of the content.</summary>
        Left,

        /// <summary>Icon appears to the right of the content.</summary>
        Right,

        /// <summary>Icon appears above the content.</summary>
        Top,

        /// <summary>Icon appears below the content.</summary>
        Bottom
    }
}
