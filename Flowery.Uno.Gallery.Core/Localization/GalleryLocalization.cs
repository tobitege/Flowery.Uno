using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flowery.Localization;

#nullable enable

namespace Flowery.Uno.Gallery.Localization
{
    /// <summary>
    /// Provides JSON-based localization services for the Flowery.Uno.Gallery application.
    /// </summary>
    [Microsoft.UI.Xaml.Data.Bindable]
    public class GalleryLocalization : INotifyPropertyChanged
    {
        private static string _currentCultureName = "en";
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = [];
        private static readonly Lazy<GalleryLocalization> _instance = new(() => new GalleryLocalization());

        public static GalleryLocalization Instance => _instance.Value;
        public static event EventHandler<string>? CultureChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        static GalleryLocalization()
        {
            foreach (var lang in FloweryLocalization.SupportedLanguages)
                LoadTranslation(lang);

            FloweryLocalization.CultureChanged += OnFloweryCultureChanged;

            // Register Gallery's localization as the custom resolver for Flowery controls.
            FloweryLocalization.CustomResolver = GetString;

            // Sync with the current app culture in case this loads after a change.
            SetCulture(FloweryLocalization.CurrentCultureName);
        }

        private static void OnFloweryCultureChanged(object? sender, string cultureName)
        {
            SetCulture(cultureName);
        }

        public GalleryLocalization()
        {
        }

        public static string CurrentCultureName => _currentCultureName;

        /// <summary>
        /// Indexer for XAML binding: {Binding [KeyName]}
        /// </summary>
        public string this[string key] => GetString(key);

        public static void SetCulture(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return;

            if (string.Equals(_currentCultureName, cultureName, StringComparison.OrdinalIgnoreCase))
                return;

            _currentCultureName = cultureName;

            CultureChanged?.Invoke(null, cultureName);

            // Use DispatcherQueue to ensure PropertyChanged notifications run on UI thread.
            NotifyPropertyChangedOnUIThread();
        }

        /// <summary>
        /// Notifies bindings to refresh. Called internally when culture changes.
        /// </summary>
        private static void NotifyPropertyChangedOnUIThread()
        {
            var instance = Instance;
            var handler = instance.PropertyChanged;
            if (handler == null)
                return;

            void RaiseNotifications()
            {
                handler.Invoke(instance, new PropertyChangedEventArgs("Item"));
                handler.Invoke(instance, new PropertyChangedEventArgs("Item[]"));
            }

            try
            {
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue != null)
                {
                    dispatcherQueue.TryEnqueue(RaiseNotifications);
                }
                else
                {
                    var mainDispatcher = Microsoft.UI.Xaml.Window.Current?.DispatcherQueue;
                    if (mainDispatcher != null)
                    {
                        mainDispatcher.TryEnqueue(RaiseNotifications);
                    }
                    else
                    {
                        RaiseNotifications();
                    }
                }
            }
            catch
            {
                RaiseNotifications();
            }
        }

        public static string GetString(string key)
        {
            try
            {
                // Try exact match first
                if (_translations.TryGetValue(_currentCultureName, out var exactDict) &&
                    exactDict.TryGetValue(key, out var exactValue))
                {
                    return exactValue;
                }

                // Try two-letter code (e.g., "de" from "de-DE")
                var dashIndex = _currentCultureName.IndexOf('-');
                if (dashIndex > 0)
                {
                    var twoLetter = _currentCultureName[..dashIndex];
                    if (_translations.TryGetValue(twoLetter, out var langDict) &&
                        langDict.TryGetValue(key, out var langValue))
                    {
                        return langValue;
                    }
                }

                // Fallback to English
                if (_translations.TryGetValue("en", out var enDict) &&
                    enDict.TryGetValue(key, out var enValue))
                {
                    return enValue;
                }

                return GetLibraryOrHumanize(key);
            }
            catch
            {
                return GetLibraryOrHumanize(key);
            }
        }

        private static string GetLibraryOrHumanize(string key)
        {
            var libraryValue = FloweryLocalization.GetStringInternal(key);
            return string.Equals(libraryValue, key, StringComparison.Ordinal)
                ? HumanizeKey(key)
                : libraryValue;
        }

        private static string HumanizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key;

            var parts = key.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return key;

            var startIndex = 0;
            if (parts.Length > 1 && IsKnownPrefix(parts[0]))
            {
                startIndex = 1;
            }

            List<string> words = [];
            for (var i = startIndex; i < parts.Length; i++)
            {
                var token = SplitCamelCase(parts[i]);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    words.Add(token);
                }
            }

            return words.Count == 0 ? key : string.Join(" ", words);
        }

        private static bool IsKnownPrefix(string value)
        {
            return string.Equals(value, "Sidebar", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Gallery", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Showcase", StringComparison.OrdinalIgnoreCase);
        }

        private static string SplitCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var buffer = new System.Text.StringBuilder(value.Length * 2);
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (i > 0)
                {
                    var previous = value[i - 1];
                    var next = i + 1 < value.Length ? value[i + 1] : '\0';

                    if (char.IsDigit(current) && char.IsLetter(previous))
                    {
                        buffer.Append(' ');
                    }
                    else if (char.IsLetter(current) && char.IsDigit(previous))
                    {
                        buffer.Append(' ');
                    }
                    else if (char.IsUpper(current)
                        && (char.IsLower(previous)
                            || (char.IsUpper(previous) && next != '\0' && char.IsLower(next))))
                    {
                        buffer.Append(' ');
                    }
                }

                buffer.Append(current);
            }

            return buffer.ToString();
        }

        private static void LoadTranslation(string languageCode)
        {
            try
            {
                var assembly = typeof(GalleryLocalization).Assembly;
                var resourceName = $"{assembly.GetName().Name}.Localization.{languageCode}.json";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return;

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var dict = JsonSerializer.Deserialize(json, LocalizationJsonContext.Default.DictionaryStringString);

                if (dict != null)
                    _translations[languageCode] = dict;
            }
            catch
            {
                // Silently ignore - fallback to English will be used.
            }
        }
    }

    /// <summary>
    /// JSON source generator context for AOT compatibility.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal partial class LocalizationJsonContext : JsonSerializerContext
    {
    }
}
