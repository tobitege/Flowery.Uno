using System;
using System.Collections.Generic;
using System.Linq;
using Flowery.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Controls
{
    [TestClass]
    public class Given_GlobalNeumorphicEnabled
    {
        [TestMethod]
        public void When_Toggled_MultipleSubscribers_NoReentrantLoops()
        {
            var originalEnabled = DaisyBaseContentControl.GlobalNeumorphicEnabled;
            var originalMode = DaisyBaseContentControl.GlobalNeumorphicMode;
            var subscribers = new List<NeumorphicSubscriber>();

            try
            {
                subscribers.Add(new NeumorphicSubscriber("DaisyButton"));
                subscribers.Add(new NeumorphicSubscriber("DaisyButtonGroup"));
                subscribers.Add(new NeumorphicSubscriber("DaisyComboBoxBase"));
                subscribers.Add(new NeumorphicSubscriber("DaisyBaseContentControl"));

                DaisyBaseContentControl.GlobalNeumorphicEnabled = false;
                foreach (var subscriber in subscribers)
                {
                    subscriber.ResetCounters();
                }

                var toggleValues = new[] { true, false, true, false, true };
                var toggleId = 0;

                foreach (var value in toggleValues)
                {
                    toggleId++;
                    foreach (var subscriber in subscribers)
                    {
                        subscriber.BeginToggle(toggleId);
                    }

                    DaisyBaseContentControl.GlobalNeumorphicEnabled = value;

                    foreach (var subscriber in subscribers)
                    {
                        subscriber.AssertSingleInvocation(toggleId);
                    }
                }

                var expectedTotal = toggleValues.Length * subscribers.Count;
                var actualTotal = subscribers.Sum(subscriber => subscriber.TotalInvocations);

                Assert.AreEqual(expectedTotal, actualTotal, "Unexpected total GlobalNeumorphicChanged invocations.");
                Assert.IsFalse(subscribers.Any(subscriber => subscriber.HasReentrantInvocations),
                    "Detected re-entrant GlobalNeumorphicChanged invocations during toggles.");
            }
            finally
            {
                foreach (var subscriber in subscribers)
                {
                    subscriber.Dispose();
                }

                DaisyBaseContentControl.GlobalNeumorphicMode = originalMode;
                if (originalEnabled != (originalMode != DaisyNeumorphicMode.None))
                {
                    DaisyBaseContentControl.GlobalNeumorphicEnabled = originalEnabled;
                }
            }
        }

        private sealed class NeumorphicSubscriber : IDisposable
        {
            private int _lastToggleId = -1;
            private int _invocationsThisToggle;

            public NeumorphicSubscriber(string name)
            {
                Name = name;
                DaisyBaseContentControl.GlobalNeumorphicChanged += OnGlobalNeumorphicChanged;
            }

            public string Name { get; }
            public int TotalInvocations { get; private set; }
            public bool HasReentrantInvocations { get; private set; }

            public void BeginToggle(int toggleId)
            {
                _lastToggleId = toggleId;
                _invocationsThisToggle = 0;
            }

            public void ResetCounters()
            {
                TotalInvocations = 0;
                HasReentrantInvocations = false;
                _invocationsThisToggle = 0;
                _lastToggleId = -1;
            }

            public void AssertSingleInvocation(int toggleId)
            {
                Assert.AreEqual(toggleId, _lastToggleId, $"{Name} did not receive the toggle.");
                Assert.AreEqual(1, _invocationsThisToggle, $"{Name} received multiple invocations for a single toggle.");
            }

            private void OnGlobalNeumorphicChanged(object? sender, EventArgs e)
            {
                _invocationsThisToggle++;
                TotalInvocations++;

                if (_invocationsThisToggle > 1)
                {
                    HasReentrantInvocations = true;
                }

                var current = DaisyBaseContentControl.GlobalNeumorphicEnabled;
                DaisyBaseContentControl.GlobalNeumorphicEnabled = current;
            }

            public void Dispose()
            {
                DaisyBaseContentControl.GlobalNeumorphicChanged -= OnGlobalNeumorphicChanged;
            }
        }
    }
}
