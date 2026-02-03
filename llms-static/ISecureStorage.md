# ISecureStorage

A cross-platform abstraction for persisting sensitive data (secrets, tokens, passwords) securely.

## Overview

`ISecureStorage` provides a unified interface for credential management. On platforms with native vault support, values are routed to OS-backed storage (Keychain, KeyStore, Credential Manager). On WASM/Skia, the default implementation falls back to `ApplicationData.Current.LocalSettings`, which is not a secure vault.

This is the primary security foundation for FlowKanban integrations like GitHub, Teams, and Slack.

## API Reference

### Methods

| Method | Signature | Description |
| -------- | --------- | ----------- |
| `SetValue` | `void SetValue(string resource, string key, string value)` | Stores a secret value securely under a specific resource namespace and key. |
| `GetValue` | `string? GetValue(string resource, string key)` | Retrieves a secret value. Returns `null` if the key or resource is not found. |
| `RemoveValue` | `void RemoveValue(string resource, string key)` | Deletes a secret value from the secure storage. |

### Resource and Key Naming

- **Resource**: Should be a unique namespace for the application or integration (e.g., `"Flowery.GitHub"`, `"Flowery.Teams"`).
- **Key**: The specific identifier for the secret within that resource (e.g., `"AccessToken"`, `"ClientSecret"`).

## Usage Example

```csharp
using Flowery.Services;

public void StoreGithubToken(ISecureStorage storage, string token)
{
    // Save securely
    storage.SetValue("Flowery.GitHub", "AccessToken", token);
    
    // Retrieve later
    string? savedToken = storage.GetValue("Flowery.GitHub", "AccessToken");
    
    if (savedToken != null)
    {
        Console.WriteLine("Token retrieved successfully.");
    }
}
```

## Implementations

- **[PasswordVaultSecureStorage](PasswordVaultSecureStorage.md)**: Hardware-backed implementation using OS-level security vaults.

## See Also

- [PasswordVaultSecureStorage](PasswordVaultSecureStorage.md)
- [GitHubUserProvider](GitHubUserProvider.md)
- [IUserProvider](../Flowery.Uno/Controls/Kanban/Users/Interfaces/IUserProvider.cs)
