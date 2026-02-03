using System;
using System.Globalization;
using System.IO;
using Flowery.Controls;
using Flowery.Helpers;

namespace Flowery.Tools
{
    /// <summary>
    /// Exports pattern tiles to SVG files using FloweryPatternTileGenerator.
    /// Generates both black and white color variants for theme compatibility.
    /// </summary>
    public static class PatternSvgExporter
    {
        /// <summary>
        /// Generates all pattern SVG files in black and white variants to the specified output directory.
        /// </summary>
        public static void ExportAllPatterns(string outputDirectory)
        {
            // Use invariant culture for SVG decimal points (period, not comma)
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            
            try
            {
                // Generate black variants (for light backgrounds)
                var blackDir = System.IO.Path.Combine(outputDirectory, "black");
                Directory.CreateDirectory(blackDir);
                ExportPatternsWithColor(blackDir, "black");
                
                // Generate white variants (for dark backgrounds)
                var whiteDir = System.IO.Path.Combine(outputDirectory, "white");
                Directory.CreateDirectory(whiteDir);
                ExportPatternsWithColor(whiteDir, "white");
                
                Console.WriteLine($"Exported pattern SVGs to: {outputDirectory}");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
        
        private static void ExportPatternsWithColor(string directory, string fillColor)
        {
            Console.WriteLine($"Generating {fillColor} variants...");
            
            foreach (DaisyCardPattern pattern in Enum.GetValues<DaisyCardPattern>())
            {
                if (pattern == DaisyCardPattern.None) continue;
                
                var svg = FloweryPatternTileGenerator.GeneratePatternSvg(pattern, fillColor);
                if (svg != null)
                {
                    var fileName = PatternToFileName(pattern);
                    var path = System.IO.Path.Combine(directory, $"{fileName}.svg");
                    File.WriteAllText(path, svg);
                    Console.WriteLine($"  Created: {fillColor}/{fileName}.svg");
                }
            }
        }
        
        private static string PatternToFileName(DaisyCardPattern pattern)
        {
            return pattern switch
            {
                DaisyCardPattern.CarbonFiber => "carbon_fiber",
                DaisyCardPattern.DiamondPlate => "diamond_plate",
                _ => pattern.ToString().ToLowerInvariant()
            };
        }
    }
}
