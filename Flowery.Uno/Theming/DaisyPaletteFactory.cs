using System;
using System.Collections.Generic;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Theming
{
    /// <summary>
    /// Holds all color values for a DaisyUI theme palette.
    /// </summary>
    public sealed partial class ThemePalette
    {
        public string Primary { get; init; } = "#605DFF";
        public string PrimaryFocus { get; init; } = "#4C4ACC";
        public string PrimaryContent { get; init; } = "#FFFFFF";
        public string Secondary { get; init; } = "#F43098";
        public string SecondaryFocus { get; init; } = "#C32679";
        public string SecondaryContent { get; init; } = "#FFFFFF";
        public string Accent { get; init; } = "#00D3BB";
        public string AccentFocus { get; init; } = "#00A895";
        public string AccentContent { get; init; } = "#FFFFFF";
        public string Neutral { get; init; } = "#09090B";
        public string NeutralFocus { get; init; } = "#070708";
        public string NeutralContent { get; init; } = "#E4E4E7";
        public string Base100 { get; init; } = "#1D232A";
        public string Base200 { get; init; } = "#191E24";
        public string Base300 { get; init; } = "#374151";
        public string BaseContent { get; init; } = "#FFFFFF";
        public string Info { get; init; } = "#00BAFE";
        public string InfoContent { get; init; } = "#000000";
        public string Success { get; init; } = "#00D390";
        public string SuccessContent { get; init; } = "#000000";
        public string Warning { get; init; } = "#FCB700";
        public string WarningContent { get; init; } = "#000000";
        public string Error { get; init; } = "#FF627D";
        public string ErrorContent { get; init; } = "#000000";
    }

    /// <summary>
    /// Factory for creating DaisyUI theme palettes as ResourceDictionary objects.
    /// Contains all 36 DaisyUI themes ported from the Avalonia version.
    /// </summary>
    public static class DaisyPaletteFactory
    {
        private static readonly Dictionary<string, ThemePalette> Palettes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Abyss"] = new ThemePalette
            {
                Primary = "#BDFF00", PrimaryFocus = "#97CC00", PrimaryContent = "#427600",
                Secondary = "#CEBEF4", SecondaryFocus = "#A498C3", SecondaryContent = "#564775",
                Accent = "#505050", AccentFocus = "#404040", AccentContent = "#F8F8F8",
                Neutral = "#003843", NeutralFocus = "#002C35", NeutralContent = "#FFD6A7",
                Base100 = "#001E29", Base200 = "#00111D", Base300 = "#60757D", BaseContent = "#FFD6A7",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#01DF72", SuccessContent = "#000000",
                Warning = "#FFBF00", WarningContent = "#000000", Error = "#F04E4F", ErrorContent = "#FFFFFF"
            },
            ["Acid"] = new ThemePalette
            {
                Primary = "#FF00FF", PrimaryFocus = "#CC00CC", PrimaryContent = "#180017",
                Secondary = "#FF6E00", SecondaryFocus = "#CC5800", SecondaryContent = "#180400",
                Accent = "#C8FF00", AccentFocus = "#A0CC00", AccentContent = "#0F1600",
                Neutral = "#140151", NeutralFocus = "#100040", NeutralContent = "#C7CADC",
                Base100 = "#F8F8F8", Base200 = "#EEEEEE", Base300 = "#C1C1C1", BaseContent = "#000000",
                Info = "#007FFF", InfoContent = "#FFFFFF", Success = "#00FF8A", SuccessContent = "#000000",
                Warning = "#FFE200", WarningContent = "#000000", Error = "#FF0000", ErrorContent = "#FFFFFF"
            },
            ["Aqua"] = new ThemePalette
            {
                Primary = "#13ECF3", PrimaryFocus = "#0FBCC2", PrimaryContent = "#015355",
                Secondary = "#966FB3", SecondaryFocus = "#78588F", SecondaryContent = "#F2F0FC",
                Accent = "#FFE999", AccentFocus = "#CCBA7A", AccentContent = "#161309",
                Neutral = "#05176C", NeutralFocus = "#041256", NeutralContent = "#90BAFF",
                Base100 = "#1A368B", Base200 = "#162455", Base300 = "#7286C1", BaseContent = "#B8E6FE",
                Info = "#2563EB", InfoContent = "#FFFFFF", Success = "#18A34A", SuccessContent = "#FFFFFF",
                Warning = "#D97708", WarningContent = "#000000", Error = "#FF7265", ErrorContent = "#000000"
            },
            ["Autumn"] = new ThemePalette
            {
                Primary = "#8C0327", PrimaryFocus = "#70021F", PrimaryContent = "#EDD0D0",
                Secondary = "#D85251", SecondaryFocus = "#AC4140", SecondaryContent = "#110202",
                Accent = "#D59B6B", AccentFocus = "#AA7C55", AccentContent = "#100904",
                Neutral = "#826A5C", NeutralFocus = "#685449", NeutralContent = "#E5E0DD",
                Base100 = "#F1F1F1", Base200 = "#DBDBDB", Base300 = "#C5C5C5", BaseContent = "#141414",
                Info = "#44ADBB", InfoContent = "#000000", Success = "#499380", SuccessContent = "#FFFFFF",
                Warning = "#E97F16", WarningContent = "#000000", Error = "#D40014", ErrorContent = "#FFFFFF"
            },
            ["Black"] = new ThemePalette
            {
                Primary = "#3A3A3A", PrimaryFocus = "#2E2E2E", PrimaryContent = "#FFFFFF",
                Secondary = "#3A3A3A", SecondaryFocus = "#2E2E2E", SecondaryContent = "#FFFFFF",
                Accent = "#3A3A3A", AccentFocus = "#2E2E2E", AccentContent = "#FFFFFF",
                Neutral = "#3A3A3A", NeutralFocus = "#2E2E2E", NeutralContent = "#FFFFFF",
                Base100 = "#000000", Base200 = "#141414", Base300 = "#1B1B1B", BaseContent = "#D6D6D6",
                Info = "#0000FF", InfoContent = "#FFFFFF", Success = "#028002", SuccessContent = "#FFFFFF",
                Warning = "#FFFF00", WarningContent = "#000000", Error = "#FF0301", ErrorContent = "#FFFFFF"
            },
            ["Bumblebee"] = new ThemePalette
            {
                Primary = "#FDC700", PrimaryFocus = "#CA9F00", PrimaryContent = "#733E0A",
                Secondary = "#FF8904", SecondaryFocus = "#CC6D03", SecondaryContent = "#7C2808",
                Accent = "#000000", AccentFocus = "#000000", AccentContent = "#FFFFFF",
                Neutral = "#433F3A", NeutralFocus = "#35322E", NeutralContent = "#E6E4E3",
                Base100 = "#FFFFFF", Base200 = "#F5F5F5", Base300 = "#C7C7C7", BaseContent = "#161616",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF6266", ErrorContent = "#000000"
            },
            ["Business"] = new ThemePalette
            {
                Primary = "#1C4E80", PrimaryFocus = "#163E66", PrimaryContent = "#D0DAE5",
                Secondary = "#7C909A", SecondaryFocus = "#63737B", SecondaryContent = "#050708",
                Accent = "#EA6947", AccentFocus = "#BB5438", AccentContent = "#130402",
                Neutral = "#23282E", NeutralFocus = "#1C2024", NeutralContent = "#CECFD0",
                Base100 = "#202020", Base200 = "#1C1C1C", Base300 = "#767676", BaseContent = "#CDCDCD",
                Info = "#0291D5", InfoContent = "#FFFFFF", Success = "#6BB187", SuccessContent = "#000000",
                Warning = "#DBAE5A", WarningContent = "#000000", Error = "#AC3E31", ErrorContent = "#FFFFFF"
            },
            ["Caramellatte"] = new ThemePalette
            {
                Primary = "#000000", PrimaryFocus = "#000000", PrimaryContent = "#FFFFFF",
                Secondary = "#370A00", SecondaryFocus = "#2C0800", SecondaryContent = "#FFD6A7",
                Accent = "#8C3F27", AccentFocus = "#70321F", AccentContent = "#FFD6A7",
                Neutral = "#C93400", NeutralFocus = "#A02900", NeutralContent = "#FFF7ED",
                Base100 = "#FFF7ED", Base200 = "#FEECD3", Base300 = "#C7C0B8", BaseContent = "#7C2808",
                Info = "#193AB7", InfoContent = "#FFFFFF", Success = "#006044", SuccessContent = "#FFFFFF",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF6266", ErrorContent = "#000000"
            },
            ["Cmyk"] = new ThemePalette
            {
                Primary = "#45AEEE", PrimaryFocus = "#378BBE", PrimaryContent = "#020B13",
                Secondary = "#E8488A", SecondaryFocus = "#B9396E", SecondaryContent = "#130207",
                Accent = "#FFF234", AccentFocus = "#CCC129", AccentContent = "#161401",
                Neutral = "#1A1A1A", NeutralFocus = "#141414", NeutralContent = "#CBCBCB",
                Base100 = "#FFFFFF", Base200 = "#EEEEEE", Base300 = "#C7C7C7", BaseContent = "#161616",
                Info = "#4BA8C0", InfoContent = "#000000", Success = "#823290", SuccessContent = "#FFFFFF",
                Warning = "#EE8134", WarningContent = "#000000", Error = "#E93F33", ErrorContent = "#FFFFFF"
            },
            ["Coffee"] = new ThemePalette
            {
                Primary = "#DB924C", PrimaryFocus = "#AF743C", PrimaryContent = "#110802",
                Secondary = "#273E3F", SecondaryFocus = "#1F3132", SecondaryContent = "#D0D5D5",
                Accent = "#11576D", AccentFocus = "#0D4557", AccentContent = "#D0DBE0",
                Neutral = "#120C12", NeutralFocus = "#0E090E", NeutralContent = "#C9C7C9",
                Base100 = "#261B25", Base200 = "#1E151D", Base300 = "#7B737A", BaseContent = "#C59F61",
                Info = "#8ECAC1", InfoContent = "#000000", Success = "#9DB787", SuccessContent = "#000000",
                Warning = "#FFD260", WarningContent = "#000000", Error = "#FC9581", ErrorContent = "#000000"
            },
            ["Corporate"] = new ThemePalette
            {
                Primary = "#0082CE", PrimaryFocus = "#0068A4", PrimaryContent = "#FFFFFF",
                Secondary = "#61738D", SecondaryFocus = "#4D5C70", SecondaryContent = "#FFFFFF",
                Accent = "#009689", AccentFocus = "#00786D", AccentContent = "#FFFFFF",
                Neutral = "#000000", NeutralFocus = "#000000", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFF", Base200 = "#E8E8E8", Base300 = "#D1D1D1", BaseContent = "#181A2A",
                Info = "#0090B5", InfoContent = "#FFFFFF", Success = "#00A43B", SuccessContent = "#FFFFFF",
                Warning = "#FDC700", WarningContent = "#000000", Error = "#FF6266", ErrorContent = "#000000"
            },
            ["Cupcake"] = new ThemePalette
            {
                Primary = "#44EBD3", PrimaryFocus = "#36BCA8", PrimaryContent = "#005D58",
                Secondary = "#F9CBE5", SecondaryFocus = "#C7A2B7", SecondaryContent = "#A0004A",
                Accent = "#FFD6A7", AccentFocus = "#CCAB85", AccentContent = "#9F2D00",
                Neutral = "#262629", NeutralFocus = "#1E1E20", NeutralContent = "#E4E4E7",
                Base100 = "#FAF7F5", Base200 = "#EFEAE6", Base300 = "#C3C0BE", BaseContent = "#291334",
                Info = "#00A4F2", InfoContent = "#FFFFFF", Success = "#00BA7B", SuccessContent = "#FFFFFF",
                Warning = "#EEAF00", WarningContent = "#000000", Error = "#FE1C55", ErrorContent = "#FFFFFF"
            },
            ["Cyberpunk"] = new ThemePalette
            {
                Primary = "#FF6596", PrimaryFocus = "#CC5078", PrimaryContent = "#180408",
                Secondary = "#00E8FF", SecondaryFocus = "#00B9CC", SecondaryContent = "#001316",
                Accent = "#CE74FF", AccentFocus = "#A45CCC", AccentContent = "#0F0517",
                Neutral = "#111A3B", NeutralFocus = "#0D142F", NeutralContent = "#FFF248",
                Base100 = "#FFF248", Base200 = "#F7E83A", Base300 = "#E3D40E", BaseContent = "#000000",
                Info = "#00B5FF", InfoContent = "#000000", Success = "#00A96E", SuccessContent = "#FFFFFF",
                Warning = "#FFBE00", WarningContent = "#000000", Error = "#FF5861", ErrorContent = "#000000"
            },
            ["Dark"] = new ThemePalette
            {
                Primary = "#605DFF", PrimaryFocus = "#4C4ACC", PrimaryContent = "#EDF1FE",
                Secondary = "#F43098", SecondaryFocus = "#C32679", SecondaryContent = "#F9E4F0",
                Accent = "#00D3BB", AccentFocus = "#00A895", AccentContent = "#084D49",
                Neutral = "#09090B", NeutralFocus = "#070708", NeutralContent = "#E4E4E7",
                Base100 = "#1D232A", Base200 = "#191E24", Base300 = "#374151", BaseContent = "#ECF9FF",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["Dim"] = new ThemePalette
            {
                Primary = "#9FE88D", PrimaryFocus = "#7FB970", PrimaryContent = "#091307",
                Secondary = "#FF7D5D", SecondaryFocus = "#CC644A", SecondaryContent = "#160503",
                Accent = "#C792E9", AccentFocus = "#9F74BA", AccentContent = "#0E0813",
                Neutral = "#1C212B", NeutralFocus = "#161A22", NeutralContent = "#B2CCD6",
                Base100 = "#2A303C", Base200 = "#242933", Base300 = "#7D828A", BaseContent = "#B2CCD6",
                Info = "#28EBFF", InfoContent = "#000000", Success = "#62EFBD", SuccessContent = "#000000",
                Warning = "#EFD057", WarningContent = "#000000", Error = "#FFAE9B", ErrorContent = "#000000"
            },
            ["Dracula"] = new ThemePalette
            {
                Primary = "#FF79C6", PrimaryFocus = "#CC609E", PrimaryContent = "#16050E",
                Secondary = "#BD93F9", SecondaryFocus = "#9775C7", SecondaryContent = "#0D0815",
                Accent = "#FFB86C", AccentFocus = "#CC9356", AccentContent = "#160D04",
                Neutral = "#414558", NeutralFocus = "#343746", NeutralContent = "#D6D7DB",
                Base100 = "#282A36", Base200 = "#232530", Base300 = "#7C7D86", BaseContent = "#F8F8F3",
                Info = "#8BE9FD", InfoContent = "#000000", Success = "#51FA7B", SuccessContent = "#000000",
                Warning = "#F1FA8C", WarningContent = "#000000", Error = "#FF5555", ErrorContent = "#000000"
            },
            ["Emerald"] = new ThemePalette
            {
                Primary = "#66CC8A", PrimaryFocus = "#51A36E", PrimaryContent = "#223D30",
                Secondary = "#377CFB", SecondaryFocus = "#2C63C8", SecondaryContent = "#FFFFFF",
                Accent = "#F68067", AccentFocus = "#C46652", AccentContent = "#000000",
                Neutral = "#333C4D", NeutralFocus = "#28303D", NeutralContent = "#F9FAFB",
                Base100 = "#FFFFFF", Base200 = "#E8E8E8", Base300 = "#D1D1D1", BaseContent = "#333C4D",
                Info = "#00B5FF", InfoContent = "#000000", Success = "#00A96E", SuccessContent = "#FFFFFF",
                Warning = "#FFBE00", WarningContent = "#000000", Error = "#FF5861", ErrorContent = "#000000"
            },
            ["Fantasy"] = new ThemePalette
            {
                Primary = "#6D0076", PrimaryFocus = "#57005E", PrimaryContent = "#E3CEE4",
                Secondary = "#0075C2", SecondaryFocus = "#005D9B", SecondaryContent = "#CFE4F4",
                Accent = "#FF8600", AccentFocus = "#CC6B00", AccentContent = "#180600",
                Neutral = "#1F2937", NeutralFocus = "#18202C", NeutralContent = "#CDD0D3",
                Base100 = "#FFFFFF", Base200 = "#E8E8E8", Base300 = "#D1D1D1", BaseContent = "#1F2937",
                Info = "#00B5FF", InfoContent = "#000000", Success = "#00A96E", SuccessContent = "#FFFFFF",
                Warning = "#FFBE00", WarningContent = "#000000", Error = "#FF5861", ErrorContent = "#000000"
            },
            ["Forest"] = new ThemePalette
            {
                Primary = "#1FB854", PrimaryFocus = "#189343", PrimaryContent = "#000000",
                Secondary = "#1EB88E", SecondaryFocus = "#189371", SecondaryContent = "#000C07",
                Accent = "#1FB8AB", AccentFocus = "#189388", AccentContent = "#010C0B",
                Neutral = "#19362D", NeutralFocus = "#142B24", NeutralContent = "#CDD3D1",
                Base100 = "#1B1717", Base200 = "#161212", Base300 = "#737070", BaseContent = "#CAC9C9",
                Info = "#00B5FF", InfoContent = "#000000", Success = "#00A96E", SuccessContent = "#FFFFFF",
                Warning = "#FFBE00", WarningContent = "#000000", Error = "#FF5861", ErrorContent = "#000000"
            },
            ["Garden"] = new ThemePalette
            {
                Primary = "#FE0075", PrimaryFocus = "#CB005D", PrimaryContent = "#FFFFFF",
                Secondary = "#8E4162", SecondaryFocus = "#71344E", SecondaryContent = "#EAD7DE",
                Accent = "#5C7F67", AccentFocus = "#496552", AccentContent = "#FFFFFF",
                Neutral = "#291E00", NeutralFocus = "#201800", NeutralContent = "#E9E7E7",
                Base100 = "#E9E7E7", Base200 = "#D4D2D2", Base300 = "#BEBDBD", BaseContent = "#100F0F",
                Info = "#00B5FF", InfoContent = "#000000", Success = "#00A96E", SuccessContent = "#FFFFFF",
                Warning = "#FFBE00", WarningContent = "#000000", Error = "#FF5861", ErrorContent = "#000000"
            },
            ["Halloween"] = new ThemePalette
            {
                Primary = "#FF8F00", PrimaryFocus = "#CC7200", PrimaryContent = "#131616",
                Secondary = "#7A00C2", SecondaryFocus = "#61009B", SecondaryContent = "#E3D4F6",
                Accent = "#42AA00", AccentFocus = "#348800", AccentContent = "#000000",
                Neutral = "#2F1B05", NeutralFocus = "#251504", NeutralContent = "#D2CCC7",
                Base100 = "#1B1816", Base200 = "#0B0908", Base300 = "#73716F", BaseContent = "#CDCDCD",
                Info = "#2563EB", InfoContent = "#FFFFFF", Success = "#18A34A", SuccessContent = "#FFFFFF",
                Warning = "#D97708", WarningContent = "#000000", Error = "#F35248", ErrorContent = "#000000"
            },
            ["Lemonade"] = new ThemePalette
            {
                Primary = "#419400", PrimaryFocus = "#347600", PrimaryContent = "#010800",
                Secondary = "#BDC000", SecondaryFocus = "#979900", SecondaryContent = "#0D0E00",
                Accent = "#EDD000", AccentFocus = "#BDA600", AccentContent = "#141000",
                Neutral = "#343300", NeutralFocus = "#292800", NeutralContent = "#D2D3C7",
                Base100 = "#F8FDEF", Base200 = "#E1E6D9", Base300 = "#CBCFC3", BaseContent = "#151614",
                Info = "#B1D9E9", InfoContent = "#000000", Success = "#B9DBC6", SuccessContent = "#000000",
                Warning = "#D7D3B0", WarningContent = "#000000", Error = "#EFC6C2", ErrorContent = "#000000"
            },
            ["Light"] = new ThemePalette
            {
                Primary = "#422AD5", PrimaryFocus = "#3421AA", PrimaryContent = "#E0E7FF",
                Secondary = "#F43098", SecondaryFocus = "#C32679", SecondaryContent = "#F9E4F0",
                Accent = "#00D3BB", AccentFocus = "#00A895", AccentContent = "#084D49",
                Neutral = "#09090B", NeutralFocus = "#070708", NeutralContent = "#E4E4E7",
                Base100 = "#FFFFFF", Base200 = "#F2F2F2", Base300 = "#D1D5DB", BaseContent = "#18181B",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["Lofi"] = new ThemePalette
            {
                Primary = "#0D0D0D", PrimaryFocus = "#0A0A0A", PrimaryContent = "#FFFFFF",
                Secondary = "#1A1919", SecondaryFocus = "#141414", SecondaryContent = "#FFFFFF",
                Accent = "#262626", AccentFocus = "#1E1E1E", AccentContent = "#FFFFFF",
                Neutral = "#000000", NeutralFocus = "#000000", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFF", Base200 = "#F5F5F5", Base300 = "#C7C7C7", BaseContent = "#000000",
                Info = "#5FCFDD", InfoContent = "#000000", Success = "#69FEC3", SuccessContent = "#000000",
                Warning = "#FFCE69", WarningContent = "#000000", Error = "#FF9181", ErrorContent = "#000000"
            },
            ["Luxury"] = new ThemePalette
            {
                Primary = "#FFFFFF", PrimaryFocus = "#CCCCCC", PrimaryContent = "#161616",
                Secondary = "#152747", SecondaryFocus = "#101F38", SecondaryContent = "#CBD0D7",
                Accent = "#513448", AccentFocus = "#402939", AccentContent = "#DAD3D7",
                Neutral = "#331800", NeutralFocus = "#281300", NeutralContent = "#FFE7A4",
                Base100 = "#09090B", Base200 = "#171618", Base300 = "#1E1D1F", BaseContent = "#DCA54D",
                Info = "#67C6FF", InfoContent = "#000000", Success = "#87D03A", SuccessContent = "#000000",
                Warning = "#E2D563", WarningContent = "#000000", Error = "#FF6F6F", ErrorContent = "#000000"
            },
            ["Night"] = new ThemePalette
            {
                Primary = "#3ABDF7", PrimaryFocus = "#2E97C5", PrimaryContent = "#010D15",
                Secondary = "#818CF8", SecondaryFocus = "#6770C6", SecondaryContent = "#060715",
                Accent = "#F471B5", AccentFocus = "#C35A90", AccentContent = "#14040C",
                Neutral = "#1E293B", NeutralFocus = "#18202F", NeutralContent = "#CDD0D4",
                Base100 = "#0F172A", Base200 = "#0C1425", Base300 = "#6B707D", BaseContent = "#C9CBD0",
                Info = "#0CA5E9", InfoContent = "#FFFFFF", Success = "#2FD4BF", SuccessContent = "#000000",
                Warning = "#F4BF51", WarningContent = "#000000", Error = "#FB7085", ErrorContent = "#000000"
            },
            ["Nord"] = new ThemePalette
            {
                Primary = "#5E81AC", PrimaryFocus = "#4B6789", PrimaryContent = "#03060B",
                Secondary = "#81A1C1", SecondaryFocus = "#67809A", SecondaryContent = "#06090D",
                Accent = "#88C0D0", AccentFocus = "#6C99A6", AccentContent = "#070D10",
                Neutral = "#4C566A", NeutralFocus = "#3C4454", NeutralContent = "#D8DEE9",
                Base100 = "#ECEFF4", Base200 = "#E5E9F0", Base300 = "#B7B9BE", BaseContent = "#2E3440",
                Info = "#B48EAD", InfoContent = "#000000", Success = "#A3BE8D", SuccessContent = "#000000",
                Warning = "#EBCB8B", WarningContent = "#000000", Error = "#BF616A", ErrorContent = "#FFFFFF"
            },
            ["Pastel"] = new ThemePalette
            {
                Primary = "#E9D4FF", PrimaryFocus = "#BAA9CC", PrimaryContent = "#8000D9",
                Secondary = "#FECCD2", SecondaryFocus = "#CBA3A8", SecondaryContent = "#C50035",
                Accent = "#A3F2CE", AccentFocus = "#82C1A4", AccentContent = "#007853",
                Neutral = "#61738D", NeutralFocus = "#4D5C70", NeutralContent = "#DFE5ED",
                Base100 = "#FFFFFF", Base200 = "#F9FAFB", Base300 = "#C7C7C7", BaseContent = "#161616",
                Info = "#51E8FB", InfoContent = "#000000", Success = "#7AF1A7", SuccessContent = "#000000",
                Warning = "#FFB667", WarningContent = "#000000", Error = "#FF9FA0", ErrorContent = "#000000"
            },
            ["Retro"] = new ThemePalette
            {
                Primary = "#FF9FA0", PrimaryFocus = "#CC7F80", PrimaryContent = "#801518",
                Secondary = "#B7F6CD", SecondaryFocus = "#92C4A4", SecondaryContent = "#00642E",
                Accent = "#D08700", AccentFocus = "#A66C00", AccentContent = "#793205",
                Neutral = "#56524C", NeutralFocus = "#44413C", NeutralContent = "#D4D0CE",
                Base100 = "#ECE3CA", Base200 = "#E4D8B4", Base300 = "#B7B09B", BaseContent = "#793205",
                Info = "#0082CE", InfoContent = "#FFFFFF", Success = "#00776F", SuccessContent = "#FFFFFF",
                Warning = "#F34700", WarningContent = "#FFFFFF", Error = "#FF6266", ErrorContent = "#000000"
            },
            ["Silk"] = new ThemePalette
            {
                Primary = "#1C1C29", PrimaryFocus = "#161620", PrimaryContent = "#E1FF00",
                Secondary = "#1C1C29", SecondaryFocus = "#161620", SecondaryContent = "#FF7700",
                Accent = "#1C1C29", AccentFocus = "#161620", AccentContent = "#00FFF8",
                Neutral = "#161616", NeutralFocus = "#111111", NeutralContent = "#C2BDB9",
                Base100 = "#F7F5F3", Base200 = "#F3EDE9", Base300 = "#C0BEBD", BaseContent = "#4B4743",
                Info = "#78C8FF", InfoContent = "#000000", Success = "#AFD89E", SuccessContent = "#000000",
                Warning = "#EFC375", WarningContent = "#000000", Error = "#FF7878", ErrorContent = "#000000"
            },
            ["Smooth"] = new ThemePalette
            {
                Primary = "#FB216F", PrimaryFocus = "#D71152", PrimaryContent = "#FFFFFF",
                Secondary = "#D71152", SecondaryFocus = "#B00E45", SecondaryContent = "#FFFFFF",
                Accent = "#FF4387", AccentFocus = "#FB216F", AccentContent = "#FFFFFF",
                Neutral = "#080808", NeutralFocus = "#060606", NeutralContent = "#D7D7D7",
                Base100 = "#020202", Base200 = "#040404", Base300 = "#0C0C0C", BaseContent = "#D7D7D7",
                Info = "#38BDF8", InfoContent = "#0C4A6E", Success = "#34D399", SuccessContent = "#064E3B",
                Warning = "#FBBF24", WarningContent = "#78350F", Error = "#F87171", ErrorContent = "#7F1D1D"
            },
            ["Sunset"] = new ThemePalette
            {
                Primary = "#FF865B", PrimaryFocus = "#CC6B48", PrimaryContent = "#160603",
                Secondary = "#FD6F9C", SecondaryFocus = "#CA587C", SecondaryContent = "#160409",
                Accent = "#B387FA", AccentFocus = "#8F6CC8", AccentContent = "#0C0615",
                Neutral = "#1B262C", NeutralFocus = "#151E23", NeutralContent = "#94A0A9",
                Base100 = "#121C22", Base200 = "#0E171E", Base300 = "#6D7478", BaseContent = "#9FB9D0",
                Info = "#89E0EB", InfoContent = "#000000", Success = "#ADDFAD", SuccessContent = "#000000",
                Warning = "#F1C892", WarningContent = "#000000", Error = "#FFBBBD", ErrorContent = "#000000"
            },
            ["Synthwave"] = new ThemePalette
            {
                Primary = "#F861B4", PrimaryFocus = "#C64D90", PrimaryContent = "#500323",
                Secondary = "#71D1FE", SecondaryFocus = "#5AA7CB", SecondaryContent = "#042E49",
                Accent = "#FF8904", AccentFocus = "#CC6D03", AccentContent = "#421104",
                Neutral = "#422AD5", NeutralFocus = "#3421AA", NeutralContent = "#C6D2FF",
                Base100 = "#09002F", Base200 = "#120B3D", Base300 = "#1C184B", BaseContent = "#A1B1FF",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D3BB", SuccessContent = "#000000",
                Warning = "#FEDE1C", WarningContent = "#000000", Error = "#EC8C78", ErrorContent = "#000000"
            },
            ["Valentine"] = new ThemePalette
            {
                Primary = "#F43098", PrimaryFocus = "#C32679", PrimaryContent = "#FFFFFF",
                Secondary = "#AB44FF", SecondaryFocus = "#8836CC", SecondaryContent = "#F8F3FD",
                Accent = "#71D1FE", AccentFocus = "#5AA7CB", AccentContent = "#014A70",
                Neutral = "#830C41", NeutralFocus = "#680934", NeutralContent = "#F9CBE5",
                Base100 = "#FCF2F8", Base200 = "#F9E4F0", Base300 = "#C4BCC1", BaseContent = "#C5005A",
                Info = "#51E8FB", InfoContent = "#000000", Success = "#5CE8B3", SuccessContent = "#000000",
                Warning = "#FF8904", WarningContent = "#000000", Error = "#F82834", ErrorContent = "#FFFFFF"
            },
            ["Winter"] = new ThemePalette
            {
                Primary = "#0069FF", PrimaryFocus = "#0054CC", PrimaryContent = "#CEE4FF",
                Secondary = "#463AA2", SecondaryFocus = "#382E81", SecondaryContent = "#D5D7EE",
                Accent = "#C148AC", AccentFocus = "#9A3989", AccentContent = "#0E020B",
                Neutral = "#021431", NeutralFocus = "#011027", NeutralContent = "#C5CBD2",
                Base100 = "#FFFFFF", Base200 = "#F2F7FE", Base300 = "#C7C7C7", BaseContent = "#394E6A",
                Info = "#94E7FB", InfoContent = "#000000", Success = "#81CFD1", SuccessContent = "#000000",
                Warning = "#EFD7BC", WarningContent = "#000000", Error = "#E58B8B", ErrorContent = "#000000"
            },
            ["Wireframe"] = new ThemePalette
            {
                Primary = "#D4D4D4", PrimaryFocus = "#A9A9A9", PrimaryContent = "#242424",
                Secondary = "#D4D4D4", SecondaryFocus = "#A9A9A9", SecondaryContent = "#242424",
                Accent = "#D4D4D4", AccentFocus = "#A9A9A9", AccentContent = "#242424",
                Neutral = "#D4D4D4", NeutralFocus = "#A9A9A9", NeutralContent = "#242424",
                Base100 = "#FFFFFF", Base200 = "#F5F5F5", Base300 = "#C7C7C7", BaseContent = "#161616",
                Info = "#005889", InfoContent = "#FFFFFF", Success = "#006044", SuccessContent = "#FFFFFF",
                Warning = "#963B00", WarningContent = "#FFFFFF", Error = "#9D0410", ErrorContent = "#FFFFFF"
            },
            ["HappyHues01"] = new ThemePalette
            {
                Primary = "#4FC4CF", PrimaryFocus = "#3F9CA5", PrimaryContent = "#000000",
                Secondary = "#994FF3", SecondaryFocus = "#7A3FC2", SecondaryContent = "#FFFFFF",
                Accent = "#FBDD74", AccentFocus = "#C8B05C", AccentContent = "#000000",
                Neutral = "#181818", NeutralFocus = "#131313", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFE", Base200 = "#F6EFEF", Base300 = "#CAC4C4", BaseContent = "#181818",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues02"] = new ThemePalette
            {
                Primary = "#00EBC7", PrimaryFocus = "#00BC9F", PrimaryContent = "#000000",
                Secondary = "#FF5470", SecondaryFocus = "#CC4359", SecondaryContent = "#000000",
                Accent = "#FDE24F", AccentFocus = "#CAB43F", AccentContent = "#000000",
                Neutral = "#00214D", NeutralFocus = "#001A3D", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFE", Base200 = "#F2F4F6", Base300 = "#C7C8CA", BaseContent = "#00214D",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues03"] = new ThemePalette
            {
                Primary = "#3DA9FC", PrimaryFocus = "#3087C9", PrimaryContent = "#000000",
                Secondary = "#90B4CE", SecondaryFocus = "#7390A4", SecondaryContent = "#000000",
                Accent = "#EF4565", AccentFocus = "#BF3750", AccentContent = "#FFFFFF",
                Neutral = "#094067", NeutralFocus = "#073352", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFE", Base200 = "#D8EEFE", Base300 = "#B1C3D0", BaseContent = "#094067",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues04"] = new ThemePalette
            {
                Primary = "#7F5AF0", PrimaryFocus = "#6548C0", PrimaryContent = "#FFFFFF",
                Secondary = "#72757E", SecondaryFocus = "#5B5D64", SecondaryContent = "#FFFFFF",
                Accent = "#2CB67D", AccentFocus = "#239164", AccentContent = "#000000",
                Neutral = "#010101", NeutralFocus = "#000000", NeutralContent = "#FFFFFF",
                Base100 = "#16161A", Base200 = "#242629", Base300 = "#7B7C7E", BaseContent = "#FFFFFE",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues05"] = new ThemePalette
            {
                Primary = "#FAAE2B", PrimaryFocus = "#C88B22", PrimaryContent = "#000000",
                Secondary = "#FFA8BA", SecondaryFocus = "#CC8694", SecondaryContent = "#000000",
                Accent = "#FA5246", AccentFocus = "#C84138", AccentContent = "#000000",
                Neutral = "#00332C", NeutralFocus = "#002823", NeutralContent = "#FFFFFF",
                Base100 = "#F2F7F5", Base200 = "#E5EAE8", Base300 = "#BCC0BE", BaseContent = "#00473E",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues06"] = new ThemePalette
            {
                Primary = "#6246EA", PrimaryFocus = "#4E38BB", PrimaryContent = "#FFFFFF",
                Secondary = "#D1D1E9", SecondaryFocus = "#A7A7BA", SecondaryContent = "#000000",
                Accent = "#E45858", AccentFocus = "#B64646", AccentContent = "#000000",
                Neutral = "#2B2C34", NeutralFocus = "#222329", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFE", Base200 = "#FFFFFE", Base300 = "#D1D1D0", BaseContent = "#2B2C34",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues07"] = new ThemePalette
            {
                Primary = "#FEC7D7", PrimaryFocus = "#CB9FAC", PrimaryContent = "#000000",
                Secondary = "#D9D4E7", SecondaryFocus = "#ADA9B8", SecondaryContent = "#000000",
                Accent = "#A786DF", AccentFocus = "#856BB2", AccentContent = "#000000",
                Neutral = "#0E172C", NeutralFocus = "#0B1223", NeutralContent = "#FFFFFF",
                Base100 = "#FEC7D7", Base200 = "#FFFFFE", Base300 = "#D1D1D0", BaseContent = "#0E172C",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues08"] = new ThemePalette
            {
                Primary = "#078080", PrimaryFocus = "#056666", PrimaryContent = "#FFFFFF",
                Secondary = "#F45D48", SecondaryFocus = "#C34A39", SecondaryContent = "#000000",
                Accent = "#F8F5F2", AccentFocus = "#C6C4C1", AccentContent = "#000000",
                Neutral = "#232323", NeutralFocus = "#1C1C1C", NeutralContent = "#FFFFFF",
                Base100 = "#F8F5F2", Base200 = "#FFFFFE", Base300 = "#D1D1D0", BaseContent = "#232323",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues09"] = new ThemePalette
            {
                Primary = "#FF8E3C", PrimaryFocus = "#CC7130", PrimaryContent = "#000000",
                Secondary = "#FFFFFE", SecondaryFocus = "#CCCCCB", SecondaryContent = "#000000",
                Accent = "#D9376E", AccentFocus = "#AD2C58", AccentContent = "#FFFFFF",
                Neutral = "#0D0D0D", NeutralFocus = "#0A0A0A", NeutralContent = "#FFFFFF",
                Base100 = "#EFF0F3", Base200 = "#FFFFFE", Base300 = "#D1D1D0", BaseContent = "#0D0D0D",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues10"] = new ThemePalette
            {
                Primary = "#F9BC60", PrimaryFocus = "#C7964C", PrimaryContent = "#000000",
                Secondary = "#ABD1C6", SecondaryFocus = "#88A79E", SecondaryContent = "#000000",
                Accent = "#E16162", AccentFocus = "#B44D4E", AccentContent = "#000000",
                Neutral = "#001E1D", NeutralFocus = "#001817", NeutralContent = "#FFFFFF",
                Base100 = "#004643", Base200 = "#0C4F4C", Base300 = "#6D9593", BaseContent = "#FFFFFE",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues11"] = new ThemePalette
            {
                Primary = "#8C7851", PrimaryFocus = "#706040", PrimaryContent = "#FFFFFF",
                Secondary = "#EADDCF", SecondaryFocus = "#BBB0A5", SecondaryContent = "#000000",
                Accent = "#F25042", AccentFocus = "#C14034", AccentContent = "#FFFFFF",
                Neutral = "#020826", NeutralFocus = "#01061E", NeutralContent = "#FFFFFF",
                Base100 = "#F9F4EF", Base200 = "#FFFFFE", Base300 = "#D1D1D0", BaseContent = "#020826",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues12"] = new ThemePalette
            {
                Primary = "#EEBBC3", PrimaryFocus = "#BE959C", PrimaryContent = "#000000",
                Secondary = "#FFFFFE", SecondaryFocus = "#CCCCCB", SecondaryContent = "#000000",
                Accent = "#EEBBC3", AccentFocus = "#BE959C", AccentContent = "#000000",
                Neutral = "#121629", NeutralFocus = "#0E1120", NeutralContent = "#FFFFFF",
                Base100 = "#232946", Base200 = "#2E334F", Base300 = "#818495", BaseContent = "#FFFFFE",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues13"] = new ThemePalette
            {
                Primary = "#FF8906", PrimaryFocus = "#CC6D04", PrimaryContent = "#000000",
                Secondary = "#F25F4C", SecondaryFocus = "#C14C3C", SecondaryContent = "#000000",
                Accent = "#E53170", AccentFocus = "#B72759", AccentContent = "#FFFFFF",
                Neutral = "#000000", NeutralFocus = "#000000", NeutralContent = "#FFFFFF",
                Base100 = "#0F0E17", Base200 = "#1B1A22", Base300 = "#76757A", BaseContent = "#FFFFFE",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues14"] = new ThemePalette
            {
                Primary = "#FFD803", PrimaryFocus = "#CCAC02", PrimaryContent = "#000000",
                Secondary = "#E3F6F5", SecondaryFocus = "#B5C4C4", SecondaryContent = "#000000",
                Accent = "#BAE8E8", AccentFocus = "#94B9B9", AccentContent = "#000000",
                Neutral = "#272343", NeutralFocus = "#1F1C35", NeutralContent = "#FFFFFF",
                Base100 = "#FFFFFE", Base200 = "#E3F6F5", Base300 = "#BACAC9", BaseContent = "#272343",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues15"] = new ThemePalette
            {
                Primary = "#FF8BA7", PrimaryFocus = "#CC6F85", PrimaryContent = "#000000",
                Secondary = "#FFC6C7", SecondaryFocus = "#CC9E9F", SecondaryContent = "#000000",
                Accent = "#C3F0CA", AccentFocus = "#9CC0A1", AccentContent = "#000000",
                Neutral = "#33272A", NeutralFocus = "#281F21", NeutralContent = "#FFFFFF",
                Base100 = "#FAEEE7", Base200 = "#FFFFFE", Base300 = "#D1D1D0", BaseContent = "#33272A",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues16"] = new ThemePalette
            {
                Primary = "#E78FB3", PrimaryFocus = "#B8728F", PrimaryContent = "#000000",
                Secondary = "#FFC0AD", SecondaryFocus = "#CC998A", SecondaryContent = "#000000",
                Accent = "#9656A1", AccentFocus = "#784480", AccentContent = "#FFFFFF",
                Neutral = "#140D0B", NeutralFocus = "#100A08", NeutralContent = "#FFFFFF",
                Base100 = "#55423D", Base200 = "#271C19", Base300 = "#7D7674", BaseContent = "#FFFFFE",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            },
            ["HappyHues17"] = new ThemePalette
            {
                Primary = "#FEF6E4", PrimaryFocus = "#CBC4B6", PrimaryContent = "#000000",
                Secondary = "#8BD3DD", SecondaryFocus = "#6FA8B0", SecondaryContent = "#000000",
                Accent = "#F582AE", AccentFocus = "#C4688B", AccentContent = "#000000",
                Neutral = "#001858", NeutralFocus = "#001346", NeutralContent = "#FFFFFF",
                Base100 = "#FEF6E4", Base200 = "#F3D2C1", Base300 = "#C7AC9E", BaseContent = "#001858",
                Info = "#00BAFE", InfoContent = "#000000", Success = "#00D390", SuccessContent = "#000000",
                Warning = "#FCB700", WarningContent = "#000000", Error = "#FF627D", ErrorContent = "#000000"
            }
        };

        /// <summary>
        /// Creates a ResourceDictionary for the specified theme.
        /// </summary>
        public static ResourceDictionary Create(string themeName)
        {
            if (!Palettes.TryGetValue(themeName, out var palette))
            {
                palette = Palettes["Dark"]; // Fallback to Dark
            }

            return CreateFromPalette(palette);
        }

        /// <summary>
        /// Creates the Light theme palette.
        /// </summary>
        public static ResourceDictionary CreateLight() => Create("Light");

        /// <summary>
        /// Creates the Dark theme palette.
        /// </summary>
        public static ResourceDictionary CreateDark() => Create("Dark");

        /// <summary>
        /// Creates a ResourceDictionary from a ThemePalette instance.
        /// Used by ProductPaletteFactory to convert product palettes.
        /// </summary>
        public static ResourceDictionary Create(ThemePalette palette) => CreateFromPalette(palette);

        private static ResourceDictionary CreateFromPalette(ThemePalette p)
        {
            var dict = new ResourceDictionary();

            AddColorAndBrush(dict, "DaisyPrimary", FloweryColorHelpers.ColorFromHex(p.Primary));
            AddColorAndBrush(dict, "DaisyPrimaryFocus", FloweryColorHelpers.ColorFromHex(p.PrimaryFocus));
            AddColorAndBrush(dict, "DaisyPrimaryContent", FloweryColorHelpers.ColorFromHex(p.PrimaryContent));

            AddColorAndBrush(dict, "DaisySecondary", FloweryColorHelpers.ColorFromHex(p.Secondary));
            AddColorAndBrush(dict, "DaisySecondaryFocus", FloweryColorHelpers.ColorFromHex(p.SecondaryFocus));
            AddColorAndBrush(dict, "DaisySecondaryContent", FloweryColorHelpers.ColorFromHex(p.SecondaryContent));

            AddColorAndBrush(dict, "DaisyAccent", FloweryColorHelpers.ColorFromHex(p.Accent));
            AddColorAndBrush(dict, "DaisyAccentFocus", FloweryColorHelpers.ColorFromHex(p.AccentFocus));
            AddColorAndBrush(dict, "DaisyAccentContent", FloweryColorHelpers.ColorFromHex(p.AccentContent));

            AddColorAndBrush(dict, "DaisyNeutral", FloweryColorHelpers.ColorFromHex(p.Neutral));
            AddColorAndBrush(dict, "DaisyNeutralFocus", FloweryColorHelpers.ColorFromHex(p.NeutralFocus));
            AddColorAndBrush(dict, "DaisyNeutralContent", FloweryColorHelpers.ColorFromHex(p.NeutralContent));

            AddColorAndBrush(dict, "DaisyBase100", FloweryColorHelpers.ColorFromHex(p.Base100));
            AddColorAndBrush(dict, "DaisyBase200", FloweryColorHelpers.ColorFromHex(p.Base200));
            AddColorAndBrush(dict, "DaisyBase300", FloweryColorHelpers.ColorFromHex(p.Base300));
            AddColorAndBrush(dict, "DaisyBaseContent", FloweryColorHelpers.ColorFromHex(p.BaseContent));

            AddColorAndBrush(dict, "DaisyInfo", FloweryColorHelpers.ColorFromHex(p.Info));
            AddColorAndBrush(dict, "DaisyInfoContent", FloweryColorHelpers.ColorFromHex(p.InfoContent));

            AddColorAndBrush(dict, "DaisySuccess", FloweryColorHelpers.ColorFromHex(p.Success));
            AddColorAndBrush(dict, "DaisySuccessContent", FloweryColorHelpers.ColorFromHex(p.SuccessContent));

            AddColorAndBrush(dict, "DaisyWarning", FloweryColorHelpers.ColorFromHex(p.Warning));
            AddColorAndBrush(dict, "DaisyWarningContent", FloweryColorHelpers.ColorFromHex(p.WarningContent));

            AddColorAndBrush(dict, "DaisyError", FloweryColorHelpers.ColorFromHex(p.Error));
            AddColorAndBrush(dict, "DaisyErrorContent", FloweryColorHelpers.ColorFromHex(p.ErrorContent));

            return dict;
        }

        private static void AddColorAndBrush(ResourceDictionary dict, string keyPrefix, Color color)
        {
            dict[keyPrefix + "Color"] = color;
            dict[keyPrefix + "Brush"] = new SolidColorBrush(color);
        }
    }
}
