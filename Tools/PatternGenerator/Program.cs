// Pattern SVG Generator
// Run this once to generate pattern tile SVG files
// This tool references Flowery.Uno to use the same pattern definitions

using Flowery.Tools;

string outputDir = args.Length > 0 ? args[0] : System.IO.Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);

Console.WriteLine($"Generating pattern SVG tiles to: {outputDir}");
Console.WriteLine();

PatternSvgExporter.ExportAllPatterns(outputDir);
