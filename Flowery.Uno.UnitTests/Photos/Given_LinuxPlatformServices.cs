using Flowery.Uno.Photos.Services.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_LinuxPlatformServices
    {
        private static FakeCommandRunner GnomeRunner() => new FakeCommandRunner("gio", "dbus-send", "xdg-open", "gsettings")
            .Returns("gsettings list-keys org.gnome.desktop.background", 0, "picture-options\npicture-uri\npicture-uri-dark\n")
            .Returns("gsettings list-keys org.gnome.desktop.interface", 0, "accent-color\ncolor-scheme\n");

        [TestMethod]
        public void When_AllToolsAndGnomeKeysPresent_AllFeaturesAreOn()
        {
            var services = new LinuxPlatformServices(GnomeRunner());

            var expected = PlatformFeatures.Trash | PlatformFeatures.RevealInFileManager | PlatformFeatures.SetWallpaper
                | PlatformFeatures.OpenWithList | PlatformFeatures.CopyFiles | PlatformFeatures.SystemAccent | PlatformFeatures.DisplayInfo;
            Assert.AreEqual(expected, services.Features);
        }

        [TestMethod]
        public void When_GsettingsExistsWithoutTheGnomeSchemas_WallpaperAndAccentStayHidden()
        {
            // KDE and others ship gsettings but not the GNOME schemas; the keys do not exist there.
            var runner = new FakeCommandRunner("gio", "xdg-open", "gsettings")
                .Returns("gsettings list-keys org.gnome.desktop.background", 1, "No such schema")
                .Returns("gsettings list-keys org.gnome.desktop.interface", 1, "No such schema");

            var features = new LinuxPlatformServices(runner).Features;

            Assert.IsFalse(features.HasFlag(PlatformFeatures.SetWallpaper));
            Assert.IsFalse(features.HasFlag(PlatformFeatures.SystemAccent));
            Assert.IsTrue(features.HasFlag(PlatformFeatures.Trash));
        }

        [TestMethod]
        public void When_GnomeIsOlderThanTheAccentKey_OnlyTheWallpaperIsOn()
        {
            var runner = new FakeCommandRunner("gsettings")
                .Returns("gsettings list-keys org.gnome.desktop.background", 0, "picture-uri\n")
                .Returns("gsettings list-keys org.gnome.desktop.interface", 0, "color-scheme\n");

            var features = new LinuxPlatformServices(runner).Features;

            Assert.IsTrue(features.HasFlag(PlatformFeatures.SetWallpaper));
            Assert.IsFalse(features.HasFlag(PlatformFeatures.SystemAccent));
        }

        [TestMethod]
        public void When_FeaturesAreReadRepeatedly_TheSystemIsProbedOnce()
        {
            var runner = GnomeRunner();
            var services = new LinuxPlatformServices(runner);

            _ = services.Features;
            _ = services.Features;
            _ = services.Features;

            Assert.AreEqual(2, runner.Ran.Count, "one gsettings key listing per schema");
        }

        [TestMethod]
        public void When_ToolsAreMissing_TheMatchingFeaturesAreOff()
        {
            var runner = new FakeCommandRunner("xdg-open");
            var services = new LinuxPlatformServices(runner);

            Assert.AreEqual(PlatformFeatures.RevealInFileManager | PlatformFeatures.CopyFiles | PlatformFeatures.DisplayInfo, services.Features);
            Assert.AreEqual(0, runner.Ran.Count);
        }

        [TestMethod]
        public void When_Trashing_UsesGioAndRequiresTheFileToBeGone()
        {
            var runner = new FakeCommandRunner("gio");
            var services = new LinuxPlatformServices(runner);
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            Assert.IsFalse(services.MoveToTrash(missing));
            Assert.AreEqual(0, runner.Ran.Count);

            var file = Path.GetTempFileName();
            try
            {
                // gio "succeeds" but the file is still there: not trashed.
                Assert.IsFalse(services.MoveToTrash(file));
                Assert.AreEqual($"gio trash {file}", runner.Ran.Single().ToString());
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void When_Revealing_UsesTheFileManagerBusThenFallsBackToTheFolder()
        {
            var path = "/home/me/Pictures/a b.jpg";
            var withBus = new FakeCommandRunner("dbus-send");
            new LinuxPlatformServices(withBus).RevealInFileManager(path);

            var command = withBus.Ran.Single();
            Assert.AreEqual("dbus-send", command.File);
            CollectionAssert.Contains(command.Args, "array:string:file:///home/me/Pictures/a%20b.jpg");
            Assert.AreEqual(0, withBus.Started.Count);

            var failingBus = new FakeCommandRunner("dbus-send").Returns(command.ToString(), 1);
            new LinuxPlatformServices(failingBus).RevealInFileManager(path);
            Assert.AreEqual("xdg-open /home/me/Pictures", failingBus.Started.Single().ToString());

            var noBus = new FakeCommandRunner();
            new LinuxPlatformServices(noBus).RevealInFileManager(path);
            Assert.AreEqual("xdg-open /home/me/Pictures", noBus.Started.Single().ToString());
        }

        [TestMethod]
        public void When_SettingWallpaper_BothLightAndDarkKeysGetTheFileUri()
        {
            var runner = new FakeCommandRunner("gsettings");

            Assert.IsTrue(new LinuxPlatformServices(runner).SetAsWallpaper("/home/me/Pictures/a b.jpg"));

            CollectionAssert.AreEqual(
                new[]
                {
                    "gsettings set org.gnome.desktop.background picture-uri file:///home/me/Pictures/a%20b.jpg",
                    "gsettings set org.gnome.desktop.background picture-uri-dark file:///home/me/Pictures/a%20b.jpg",
                },
                runner.Ran.Select(c => c.ToString()).ToArray());
        }

        [TestMethod]
        public void When_OneWallpaperKeyFails_ReportsFailure()
        {
            var runner = new FakeCommandRunner("gsettings")
                .Returns("gsettings set org.gnome.desktop.background picture-uri-dark file:///a.jpg", 1);

            Assert.IsFalse(new LinuxPlatformServices(runner).SetAsWallpaper("/a.jpg"));
        }

        [TestMethod]
        public async Task When_ListingOpenWithApps_ParsesGioMimeAndSkipsDuplicates()
        {
            const string output = """
                Default application for “image/jpeg”: org.gnome.Loupe.desktop
                Registered applications:
                	org.gnome.Loupe.desktop
                	gimp.desktop
                Recommended applications:
                	org.gnome.Loupe.desktop
                """;
            var runner = new FakeCommandRunner("gio").Returns("gio mime image/jpeg", 0, output);

            var apps = await new LinuxPlatformServices(runner).GetOpenWithAppsAsync("/pics/a.JPG");

            CollectionAssert.AreEqual(
                new[] { new OpenWithApp("Loupe", "org.gnome.Loupe.desktop"), new OpenWithApp("Gimp", "gimp.desktop") },
                apps.ToArray());
        }

        [TestMethod]
        public async Task When_OpeningWithAnApp_LaunchesItsDesktopFileFoundInTheDataDirs()
        {
            var runner = new FakeCommandRunner("gio");
            var services = new LinuxPlatformServices(
                runner,
                path => path == "/usr/share/applications/gimp.desktop",
                ["/home/me/.local/share", "/usr/share"]);

            Assert.IsTrue(await services.OpenWithAsync("/pics/a.jpg", new OpenWithApp("Gimp", "gimp.desktop")));
            Assert.AreEqual("gio launch /usr/share/applications/gimp.desktop /pics/a.jpg", runner.Started.Single().ToString());

            Assert.IsFalse(await services.OpenWithAsync("/pics/a.jpg", new OpenWithApp("Other", "other.desktop")));
            Assert.IsFalse(await services.OpenWithAsync("/pics/a.jpg"), "Linux has no system chooser");
        }

        [TestMethod]
        [DataRow(".jpg", "image/jpeg")]
        [DataRow(".JPEG", "image/jpeg")]
        [DataRow(".png", "image/png")]
        [DataRow(".webp", "image/webp")]
        [DataRow(".gif", "image/gif")]
        [DataRow(".bmp", "image/bmp")]
        [DataRow(".tiff", "image/tiff")]
        [DataRow(".unknown", "image/jpeg")]
        public void When_MappingExtension_GivesTheMimeType(string extension, string mime)
        {
            Assert.AreEqual(mime, LinuxPlatformServices.Commands.MimeTypeFor("/pics/a" + extension));
        }

        [TestMethod]
        [DataRow("'blue'\n", 0x35, 0x84, 0xE4)]
        [DataRow("'purple'", 0x91, 0x41, 0xAC)]
        public void When_GnomeAccentIsKnown_ReturnsItsColor(string output, int r, int g, int b)
        {
            Assert.AreEqual(new SystemAccent((byte)r, (byte)g, (byte)b), LinuxPlatformServices.ParseAccent(output));
        }

        [TestMethod]
        public void When_AccentKeyIsMissingOrUnknown_ReturnsNull()
        {
            Assert.IsNull(LinuxPlatformServices.ParseAccent("'magenta'"));
            Assert.IsNull(LinuxPlatformServices.ParseAccent("No such key"));

            var runner = new FakeCommandRunner("gsettings").Returns("gsettings get org.gnome.desktop.interface accent-color", 1, "No such key");
            Assert.IsNull(new LinuxPlatformServices(runner).GetSystemAccent());
        }

        [TestMethod]
        public void When_ParsingXrandrMonitors_ReturnsGeometryAndPrimary()
        {
            const string output = """
                Monitors: 2
                 0: +*eDP-1 1920/344x1080/194+0+0  eDP-1
                 1: +HDMI-1 2560/597x1440/336+1920+-200  HDMI-1
                """;

            var displays = LinuxPlatformServices.ParseMonitors(output);

            CollectionAssert.AreEqual(
                new[] { new DisplayRect(0, 0, 1920, 1080, true), new DisplayRect(1920, -200, 2560, 1440, false) },
                displays.ToArray());
        }

        [TestMethod]
        public void When_XrandrOutputIsGarbage_ReturnsNoDisplays()
        {
            Assert.AreEqual(0, LinuxPlatformServices.ParseMonitors("Can't open display").Count);
            Assert.AreEqual(0, LinuxPlatformServices.ParseMonitors("").Count);
        }
    }
}
