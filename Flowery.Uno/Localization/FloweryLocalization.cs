using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

#nullable enable

namespace Flowery.Localization
{
    /// <summary>
    /// Provides JSON-based localization services for Flowery.Uno controls.
    /// Uses embedded JSON resources for cross-platform compatibility.
    /// </summary>
    [Microsoft.UI.Xaml.Data.Bindable]
    public partial class FloweryLocalization : INotifyPropertyChanged
    {
        /// <summary>
        /// List of supported language codes. Apps can use this to iterate and load their own translations.
        /// </summary>
        public static readonly LanguageList SupportedLanguages = new([
            "en", "de", "fr", "es", "it", "ja", "ko", "zh-CN", "ar", "tr", "uk", "he"
        ]);

        /// <summary>
        /// Native display names for each supported language code.
        /// </summary>
        public static readonly LanguageDisplayNameMap LanguageDisplayNames = new(
            new(StringComparer.Ordinal)
            {
            ["en"] = "English",
            ["de"] = "Deutsch",
            ["fr"] = "Français",
            ["es"] = "Español",
            ["it"] = "Italiano",
            ["ja"] = "日本語",
            ["ko"] = "한국어",
            ["zh-CN"] = "简体中文",
            ["ar"] = "العربية",
            ["tr"] = "Türkçe",
            ["uk"] = "Українська",
            ["he"] = "עברית"
            });

        private static string _currentCultureName = GetInitialCultureName();
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = [];
        private static readonly HashSet<Assembly> _registeredAssemblies = new();
        private static readonly HashSet<string> _loadedResources = new(StringComparer.Ordinal);

        // RTL languages list for IsRtl property without needing CultureInfo
        private static readonly HashSet<string> _rtlLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", "he", "fa", "ur", "yi", "ar-SA", "he-IL", "fa-IR", "ur-PK"
        };
        private static readonly Lazy<FloweryLocalization> _instance = new(() => new FloweryLocalization());
        private readonly object _rtlMarker = new();

        /// <summary>
        /// Singleton instance for XAML bindings that use the indexer.
        /// </summary>
        public static FloweryLocalization Instance => _instance.Value;

        /// <summary>
        /// Event fired when the culture is changed. Subscribe to this to refresh UI bindings.
        /// </summary>
        public static event EventHandler<string>? CultureChanged;

        /// <summary>
        /// PropertyChanged event for INotifyPropertyChanged interface (used by XAML bindings).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        static FloweryLocalization()
        {
            RegisterAssembly(typeof(FloweryLocalization).Assembly);
        }

        private FloweryLocalization()
        {
            // Private constructor for singleton.
        }

        /// <summary>
        /// Gets the current UI culture used for localization.
        /// </summary>
        public static string CurrentCultureName => _currentCultureName;

        /// <summary>
        /// Gets whether the current culture is Right-To-Left.
        /// </summary>
        public bool IsRtl => _rtlMarker != null && IsRtlLanguage(_currentCultureName);

        /// <summary>
        /// Indexer to support XAML bindings with the Localization DataContext.
        /// Usage in XAML: {Binding [Button_Generate]} binds to this[Button_Generate].
        /// </summary>
        public string this[string key] => GetString(key);

        /// <summary>
        /// Sets the current UI culture and notifies subscribers.
        /// </summary>
        public static void SetCulture(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return;

            // Normalize culture name (e.g., "de-DE" -> "de" if we only have "de")
            var normalizedName = NormalizeCultureName(cultureName);

            if (string.Equals(_currentCultureName, normalizedName, StringComparison.OrdinalIgnoreCase))
                return;

            _currentCultureName = normalizedName;
            try
            {
                var culture = CultureInfo.GetCultureInfo(normalizedName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
            }
            CultureChanged?.Invoke(null, normalizedName);

            // Use DispatcherQueue to ensure PropertyChanged notifications run on UI thread.
            NotifyPropertyChangedOnUIThread();
        }

        /// <summary>
        /// Forces localization bindings to refresh even if the culture name is unchanged.
        /// </summary>
        public static void RefreshBindings()
        {
            var cultureName = _currentCultureName;
            CultureChanged?.Invoke(null, cultureName);
            NotifyPropertyChangedOnUIThread();
        }

        /// <summary>
        /// Notifies bindings to refresh. Called internally when culture changes.
        /// Uses DispatcherQueue.TryEnqueue for cross-platform UI thread safety.
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
                handler.Invoke(instance, new PropertyChangedEventArgs(nameof(IsRtl)));
            }

            // Try to use DispatcherQueue for thread-safe UI updates
            try
            {
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue != null)
                {
                    dispatcherQueue.TryEnqueue(RaiseNotifications);
                }
                else
                {
                    // Fallback: try main window's dispatcher (may be null in some scenarios)
                    var mainDispatcher = Microsoft.UI.Xaml.Window.Current?.DispatcherQueue;
                    if (mainDispatcher != null)
                    {
                        mainDispatcher.TryEnqueue(RaiseNotifications);
                    }
                    else
                    {
                        // Last resort: call directly (may not update UI on some platforms)
                        RaiseNotifications();
                    }
                }
            }
            catch
            {
                // If dispatcher access fails, call directly
                RaiseNotifications();
            }
        }

        /// <summary>
        /// Gets the initial culture name, defaulting to "en" for WASM compatibility.
        /// </summary>
        private static string GetInitialCultureName()
        {
            try
            {
                // Try to get the current UI culture name
                var name = CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName ?? "en";
                return SupportedLanguages.Contains(name) ? name : "en";
            }
            catch
            {
                // On WASM with invariant globalization, this may fail
                return "en";
            }
        }

        /// <summary>
        /// Normalizes a culture name to match our supported languages.
        /// e.g., "de-DE" -> "de", "zh-CN" -> "zh-CN" (exact match preferred)
        /// </summary>
        private static string NormalizeCultureName(string cultureName)
        {
            // First check for exact match
            if (SupportedLanguages.Contains(cultureName))
                return cultureName;

            // Try the two-letter code (e.g., "de-DE" -> "de")
            var dashIndex = cultureName.IndexOf('-');
            if (dashIndex > 0)
            {
                var twoLetter = cultureName.Substring(0, dashIndex);
                if (SupportedLanguages.Contains(twoLetter))
                    return twoLetter;
            }

            // Default to the original or "en"
            return SupportedLanguages.Contains(cultureName) ? cultureName : "en";
        }

        /// <summary>
        /// Checks if a language code represents a right-to-left language.
        /// </summary>
        private static bool IsRtlLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return false;

            // Check exact match first, then two-letter prefix
            if (_rtlLanguages.Contains(languageCode))
                return true;

            var dashIndex = languageCode.IndexOf('-');
            if (dashIndex > 0)
            {
                var twoLetter = languageCode.Substring(0, dashIndex);
                return _rtlLanguages.Contains(twoLetter);
            }

            return false;
        }

        /// <summary>
        /// Gets the two-letter language code from a culture name.
        /// </summary>
        private static string GetTwoLetterCode(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return "en";

            var dashIndex = cultureName.IndexOf('-');
            return dashIndex > 0 ? cultureName.Substring(0, dashIndex) : cultureName;
        }

        /// <summary>
        /// Optional custom resolver for app-specific localization keys.
        /// When set, GetString will use this resolver for keys not found in the library's translations.
        /// </summary>
        public static Func<string, string>? CustomResolver { get; set; }

        /// <summary>
        /// Gets a localized string by key from the library's internal translations.
        /// This method is used by library controls for their own keys.
        /// </summary>
        public static string GetStringInternal(string key)
        {
            try
            {
                // Try exact match first (e.g., "zh-CN")
                if (_translations.TryGetValue(_currentCultureName, out var exactDict) &&
                    exactDict.TryGetValue(key, out var exactValue))
                {
                    return exactValue;
                }

                // Try the two-letter code (e.g., "de" from "de-DE")
                var languageCode = GetTwoLetterCode(_currentCultureName);
                if (_translations.TryGetValue(languageCode, out var langDict) &&
                    langDict.TryGetValue(key, out var langValue))
                {
                    return langValue;
                }

                // Fallback to English
                if (_translations.TryGetValue("en", out var enDict) &&
                    enDict.TryGetValue(key, out var enValue))
                {
                    return enValue;
                }

                return key;
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Gets a localized string by key from the library's internal translations, with a fallback value.
        /// </summary>
        public static string GetStringInternal(string key, string fallback)
        {
            var value = GetStringInternal(key);
            return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
        }

        /// <summary>
        /// Gets a localized string by key. Uses the CustomResolver if set, otherwise falls back to library translations.
        /// </summary>
        public static string GetString(string key)
        {
            if (CustomResolver != null)
                return CustomResolver(key);

            return GetStringInternal(key);
        }

        /// <summary>
        /// Gets the localized display name for a theme.
        /// </summary>
        public static string GetThemeDisplayName(string themeName)
        {
            var key = $"Theme_{themeName}";
            var result = GetStringInternal(key);
            return result == key ? themeName : result;
        }

        /// <summary>
        /// Registers an assembly for localization lookup.
        /// </summary>
        public static void RegisterAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

            if (!_registeredAssemblies.Add(assembly))
                return;

            foreach (var lang in SupportedLanguages)
                LoadTranslation(assembly, lang);
        }

        private static void LoadTranslation(Assembly assembly, string languageCode)
        {
            try
            {
                var resourceName = $"{assembly.GetName().Name}.Localization.{languageCode}.json";
                if (!_loadedResources.Add(resourceName))
                    return;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return;

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var dict = JsonSerializer.Deserialize(json, FloweryLocalizationJsonContext.Default.DictionaryStringString);

                if (dict != null)
                {
                    if (!_translations.TryGetValue(languageCode, out var existing))
                    {
                        _translations[languageCode] = dict;
                    }
                    else
                    {
                        foreach (var entry in dict)
                            existing[entry.Key] = entry.Value;
                    }
                }
            }
            catch
            {
                // Silently ignore - fallback to English will be used.
            }
        }
    }

    public sealed class LanguageList
    {
        private readonly string[] _items;

        internal LanguageList(string[] items)
        {
            _items = items ?? [];
        }

        public int Count => _items.Length;

        public string this[int index] => _items[index];

        public bool Contains(string item) => Array.IndexOf(_items, item) >= 0;

        public Enumerator GetEnumerator() => new(_items);

        public struct Enumerator
        {
            private readonly string[] _items;
            private int _index;

            internal Enumerator(string[] items)
            {
                _items = items ?? [];
                _index = -1;
            }

            public readonly string Current => _items[_index];

            public bool MoveNext()
            {
                var next = _index + 1;
                if (next >= _items.Length)
                    return false;

                _index = next;
                return true;
            }
        }
    }

    public sealed class LanguageDisplayNameMap
    {
        private readonly Hashtable _values;

        internal LanguageDisplayNameMap(Hashtable values)
        {
            _values = values ?? new(StringComparer.Ordinal);
        }

        public bool TryGetValue(string key, out string value)
        {
            if (_values[key] is string stored)
            {
                value = stored;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public string this[string key] => _values[key] as string ?? string.Empty;
    }

    /// <summary>
    /// JSON source generator context for AOT compatibility.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    internal partial class FloweryLocalizationJsonContext : JsonSerializerContext
    {
    }
}
