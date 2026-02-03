using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Flowery.Uno.RuntimeTests.Controls
{
    internal static class ControlCatalog
    {
        private static readonly Type ControlBaseType = typeof(FrameworkElement);

        public static IReadOnlyList<Type> GetAllControls()
        {
            return GetControlsByNamespace("Flowery.Controls");
        }

        public static IReadOnlyList<Type> GetDaisyControls()
        {
            return GetControlsByNamespace("Flowery.Controls")
                .Where(type => !type.Namespace!.Contains(".Kanban", StringComparison.Ordinal)
                               && !type.Namespace.Contains(".ColorPicker", StringComparison.Ordinal)
                               && !type.Namespace.Contains(".Custom", StringComparison.Ordinal))
                .ToList();
        }

        public static IReadOnlyList<Type> GetKanbanControls()
        {
            return GetControlsByNamespace("Flowery.Uno.Kanban.Controls");
        }

        public static IReadOnlyList<Type> GetColorPickerControls()
        {
            return GetControlsByNamespace("Flowery.Controls.ColorPicker");
        }

        public static IReadOnlyList<Type> GetCustomControls()
        {
            return GetControlsByNamespace("Flowery.Controls");
        }

        private static IReadOnlyList<Type> GetControlsByNamespace(string rootNamespace)
        {
            var assembly = typeof(Flowery.Controls.DaisyButton).Assembly;
            return assembly.GetTypes()
                .Where(type => type.IsPublic)
                .Where(type => !type.IsAbstract)
                .Where(type => type.Namespace != null && type.Namespace.StartsWith(rootNamespace, StringComparison.Ordinal))
                .Where(type => ControlBaseType.IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();
        }
    }
}
