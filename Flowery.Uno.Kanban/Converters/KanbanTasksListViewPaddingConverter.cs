using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Flowery.Uno.Kanban.Converters;

/// <summary>
/// Returns the ListView padding used for the tasks list.
/// Skia/Desktop uses non-overlay scrollbars, so extra right padding creates an unnecessary gap.
/// </summary>
public sealed class KanbanTasksListViewPaddingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
#if HAS_UNO_SKIA
        // Reserve a right gutter so the scrollbar doesn't overlap cards.
        return new Thickness(0, 0, 20, 0);
#else
        // Preserve the existing XAML intent: add a small right inset where scrollbars can overlap content.
        return new Thickness(0, 0, 12, 0);
#endif
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

