namespace Flowery.Services;

/// <summary>
/// Abstraction for secure credential storage across platforms.
/// Implementation typically uses PasswordVault (Windows/Uno) or Keychain/KeyStore.
/// </summary>
public interface ISecureStorage
{
    /// <summary>
    /// Stores a secret value securely.
    /// </summary>
    /// <param name="resource">Namespace or group for the secret (e.g., "Flowery.GitHub").</param>
    /// <param name="key">The key identifying the secret (e.g., "AccessToken").</param>
    /// <param name="value">The sensitive value to store.</param>
    void SetValue(string resource, string key, string value);

    /// <summary>
    /// Retrieves a secret value.
    /// </summary>
    /// <param name="resource">Namespace or group for the secret.</param>
    /// <param name="key">The key identifying the secret.</param>
    /// <returns>The stored value, or null if not found.</returns>
    string? GetValue(string resource, string key);

    /// <summary>
    /// Removes a secret from storage.
    /// </summary>
    /// <param name="resource">Namespace or group for the secret.</param>
    /// <param name="key">The key identifying the secret.</param>
    void RemoveValue(string resource, string key);
}
