// Icon SVG Generator
// Run this to generate icon SVG files from DaisyIcons.xaml
// This tool references Flowery.Uno to use the icon exporter
//
// Usage: dotnet run <output-folder> <hex-color>
// Example: dotnet run ./icons #FF5733

using Flowery.Tools;

if (args.Length < 2)
{
    Console.WriteLine("Icon SVG Generator");
    Console.WriteLine("==================");
    Console.WriteLine();
    Console.WriteLine("Exports DaisyIcons.xaml icons to individual SVG files.");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run <output-folder> <hex-color>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  output-folder   Directory where SVG files will be created");
    Console.WriteLine("  hex-color       Fill color in hex format (#RGB, #RRGGBB, or #AARRGGBB)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run ./icons #000000       Export with black color");
    Console.WriteLine("  dotnet run ./icons #FFFFFF       Export with white color");
    Console.WriteLine("  dotnet run ./icons #FF5733       Export with custom color");
    Console.WriteLine("  dotnet run ./icons FF5733        # prefix is optional");
    Console.WriteLine();
    return 1;
}

string outputDir = args[0];
string hexColor = args[1];

Directory.CreateDirectory(outputDir);

Console.WriteLine($"Generating icon SVG files to: {outputDir}");
Console.WriteLine($"Using color: {hexColor}");
Console.WriteLine();

try
{
    IconSvgExporter.ExportAllIcons(outputDir, hexColor);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}
