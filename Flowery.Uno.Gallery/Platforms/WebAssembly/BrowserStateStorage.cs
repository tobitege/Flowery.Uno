using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flowery.Services;
using Uno.Foundation;

namespace Flowery.Uno.Gallery.Browser
{
    /// <summary>
    /// Browser localStorage-based state storage for WASM platforms.
    /// Uses JavaScript interop to access localStorage.
    /// </summary>
    [SupportedOSPlatform("browser")]
    public partial class BrowserStateStorage : IStateStorage
    {
        private const string LineSeparator = "\n";
        private const string StoragePrefix = "flowery_uno_";

        public IReadOnlyList<string> LoadLines(string key)
        {
            try
            {
                var data = GetLocalStorageItem(StoragePrefix + key);
                if (string.IsNullOrEmpty(data))
                    return Array.Empty<string>();

                return data.Split(new[] { LineSeparator }, StringSplitOptions.None);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public void SaveLines(string key, IEnumerable<string> lines)
        {
            try
            {
                var data = string.Join(LineSeparator, lines);
                SetLocalStorageItem(StoragePrefix + key, data);
            }
            catch
            {
            }
        }

        public void Delete(string key)
        {
            try
            {
                RemoveLocalStorageItem(StoragePrefix + key);
            }
            catch
            {
            }
        }

        public void Rename(string sourceKey, string targetKey)
        {
            try
            {
                var sourceStorageKey = StoragePrefix + sourceKey;
                var targetStorageKey = StoragePrefix + targetKey;
                var data = GetLocalStorageItem(sourceStorageKey);
                if (string.IsNullOrEmpty(data))
                    return;

                SetLocalStorageItem(targetStorageKey, data);
                RemoveLocalStorageItem(sourceStorageKey);
            }
            catch
            {
            }
        }

        public IEnumerable<string> GetKeys(string prefix)
        {
            try
            {
                var keysJson = WebAssemblyRuntime.InvokeJS("JSON.stringify(Object.keys(globalThis.localStorage))");
                var keys = JsonSerializer.Deserialize(keysJson, BrowserStateStorageJsonContext.Default.StringArray);
                if (keys == null || keys.Length == 0)
                    return Array.Empty<string>();

                var results = new List<string>();
                var prefixValue = prefix ?? string.Empty;
                foreach (var key in keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (!key.StartsWith(StoragePrefix, StringComparison.Ordinal))
                        continue;

                    var storageKey = key.Substring(StoragePrefix.Length);
                    if (!string.IsNullOrEmpty(prefixValue) &&
                        !storageKey.StartsWith(prefixValue, StringComparison.Ordinal))
                        continue;

                    if (storageKey.EndsWith(".tmp", StringComparison.Ordinal))
                        continue;

                    results.Add(storageKey);
                }

                return results;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static string? GetLocalStorageItem(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var keyLiteral = ToJsStringLiteral(key);
            var result = WebAssemblyRuntime.InvokeJS($"globalThis.localStorage.getItem({keyLiteral})");
            if (string.IsNullOrWhiteSpace(result) || string.Equals(result, "null", StringComparison.OrdinalIgnoreCase))
                return null;

            return result;
        }

        private static void SetLocalStorageItem(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var keyLiteral = ToJsStringLiteral(key);
            var valueLiteral = ToJsStringLiteral(value);
            WebAssemblyRuntime.InvokeJS($"globalThis.localStorage.setItem({keyLiteral}, {valueLiteral})");
        }

        private static void RemoveLocalStorageItem(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var keyLiteral = ToJsStringLiteral(key);
            WebAssemblyRuntime.InvokeJS($"globalThis.localStorage.removeItem({keyLiteral})");
        }

        private static string ToJsStringLiteral(string value)
        {
            if (value == null)
                return "null";

            var sb = new System.Text.StringBuilder(value.Length + 2);
            sb.Append('"');

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (ch < ' ')
                        {
                            sb.Append("\\u");
                            sb.Append(((int)ch).ToString("X4"));
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(string[]))]
    internal partial class BrowserStateStorageJsonContext : JsonSerializerContext
    {
    }
}
