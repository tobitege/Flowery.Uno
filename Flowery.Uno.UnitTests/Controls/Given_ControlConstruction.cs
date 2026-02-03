using Flowery.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Controls
{
    [TestClass]
    public class Given_ControlConstruction
    {
        [TestMethod]
        public void When_Reading_GlobalNeumorphicDefaults_NoException()
        {
            _ = DaisyBaseContentControl.GlobalNeumorphicEnabled;
            _ = DaisyBaseContentControl.GlobalNeumorphicMode;
            _ = DaisyBaseContentControl.GlobalNeumorphicIntensity;
            _ = DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor;
            _ = DaisyBaseContentControl.GlobalNeumorphicLightShadowColor;
            _ = DaisyBaseContentControl.GlobalNeumorphicRimLightEnabled;
            _ = DaisyBaseContentControl.GlobalNeumorphicSurfaceGradientEnabled;
        }
    }
}
