using System.Reflection;
using System.Text.Json;
using Flowery.Localization;
using Flowery.Uno.Photos.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_PhotosLocalizationFiles
    {
        private static Dictionary<string, Dictionary<string, string>> LoadAll()
        {
            var assembly = typeof(PhotosLocalization).Assembly;
            var prefix = assembly.GetName().Name + ".Localization.";
            Dictionary<string, Dictionary<string, string>> files = [];
            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal)))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                files[name[prefix.Length..^".json".Length]] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
            }
            return files;
        }

        [TestMethod]
        public void When_Loading_EveryFloweryLanguageHasAFile()
        {
            var files = LoadAll();

            foreach (var language in FloweryLocalization.SupportedLanguages)
            {
                Assert.IsTrue(files.ContainsKey(language), $"missing {language}.json");
            }
        }

        [TestMethod]
        public void When_Comparing_AllFilesHaveTheSameKeysAsEnglish()
        {
            var files = LoadAll();
            var english = files["en"].Keys.ToHashSet();

            foreach (var (language, entries) in files)
            {
                CollectionAssert.AreEquivalent(english.ToList(), entries.Keys.ToList(), $"{language}.json keys differ from en.json");
            }
        }

        [TestMethod]
        public void When_Comparing_PlaceholdersMatchEnglish()
        {
            var files = LoadAll();

            foreach (var (language, entries) in files.Where(f => f.Key != "en"))
            {
                foreach (var (key, value) in entries)
                {
                    CollectionAssert.AreEquivalent(Placeholders(files["en"][key]), Placeholders(value), $"{language}.json: {key}");
                }
            }
        }

        private static List<string> Placeholders(string text) =>
            System.Text.RegularExpressions.Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).ToList();

        /// <summary>
        /// A key used in code (<c>"Photos_…"</c>) or XAML (<c>{Binding [Photos_…]}</c>) that no file has would show as the
        /// key itself. The test reads the app's sources from the repository the test runs in.
        /// </summary>
        [TestMethod]
        public void When_ScanningTheSources_EveryUsedKeyExists()
        {
            var sources = FindPhotosSources();
            var english = LoadAll()["en"];
            var used = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var file in SourceFiles(sources, "*.cs"))
                used.UnionWith(System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), "\"(Photos_[A-Za-z0-9_]+)\"").Select(m => m.Groups[1].Value));
            foreach (var file in SourceFiles(sources, "*.xaml"))
                used.UnionWith(System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), @"\[(Photos_[A-Za-z0-9_]+)\]").Select(m => m.Groups[1].Value));

            Assert.IsTrue(used.Count > 100, $"only {used.Count} keys found in {sources}");
            var missing = used.Where(k => !english.ContainsKey(k)).ToList();
            Assert.AreEqual(0, missing.Count, "used but missing in en.json: " + string.Join(", ", missing));
        }

        /// <summary>The app's own files, without the build output in bin and obj.</summary>
        private static IEnumerable<string> SourceFiles(string root, string pattern) =>
            Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories).Where(f =>
            {
                var relative = Path.GetRelativePath(root, f).Replace('\\', '/');
                return !relative.StartsWith("bin/", StringComparison.Ordinal) && !relative.StartsWith("obj/", StringComparison.Ordinal);
            });

        private static string FindPhotosSources()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "Flowery.Uno.Photos");
                if (File.Exists(Path.Combine(candidate, "Flowery.Uno.Photos.csproj")))
                    return candidate;
            }
            Assert.Inconclusive("The Flowery.Uno.Photos sources are not above the test output.");
            return string.Empty;
        }

        [TestMethod]
        public void When_ResolvingAKey_FallsBackToEnglishThenToTheKey()
        {
            Assert.AreNotEqual(string.Empty, PhotosLocalization.GetString(LoadAll()["en"].Keys.First()));
            Assert.AreEqual("no-such-key", PhotosLocalization.GetString("no-such-key"));
        }
    }
}
