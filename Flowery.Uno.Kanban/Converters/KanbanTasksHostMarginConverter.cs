using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Flowery.Uno.Kanban.Converters;

/// <summary>
/// Converts a column <see cref="Thickness"/> padding into a margin that cancels the horizontal padding
/// for the tasks viewport area (Skia/Desktop only). This keeps the column header padded while allowing
/// cards to sit closer to the column border.
/// </summary>
public sealed class KanbanTasksHostMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

