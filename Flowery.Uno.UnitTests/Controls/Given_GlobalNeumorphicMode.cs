using System;
using Flowery.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Controls
{
    [TestClass]
    public class Given_GlobalNeumorphicMode
    {
        [TestMethod]
        public void When_SetToNone_DisablesEnabled()
        {
            var originalMode = DaisyBaseContentControl.GlobalNeumorphicMode;
            var originalEnabled = DaisyBaseContentControl.GlobalNeumorphicEnabled;

            try
            {
                DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.Raised;
                Assert.IsTrue(DaisyBaseContentControl.GlobalNeumorphicEnabled, "Expected GlobalNeumorphicEnabled to be true for non-None mode.");

                DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.None;
                Assert.IsFalse(DaisyBaseContentControl.GlobalNeumorphicEnabled, "Expected GlobalNeumorphicEnabled to be false for None mode.");
            }
            finally
            {
                DaisyBaseContentControl.GlobalNeumorphicMode = originalMode;
                DaisyBaseContentControl.GlobalNeumorphicEnabled = originalEnabled;
            }
        }

        [TestMethod]
        public void When_ReEnabled_RestoresLastNonNoneMode()
        {
            var originalMode = DaisyBaseContentControl.GlobalNeumorphicMode;
            var originalEnabled = DaisyBaseContentControl.GlobalNeumorphicEnabled;

            try
            {
                DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.Inset;
                DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.None;

                DaisyBaseContentControl.GlobalNeumorphicEnabled = true;
                Assert.AreEqual(DaisyNeumorphicMode.Inset, DaisyBaseContentControl.GlobalNeumorphicMode,
                    "Expected the last non-None mode to be restored when enabling.");
            }
            finally
            {
                DaisyBaseContentControl.GlobalNeumorphicMode = originalMode;
                DaisyBaseContentControl.GlobalNeumorphicEnabled = originalEnabled;
            }
        }

        [TestMethod]
        public void When_IntensityChanges_EventFiresOnce()
        {
            var originalIntensity = DaisyBaseContentControl.GlobalNeumorphicIntensity;
            var invocationCount = 0;

            void Handler(object? sender, EventArgs e)
            {
                invocationCount++;
            }

            try
            {
                DaisyBaseContentControl.GlobalNeumorphicChanged += Handler;

                DaisyBaseContentControl.GlobalNeumorphicIntensity = originalIntensity + 0.01;
                Assert.AreEqual(1, invocationCount, "Expected a single event when intensity changes.");

                DaisyBaseContentControl.GlobalNeumorphicIntensity = originalIntensity + 0.0105;
                Assert.AreEqual(1, invocationCount, "Expected no event for changes under the threshold.");

                DaisyBaseContentControl.GlobalNeumorphicIntensity = originalIntensity + 0.02;
                Assert.AreEqual(2, invocationCount, "Expected a second event after a meaningful change.");
            }
            finally
            {
                DaisyBaseContentControl.GlobalNeumorphicChanged -= Handler;
                DaisyBaseContentControl.GlobalNeumorphicIntensity = originalIntensity;
            }
        }
    }
}
