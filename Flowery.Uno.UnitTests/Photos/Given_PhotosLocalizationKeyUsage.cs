using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flowery.Uno.Photos.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    /// <summary>Every localization key the app source asks for must exist in en.json, so no screen shows a raw key.</summary>
    [TestClass]
    public class Given_PhotosLocalizationKeyUsage
    {
        private static readonly Regex CodeKey = new("\"(Photos_[A-Za-z0-9_]+)\"", RegexOptions.Compiled);
        private static readonly Regex XamlKey = new(@"\[(Photos_[A-Za-z0-9_]+)\]", RegexOptions.Compiled);

        private static string FindAppFolder()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Flowery.Uno.Photos");
                if (File.Exists(Path.Combine(candidate, "Flowery.Uno.Photos.csproj")))
                    return candidate;
                dir = dir.Parent;
            }
            Assert.Inconclusive("The Flowery.Uno.Photos source folder was not found next to the test output.");
            return string.Empty;
        }

        private static HashSet<string> EnglishKeys()
        {
            var assembly = typeof(PhotosLocalization).Assembly;
            var name = assembly.GetName().Name + ".Localization.en.json";
            using var stream = assembly.GetManifestResourceStream(name)!;
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!.Keys.ToHashSet();
        }

        private static IEnumerable<(string File, string Key)> UsedKeys(string folder)
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(folder, file);
                if (relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    continue;
                }

                var regex = file.EndsWith(".cs", StringComparison.Ordinal) ? CodeKey :
                    file.EndsWith(".xaml", StringComparison.Ordinal) ? XamlKey : null;
                if (regex == null)
                    continue;

                foreach (Match match in regex.Matches(File.ReadAllText(file)))
                    yield return (relative, match.Groups[1].Value);
            }
        }

        [TestMethod]
        public void When_SourceAsksForAKey_EnglishHasIt()
        {
            var english = EnglishKeys();

            var missing = UsedKeys(FindAppFolder())
                .Where(u => !english.Contains(u.Key))
                .Select(u => $"{u.Key} ({u.File})")
                .Distinct()
                .ToList();

            Assert.AreEqual(0, missing.Count, "Keys used in the source but missing in en.json: " + string.Join(", ", missing));
        }

        [TestMethod]
        public void When_ScanningTheSource_KeysAreFound()
        {
            // Guards the scanner itself: a pattern that silently matches nothing would make the test above pass for nothing.
            var used = UsedKeys(FindAppFolder()).Select(u => u.Key).Distinct().Count();

            Assert.IsTrue(used > 100, $"Only {used} keys were found in the source.");
        }

        [TestMethod]
        public void When_FilterPresetsAreListed_EachHasAnEnglishName()
        {
            var english = EnglishKeys();

            foreach (var preset in Flowery.Uno.Photos.Editing.FilterPresets.All)
                Assert.IsTrue(english.Contains(preset.LocalizationKey), $"missing {preset.LocalizationKey}");
        }
    }
}
