using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Flowery.Controls;
using Flowery.Controls.Weather;
using Flowery.Controls.Weather.Models;
using Flowery.Controls.Weather.Services;
using Flowery.Enums;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class CustomControls : ScrollableExamplePage
    {
        private OpenWeatherMapService? _weatherService;

        public CustomControls()
        {
            InitializeComponent();
            InitializeForecastData();
            InitializeStaticWeatherSamples();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void InitializeForecastData()
        {
            // Full weather card: 7-day forecast with US Fahrenheit (2-3 digit temps)
            // Stress-tests AutoFitItems with wide temperature text
            if (FullWeatherCard != null)
            {
                FullWeatherCard.TemperatureUnit = "F";
                FullWeatherCard.Forecast =
                [
                    new() { Date = new DateTime(2025, 7, 16), Condition = WeatherCondition.Sunny, HighTemperature = 102, LowTemperature = 78 },
                    new() { Date = new DateTime(2025, 7, 17), Condition = WeatherCondition.Sunny, HighTemperature = 99, LowTemperature = 75 },
                    new() { Date = new DateTime(2025, 7, 18), Condition = WeatherCondition.PartlyCloudy, HighTemperature = 95, LowTemperature = 72 },
                    new() { Date = new DateTime(2025, 7, 19), Condition = WeatherCondition.Thunderstorm, HighTemperature = 88, LowTemperature = 68 },
                    new() { Date = new DateTime(2025, 7, 20), Condition = WeatherCondition.PartlyCloudy, HighTemperature = 92, LowTemperature = 70 },
                    new() { Date = new DateTime(2025, 7, 21), Condition = WeatherCondition.Sunny, HighTemperature = 98, LowTemperature = 74 },
                    new() { Date = new DateTime(2025, 7, 22), Condition = WeatherCondition.Sunny, HighTemperature = 100, LowTemperature = 76 }
                ];
            }

            // Compact weather card: No metrics (ShowMetrics=False), 5-day forecast
            // Demonstrates warmer/sunny weather theme - AutoFitItems scales items to fit
            if (CompactWeatherCard != null)
            {
                CompactWeatherCard.Forecast =
                [
                    new() { Date = new DateTime(2025, 7, 16), Condition = WeatherCondition.Sunny, HighTemperature = 24, LowTemperature = 18 },
                    new() { Date = new DateTime(2025, 7, 17), Condition = WeatherCondition.PartlyCloudy, HighTemperature = 23, LowTemperature = 17 },
                    new() { Date = new DateTime(2025, 7, 18), Condition = WeatherCondition.Cloudy, HighTemperature = 21, LowTemperature = 16 },
                    new() { Date = new DateTime(2025, 7, 19), Condition = WeatherCondition.PartlyCloudy, HighTemperature = 22, LowTemperature = 15 },
                    new() { Date = new DateTime(2025, 7, 20), Condition = WeatherCondition.Sunny, HighTemperature = 25, LowTemperature = 17 }
                ];
            }

            // Standalone forecast strip: 6-day forecast showing various conditions
            // Demonstrates the DaisyWeatherForecast control independently
            if (ForecastStrip != null)
            {
                ForecastStrip.ItemsSource = new List<ForecastDay>
                {
                    new() { Date = new DateTime(2025, 7, 16), Condition = WeatherCondition.PartlyCloudy, HighTemperature = 16, LowTemperature = 8 },
                    new() { Date = new DateTime(2025, 7, 17), Condition = WeatherCondition.Rain, HighTemperature = 15, LowTemperature = 9 },
                    new() { Date = new DateTime(2025, 7, 18), Condition = WeatherCondition.Thunderstorm, HighTemperature = 14, LowTemperature = 10 },
                    new() { Date = new DateTime(2025, 7, 19), Condition = WeatherCondition.Sunny, HighTemperature = 18, LowTemperature = 11 },
                    new() { Date = new DateTime(2025, 7, 20), Condition = WeatherCondition.Sunny, HighTemperature = 20, LowTemperature = 12 },
                    new() { Date = new DateTime(2025, 7, 21), Condition = WeatherCondition.PartlyCloudy, HighTemperature = 19, LowTemperature = 13 }
                };
            }
        }

        private void InitializeStaticWeatherSamples()
        {
            var sampleDate = new DateTime(2025, 7, 15);

            if (FullWeatherCard != null)
            {
                FullWeatherCard.Date = sampleDate;
                FullWeatherCard.Sunrise = new TimeSpan(7, 33, 0);
                FullWeatherCard.Sunset = new TimeSpan(17, 20, 0);
            }

            if (CompactWeatherCard != null)
            {
                CompactWeatherCard.Date = sampleDate;
                CompactWeatherCard.Sunrise = new TimeSpan(5, 45, 0);
                CompactWeatherCard.Sunset = new TimeSpan(20, 30, 0);
            }

            if (CurrentWeatherCloudy != null)
            {
                CurrentWeatherCloudy.Date = sampleDate;
                CurrentWeatherCloudy.Sunrise = new TimeSpan(6, 15, 0);
                CurrentWeatherCloudy.Sunset = new TimeSpan(21, 0, 0);
            }

            if (CurrentWeatherSunny != null)
            {
                CurrentWeatherSunny.Date = sampleDate;
            }

            if (CurrentWeatherSnow != null)
            {
                CurrentWeatherSnow.Date = new DateTime(2025, 1, 15);
                CurrentWeatherSnow.Sunrise = new TimeSpan(8, 30, 0);
                CurrentWeatherSnow.Sunset = new TimeSpan(16, 45, 0);
            }
        }

        private async void FetchWeatherBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ApiKeyInput == null || LocationInput == null || StatusAlert == null ||
                StatusMessage == null || LiveResultPanel == null || LiveWeatherCard == null)
                return;

            var apiKey = ApiKeyInput.Text?.Trim();
            var location = LocationInput.Text?.Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                ShowStatus(StatusAlert, StatusMessage, "Please enter your OpenWeatherMap API key.", DaisyAlertVariant.Warning);
                return;
            }

            if (string.IsNullOrEmpty(location))
            {
                ShowStatus(StatusAlert, StatusMessage, "Please enter a location (e.g., London,UK).", DaisyAlertVariant.Warning);
                return;
            }

            try
            {
                if (FetchWeatherBtn != null) FetchWeatherBtn.IsEnabled = false;
                ShowStatus(StatusAlert, StatusMessage, $"Fetching weather for {location}...", DaisyAlertVariant.Info);

                _weatherService = new OpenWeatherMapService(apiKey);

                var currentWeather = await _weatherService.GetCurrentWeatherAsync(location);
                var forecast = await _weatherService.GetForecastAsync(location, 5);
                var metrics = await _weatherService.GetMetricsAsync(location);

                LiveWeatherCard.Temperature = currentWeather.Temperature;
                LiveWeatherCard.FeelsLike = currentWeather.FeelsLike;
                LiveWeatherCard.Condition = currentWeather.Condition;
                LiveWeatherCard.Date = currentWeather.Date;
                LiveWeatherCard.Sunrise = currentWeather.Sunrise;
                LiveWeatherCard.Sunset = currentWeather.Sunset;
                LiveWeatherCard.Forecast = [.. forecast];
                LiveWeatherCard.UvIndex = metrics.UvIndex;
                LiveWeatherCard.UvMax = metrics.UvMax;
                LiveWeatherCard.WindSpeed = metrics.WindSpeed;
                LiveWeatherCard.WindMax = metrics.WindMax;
                LiveWeatherCard.WindUnit = metrics.WindUnit;
                LiveWeatherCard.Humidity = metrics.Humidity;
                LiveWeatherCard.HumidityMax = metrics.HumidityMax;

                LiveResultPanel.Visibility = Visibility.Visible;
                ShowStatus(StatusAlert, StatusMessage, $"Successfully fetched weather for {currentWeather.Location ?? location}!", DaisyAlertVariant.Success);
            }
            catch (Exception ex)
            {
                LiveResultPanel.Visibility = Visibility.Collapsed;
                var errorMsg = ex.Message;

                if (ex.Message.Contains("401") || ex.Message.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase))
                    errorMsg = "Invalid API key. New keys can take up to 2 hours to activate. Please wait and try again.";
                else if (ex.Message.Contains("404") || ex.Message.Contains("city not found", StringComparison.OrdinalIgnoreCase))
                    errorMsg = $"Location '{location}' not found. Try format: 'City,CountryCode' (e.g., 'London,GB').";
                else if (ex.Message.Contains("429"))
                    errorMsg = "API rate limit exceeded. Please wait a minute and try again.";

                ShowStatus(StatusAlert, StatusMessage, errorMsg, DaisyAlertVariant.Error);
            }
            finally
            {
                if (FetchWeatherBtn != null) FetchWeatherBtn.IsEnabled = true;
            }
        }

        private async void TestApiKeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ApiKeyInput == null || StatusAlert == null || StatusMessage == null)
                return;

            var apiKey = ApiKeyInput.Text?.Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                ShowStatus(StatusAlert, StatusMessage, "Please enter your API key first.", DaisyAlertVariant.Warning);
                return;
            }

            try
            {
                if (TestApiKeyBtn != null) TestApiKeyBtn.IsEnabled = false;
                ShowStatus(StatusAlert, StatusMessage, "Testing API key...", DaisyAlertVariant.Info);

                using var httpClient = new System.Net.Http.HttpClient();
                var testUrl = $"https://api.openweathermap.org/data/2.5/weather?q=London&appid={apiKey}";
                var response = await httpClient.GetAsync(testUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ShowStatus(StatusAlert, StatusMessage, "API key is valid and active!", DaisyAlertVariant.Success);
                }
                else if ((int)response.StatusCode == 401)
                {
                    ShowStatus(StatusAlert, StatusMessage, "API key is not yet active. New keys take up to 2 hours to activate.", DaisyAlertVariant.Warning);
                }
                else
                {
                    ShowStatus(StatusAlert, StatusMessage, $"API returned: {content}", DaisyAlertVariant.Error);
                }
            }
            catch (Exception ex)
            {
                ShowStatus(StatusAlert, StatusMessage, $"Connection error: {ex.Message}", DaisyAlertVariant.Error);
            }
            finally
            {
                if (TestApiKeyBtn != null) TestApiKeyBtn.IsEnabled = true;
            }
        }

        private void ClearResultsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StatusAlert != null) StatusAlert.Visibility = Visibility.Collapsed;
            if (LiveResultPanel != null) LiveResultPanel.Visibility = Visibility.Collapsed;
        }

        private void LocationInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                FetchWeatherBtn_Click(sender, new RoutedEventArgs());
            }
        }

        private static void ShowStatus(DaisyAlert alert, TextBlock message, string text, DaisyAlertVariant variant)
        {
            message.Text = text;
            alert.Variant = variant;
            alert.Visibility = Visibility.Visible;
        }

    }
}
