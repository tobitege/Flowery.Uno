using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IOPath = System.IO.Path;

namespace Flowery.Tools
{
    /// <summary>
    /// Exports DaisyIcons to SVG files with a user-specified color.
    /// Parses DaisyIcons.xaml and generates individual SVG files for each icon.
    /// </summary>
    /// <remarks>
    /// CLI Usage: dotnet run -- --export-icons &lt;output-folder&gt; &lt;hex-color&gt;
    /// Example:   dotnet run -- --export-icons ./icons #FF5733
    /// </remarks>
    public static class IconSvgExporter
    {
        private const int DefaultViewBoxSize = 24;
        
        /// <summary>
        /// Exports all icons from DaisyIcons.xaml to SVG files in the specified directory.
        /// </summary>
        /// <param name="outputDirectory">Target directory for SVG files</param>
        /// <param name="hexColor">Fill color in hex format (e.g., "#FF5733" or "FF5733")</param>
        public static void ExportAllIcons(string outputDirectory, string hexColor)
        {
            // Normalize the hex color (ensure it starts with #)
            var normalizedColor = NormalizeHexColor(hexColor);
            
            // Use invariant culture for SVG decimal points (period, not comma)
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            
            try
            {
                Directory.CreateDirectory(outputDirectory);
                
                var icons = ParseDaisyIconsXaml();
                Console.WriteLine($"Found {icons.Count} icons to export.");
                
                foreach (var icon in icons)
                {
                    var svg = GenerateIconSvg(icon.PathData, normalizedColor);
                    var fileName = IconKeyToFileName(icon.Key);
                    var path = IOPath.Combine(outputDirectory, $"{fileName}.svg");
                    File.WriteAllText(path, svg);
                    Console.WriteLine($"  Created: {fileName}.svg");
                }
                
                Console.WriteLine($"\nExported {icons.Count} icon SVGs to: {outputDirectory}");
                Console.WriteLine($"Color: {normalizedColor}");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
        
        /// <summary>
        /// Parses DaisyIcons.xaml and extracts all icon path data.
        /// </summary>
        private static List<(string Key, string PathData)> ParseDaisyIconsXaml()
        {
            var icons = new List<(string Key, string PathData)>();
            
            // Get the path to DaisyIcons.xaml relative to the assembly location
            var assemblyDir = IOPath.GetDirectoryName(typeof(IconSvgExporter).Assembly.Location);
            
            // Try multiple potential paths for the XAML file
            string? xamlPath = null;
            var potentialPaths = new[]
            {
                // From Tools/IconGenerator - go up to repo root, then into Flowery.Uno
                IOPath.Combine(Environment.CurrentDirectory, "..", "..", "Flowery.Uno", "Themes", "DaisyIcons.xaml"),
                // From bin/Debug/net9.0 output folder
                IOPath.Combine(assemblyDir ?? ".", "..", "..", "..", "..", "..", "Flowery.Uno", "Themes", "DaisyIcons.xaml"),
                // Direct sibling (if running from solution root)
                IOPath.Combine(Environment.CurrentDirectory, "Flowery.Uno", "Themes", "DaisyIcons.xaml"),
                // Other potential locations
                IOPath.Combine(assemblyDir ?? ".", "..", "..", "..", "Themes", "DaisyIcons.xaml"),
                IOPath.Combine(assemblyDir ?? ".", "Themes", "DaisyIcons.xaml"),
            };
            
            foreach (var path in potentialPaths)
            {
                var fullPath = IOPath.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    xamlPath = fullPath;
                    break;
                }
            }
            
            if (xamlPath == null)
            {
                throw new FileNotFoundException(
                    "Could not find DaisyIcons.xaml. Searched paths:\n" + 
                    string.Join("\n", potentialPaths.Select(p => $"  - {IOPath.GetFullPath(p)}")));
            }
            
            Console.WriteLine($"Reading icons from: {xamlPath}");
            
            var xamlContent = File.ReadAllText(xamlPath);
            var doc = XDocument.Parse(xamlContent);
            
            // XAML namespaces
            XNamespace defaultNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            XNamespace xNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            
            // Find all x:String elements with x:Key attributes
            foreach (var element in doc.Descendants(xNs + "String"))
            {
                var keyAttr = element.Attribute(xNs + "Key");
                if (keyAttr != null && !string.IsNullOrEmpty(element.Value))
                {
                    icons.Add((keyAttr.Value, element.Value));
                }
            }
            
            return icons;
        }
        
        /// <summary>
        /// Generates an SVG string for an icon with the specified path data and fill color.
        /// </summary>
        private static string GenerateIconSvg(string pathData, string fillColor)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {DefaultViewBoxSize} {DefaultViewBoxSize}"" width=""{DefaultViewBoxSize}"" height=""{DefaultViewBoxSize}"">
  <path d=""{pathData}"" fill=""{fillColor}""/>
</svg>";
        }
        
        /// <summary>
        /// Converts an icon key to a file-friendly name.
        /// </summary>
        private static string IconKeyToFileName(string key)
        {
            // Remove common prefixes
            var name = key;
            if (name.StartsWith("DaisyIcon"))
                name = name.Substring("DaisyIcon".Length);
            else if (name.StartsWith("ModifierIcon"))
                name = name.Substring("ModifierIcon".Length);
            
            // Convert PascalCase to snake_case
            name = PascalToSnakeCase(name);
            
            return name;
        }
        
        /// <summary>
        /// Converts PascalCase to snake_case.
        /// </summary>
        private static string PascalToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            // Insert underscore before uppercase letters, then lowercase everything
            var result = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2");
            return result.ToLowerInvariant();
        }
        
        /// <summary>
        /// Normalizes a hex color string to include the # prefix.
        /// </summary>
        private static string NormalizeHexColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                throw new ArgumentException("Hex color cannot be empty.", nameof(hexColor));
            
            var color = hexColor.Trim();
            
            // Add # if missing
            if (!color.StartsWith("#"))
                color = "#" + color;
            
            // Validate the hex color format
            if (!Regex.IsMatch(color, @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$"))
            {
                throw new ArgumentException(
                    $"Invalid hex color format: '{hexColor}'. Expected formats: #RGB, #RRGGBB, or #AARRGGBB",
                    nameof(hexColor));
            }
            
            return color;
        }
        
        /// <summary>
        /// Prints usage information for the CLI.
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine(@"
Icon SVG Exporter - Exports DaisyIcons to SVG files

Usage:
  --export-icons <output-folder> <hex-color>

Arguments:
  output-folder   Directory where SVG files will be created
  hex-color       Fill color in hex format (#RGB, #RRGGBB, or #AARRGGBB)

Examples:
  --export-icons ./icons #000000       Export with black color
  --export-icons ./icons #FFFFFF       Export with white color
  --export-icons ./icons #FF5733       Export with custom color
  --export-icons ./icons FF5733        # prefix is optional
");
        }
    }
}
