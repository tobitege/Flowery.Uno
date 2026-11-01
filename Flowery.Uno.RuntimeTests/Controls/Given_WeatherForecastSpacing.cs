using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowery.Controls.Weather;
using Flowery.Controls.Weather.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.Controls
{
    // The forecast strip spaces its days by ItemSpacing. A binding to TemplatedParent in the items panel template does
    // not reach the strip, so the strip sets the spacing of its panel itself.
    [TestClass]
    public class Given_WeatherForecastSpacing
    {
        [TestMethod]
        public async Task When_TheItemSpacingChanges_TheItemsPanelSpacesTheDaysByIt()
        {
            var forecast = new DaisyWeatherForecast
            {
                AutoFitItems = false,
                ItemSpacing = 10,
                ItemsSource = new List<ForecastDay>
                {
                    new() { Date = new DateTime(2026, 10, 31), HighTemperature = 12, LowTemperature = 4 },
                    new() { Date = new DateTime(2026, 11, 1), HighTemperature = 10, LowTemperature = 3 }
                }
            };
            RuntimeTestHelpers.AttachToHost(forecast);
            await RuntimeTestHelpers.CompleteLayoutAsync(forecast);
            var panel = forecast.ItemsPanelRoot as StackPanel;
            Assert.IsNotNull(panel, "the forecast has no stack panel for its days");
            Assert.AreEqual(10, panel.Spacing, "the days are not spaced by ItemSpacing");

            forecast.ItemSpacing = 6;
            await RuntimeTestHelpers.CompleteLayoutAsync(forecast);
            Assert.AreEqual(6, panel.Spacing, "the days do not follow a new ItemSpacing");
        }
    }
}
