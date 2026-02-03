# PasswordVaultSecureStorage

The standard cross-platform implementation of `ISecureStorage` using native operating system security services.

## Overview

`PasswordVaultSecureStorage` wraps the `Windows.Security.Credentials.PasswordVault` API. Thanks to the Uno Platform, this single implementation provides hardware-backed security on supported target platforms, with a fallback on platforms where `PasswordVault` is not available:

- **Windows**: Persists to the **Windows Credential Manager**.
- **iOS / macOS**: Stores data in the **System Keychain**.
- **Android**: Stores data in the **Android KeyStore**.
- **WASM / Browser / Skia Desktop**: Falls back to `ApplicationData.Current.LocalSettings` (localStorage on WASM, settings files on desktop). This is **not** a secure vault.

## Performance and Security

- **Encryption**: Data is encrypted at rest using OS-level keys.
- **Isolation**: Each entry is keyed by `resource` and `key`, ensuring isolation between different integrations.
- **Exceptions**: The implementation internally handles cases where keys are missing, returning `null` instead of throwing exceptions for expected "not found" scenarios.

## Quick Start

```csharp
using Flowery.Services;

// Create the storage instance
ISecureStorage secureStorage = new PasswordVaultSecureStorage();

// Store a secret
secureStorage.SetValue("App.Integrations", "ApiKey", "12345-Secret-Value");

// Retrieve it
string? token = secureStorage.GetValue("App.Integrations", "ApiKey");
```

## API Reference

### Methods

| Method | Description |
| -------- | ----------- |
| `SetValue` | Adds or updates a credential in the vault. Automatically handles resource cleanup to avoid duplicates. |
| `GetValue` | Retrieves the password for the given resource and key. Returns null if not found or if access is denied. |
| `RemoveValue` | Safely removes the credential if it exists. No-op if it doesn't exist. |

## Implementation Details

The class uses `PasswordCredential` as the underlying transport. It calls `.RetrievePassword()` internally before returning the string value, as some platforms only return metadata until the password is explicitly requested.

## See Also

- [ISecureStorage](ISecureStorage.md)
- [GitHubUserProvider](GitHubUserProvider.md)
- [PlatformCompatibility](../Flowery.Uno/Services/PlatformCompatibility.cs)
