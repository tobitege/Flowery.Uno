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

        [TestMethod]
        public void When_ResolvingAKey_FallsBackToEnglishThenToTheKey()
        {
            Assert.AreNotEqual(string.Empty, PhotosLocalization.GetString(LoadAll()["en"].Keys.First()));
            Assert.AreEqual("no-such-key", PhotosLocalization.GetString("no-such-key"));
        }
    }
}
