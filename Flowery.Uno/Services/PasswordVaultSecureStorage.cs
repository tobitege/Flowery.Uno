using System;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Flowery.Services;

/// <summary>
/// Implementation of ISecureStorage using Windows.Security.Credentials.PasswordVault.
/// Cross-platform support provided by Uno Platform (Keychain on iOS/macOS, KeyStore on Android).
/// Falls back to LocalSettings on WASM/Skia where PasswordVault is not supported.
/// </summary>
public class PasswordVaultSecureStorage : ISecureStorage
{
    private const string FallbackPrefix = "flowery_secure_";
    private PasswordVault? _vault;
    private bool _vaultInitialized;
    private bool _fallbackActive = PlatformCompatibility.IsWasmBackend || PlatformCompatibility.IsSkiaBackend;

    public void SetValue(string resource, string key, string value)
    {
        if (string.IsNullOrEmpty(resource)) throw new ArgumentNullException(nameof(resource));
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        if (TryGetVault(out var vault))
        {
            using var scope = PasswordVaultOperationScope.Begin();
            try
            {
                try
                {
                    var existing = vault.Retrieve(resource, key);
                    vault.Remove(existing);
                }
                catch (Exception ex) when (IsNotSupported(ex))
                {
                    throw;
                }
                catch (Exception)
                {
                    // Ignore missing credential
                }

                if (value != null)
                {
                    var credential = new PasswordCredential(resource, key, value);
                    vault.Add(credential);
                }
                return;
            }
            catch (Exception ex) when (IsNotSupported(ex))
            {
                _fallbackActive = true;
            }
        }

        SetFallbackValue(resource, key, value);
    }

    public string? GetValue(string resource, string key)
    {
        if (string.IsNullOrEmpty(resource)) return null;
        if (string.IsNullOrEmpty(key)) return null;

        if (TryGetVault(out var vault))
        {
            using var scope = PasswordVaultOperationScope.Begin();
            try
            {
                var credential = vault.Retrieve(resource, key);
                credential.RetrievePassword();
                return credential.Password;
            }
            catch (Exception ex) when (IsNotSupported(ex))
            {
                _fallbackActive = true;
            }
            catch (Exception)
            {
                // PasswordVault throws an exception if the resource/user combo is not found
                return null;
            }
        }

        return GetFallbackValue(resource, key);
    }

    public void RemoveValue(string resource, string key)
    {
        if (string.IsNullOrEmpty(resource)) return;
        if (string.IsNullOrEmpty(key)) return;

        if (TryGetVault(out var vault))
        {
            using var scope = PasswordVaultOperationScope.Begin();
            try
            {
                var credential = vault.Retrieve(resource, key);
                vault.Remove(credential);
                return;
            }
            catch (Exception ex) when (IsNotSupported(ex))
            {
                _fallbackActive = true;
            }
            catch (Exception)
            {
                // Ignore if not found
                return;
            }
        }

        RemoveFallbackValue(resource, key);
    }

    private bool TryGetVault(out PasswordVault vault)
    {
        vault = null!;

        if (_fallbackActive)
            return false;

        if (!_vaultInitialized)
        {
            _vaultInitialized = true;
            try
            {
                _vault = new PasswordVault();
            }
            catch (Exception ex) when (IsNotSupported(ex))
            {
                _vault = null;
                _fallbackActive = true;
            }
        }

        if (_vault == null)
            return false;

        vault = _vault;
        return true;
    }

    private static bool IsNotSupported(Exception ex)
        => ex is NotSupportedException or NotImplementedException;

    private static string GetFallbackKey(string resource, string key)
        => string.Concat(FallbackPrefix, resource, "::", key);

    private static void SetFallbackValue(string resource, string key, string? value)
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var storageKey = GetFallbackKey(resource, key);
            if (value == null)
            {
                localSettings.Values.Remove(storageKey);
            }
            else
            {
                localSettings.Values[storageKey] = value;
            }
        }
        catch (Exception)
        {
        }
    }

    private static string? GetFallbackValue(string resource, string key)
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var storageKey = GetFallbackKey(resource, key);
            return localSettings.Values.TryGetValue(storageKey, out var value) ? value as string : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void RemoveFallbackValue(string resource, string key)
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove(GetFallbackKey(resource, key));
        }
        catch (Exception)
        {
        }
    }
}
