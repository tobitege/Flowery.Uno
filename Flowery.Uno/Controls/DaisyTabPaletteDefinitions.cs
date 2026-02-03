using System.Collections.Generic;
using Flowery.Helpers;

namespace Flowery.Controls
{
    internal sealed class DaisyTabPaletteDefinition
    {
        public DaisyTabPaletteColor Color { get; }
        public string Name { get; }
        public string BackgroundHex { get; }
        public string ForegroundHex { get; }

        public DaisyTabPaletteDefinition(DaisyTabPaletteColor color, string name, string backgroundHex, string foregroundHex)
        {
            Color = color;
            Name = name;
            BackgroundHex = backgroundHex;
            ForegroundHex = foregroundHex;
        }
    }

    internal static class DaisyTabPaletteDefinitions
    {
        private static readonly IReadOnlyDictionary<DaisyTabPaletteColor, DaisyTabPaletteDefinition> Definitions =
            new Dictionary<DaisyTabPaletteColor, DaisyTabPaletteDefinition>
            {
                [DaisyTabPaletteColor.Purple] = Create(DaisyTabPaletteColor.Purple, "Purple", "#7c3aed"),
                [DaisyTabPaletteColor.Indigo] = Create(DaisyTabPaletteColor.Indigo, "Indigo", "#6366f1"),
                [DaisyTabPaletteColor.Pink] = Create(DaisyTabPaletteColor.Pink, "Pink", "#f472b6"),
                [DaisyTabPaletteColor.SkyBlue] = Create(DaisyTabPaletteColor.SkyBlue, "Sky Blue", "#38bdf8"),
                [DaisyTabPaletteColor.Blue] = Create(DaisyTabPaletteColor.Blue, "Blue", "#0ea5e9"),
                [DaisyTabPaletteColor.Lime] = Create(DaisyTabPaletteColor.Lime, "Lime", "#84cc16"),
                [DaisyTabPaletteColor.Green] = Create(DaisyTabPaletteColor.Green, "Green", "#22c55e"),
                [DaisyTabPaletteColor.Yellow] = Create(DaisyTabPaletteColor.Yellow, "Yellow", "#eab308"),
                [DaisyTabPaletteColor.Orange] = Create(DaisyTabPaletteColor.Orange, "Orange", "#f59e0b"),
                [DaisyTabPaletteColor.Red] = Create(DaisyTabPaletteColor.Red, "Red", "#ef4444"),
                [DaisyTabPaletteColor.Gray] = Create(DaisyTabPaletteColor.Gray, "Gray", "#64748b"),
            };

        public static bool TryGet(DaisyTabPaletteColor color, out DaisyTabPaletteDefinition definition) =>
            Definitions.TryGetValue(color, out definition!);

        public static IReadOnlyList<DaisyTabPaletteSwatch> GetSwatches()
        {
            var swatches = new List<DaisyTabPaletteSwatch>(Definitions.Count + 1)
            {
                new DaisyTabPaletteSwatch(DaisyTabPaletteColor.Default, "Default", null)
            };

            var order = new[]
            {
                DaisyTabPaletteColor.Purple,
                DaisyTabPaletteColor.Indigo,
                DaisyTabPaletteColor.Pink,
                DaisyTabPaletteColor.SkyBlue,
                DaisyTabPaletteColor.Blue,
                DaisyTabPaletteColor.Lime,
                DaisyTabPaletteColor.Green,
                DaisyTabPaletteColor.Yellow,
                DaisyTabPaletteColor.Orange,
                DaisyTabPaletteColor.Red,
                DaisyTabPaletteColor.Gray
            };

            foreach (var color in order)
            {
                if (Definitions.TryGetValue(color, out var def))
                {
                    swatches.Add(new DaisyTabPaletteSwatch(def.Color, def.Name, def.BackgroundHex));
                }
            }

            return swatches;
        }

        private static DaisyTabPaletteDefinition Create(DaisyTabPaletteColor color, string name, string backgroundHex)
        {
            var foregroundHex = FloweryColorHelpers.GetContrastColorHex(backgroundHex);
            return new DaisyTabPaletteDefinition(color, name, backgroundHex, foregroundHex);
        }
    }
}
