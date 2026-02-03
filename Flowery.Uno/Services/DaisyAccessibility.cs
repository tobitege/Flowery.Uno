using System;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Flowery.Services;

/// <summary>
/// A static helper class providing accessibility utilities for Daisy controls.
/// Simplifies setting up screen reader support across all controls.
/// </summary>
public static class DaisyAccessibility
{
    #region AccessibleText Attached Property

    /// <summary>
    /// Identifies the AccessibleText attached property.
    /// </summary>
    public static readonly DependencyProperty AccessibleTextProperty =
        DependencyProperty.RegisterAttached(
            "AccessibleText",
            typeof(string),
            typeof(DaisyAccessibility),
            new PropertyMetadata(null, OnAccessibleTextChanged));

    /// <summary>
    /// Gets the accessible text for a control.
    /// </summary>
    /// <param name="obj">The dependency object to get the value from.</param>
    /// <returns>The accessible text string.</returns>
    public static string GetAccessibleText(DependencyObject obj)
    {
        return (string)obj.GetValue(AccessibleTextProperty);
    }

    /// <summary>
    /// Sets the accessible text for a control.
    /// </summary>
    /// <param name="obj">The dependency object to set the value on.</param>
    /// <param name="value">The accessible text to set.</param>
    public static void SetAccessibleText(DependencyObject obj, string value)
    {
        obj.SetValue(AccessibleTextProperty, value);
    }

    private static void OnAccessibleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is string text)
        {
            AutomationProperties.SetName(element, text);
        }
    }

    #endregion

    #region Setup Helpers for Control Authors

    /// <summary>
    /// Registers accessibility handling for a control type.
    /// Call this in the static constructor of your control.
    /// </summary>
    /// <typeparam name="T">The control type to register.</typeparam>
    /// <param name="defaultText">The default accessible text for this control type.</param>
    /// <remarks>
    /// This method sets up a default style that includes the default accessible text.
    /// Control instances can override this by setting the AccessibleText attached property.
    /// </remarks>
    public static void SetupAccessibility<T>(string defaultText) where T : UIElement
    {
        // In WinUI/Uno, we register a callback for the AccessibleText property
        // that will sync to AutomationProperties.Name.
        // The default text is applied when the control is loaded if no custom text is set.
        
        // Store the default text for this type
        _defaultTexts[typeof(T)] = defaultText;
    }

    /// <summary>
    /// Gets the effective accessible text for a control, with fallback to the default.
    /// </summary>
    /// <param name="control">The control to get accessible text for.</param>
    /// <param name="defaultText">The default text to use if no custom text is set.</param>
    /// <returns>The effective accessible text.</returns>
    public static string GetEffectiveAccessibleText(UIElement control, string? defaultText = null)
    {
        var customText = GetAccessibleText(control);
        
        if (!string.IsNullOrEmpty(customText))
        {
            return customText;
        }

        // Try to get the registered default for this type
        if (defaultText == null && _defaultTexts.TryGetValue(control.GetType(), out var registeredDefault))
        {
            defaultText = registeredDefault;
        }

        return defaultText ?? string.Empty;
    }

    /// <summary>
    /// Applies the effective accessible text to a control's AutomationProperties.Name.
    /// Call this in OnApplyTemplate or Loaded event of your control.
    /// </summary>
    /// <param name="control">The control to apply accessibility to.</param>
    /// <param name="defaultText">Optional default text to use if no custom text is set.</param>
    public static void ApplyAccessibility(UIElement control, string? defaultText = null)
    {
        var effectiveText = GetEffectiveAccessibleText(control, defaultText);
        
        if (!string.IsNullOrEmpty(effectiveText))
        {
            AutomationProperties.SetName(control, effectiveText);
        }
    }

    // Storage for registered default texts by control type
    private static readonly Dictionary<Type, string> _defaultTexts = [];

    #endregion

    #region Shared Helpers for Control Authors

    public static void SetAutomationNameOrClear(DependencyObject? element, string? name)
    {
        if (element == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            element.ClearValue(AutomationProperties.NameProperty);
            return;
        }

        AutomationProperties.SetName(element, name);
    }

    public static string? GetAccessibleNameFromContent(object? content, string? fallbackText = null)
    {
        switch (content)
        {
            case string text:
                return text;
            case TextBlock textBlock:
                return textBlock.Text;
            case DaisyIconText iconText:
                return iconText.Text;
            case ContentControl control:
                return GetAccessibleNameFromContent(control.Content, fallbackText);
            default:
                return fallbackText;
        }
    }

    public static void FocusOnPointer(UIElement? element)
    {
        if (element == null)
        {
            return;
        }

        element.Focus(FocusState.Pointer);
    }

    public static bool TryHandleEnterOrSpace(KeyRoutedEventArgs e, Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (e.Handled)
        {
            return false;
        }

        if (e.Key != VirtualKey.Enter && e.Key != VirtualKey.Space)
        {
            return false;
        }

        action();
        e.Handled = true;
        return true;
    }

    #endregion
}
