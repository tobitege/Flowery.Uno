using System.Globalization;
using System.Threading.Tasks;
using Flowery.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests
{
    [TestClass]
    public class Given_RuntimeTestEnvironment
    {
        [TestMethod]
        public async Task When_RuntimeTestsStart_LanguageAndCulturesAreEnglish()
        {
            Assert.AreEqual("en", FloweryLocalization.CurrentCultureName);
            Assert.AreEqual("en", CultureInfo.CurrentCulture.Name);
            Assert.AreEqual("en", CultureInfo.CurrentUICulture.Name);
            Assert.AreEqual("en", CultureInfo.DefaultThreadCurrentCulture?.Name);
            Assert.AreEqual("en", CultureInfo.DefaultThreadCurrentUICulture?.Name);
            Assert.AreEqual("en", RuntimeTestContext.HostPanel?.Language);
            Assert.AreEqual("Cancel", FloweryLocalization.GetStringInternal("Common_Cancel"));

            await Task.Run(static () =>
            {
                Assert.AreEqual("en", CultureInfo.CurrentCulture.Name);
                Assert.AreEqual("en", CultureInfo.CurrentUICulture.Name);
            });
        }
    }
}
