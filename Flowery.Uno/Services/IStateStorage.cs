using System.Collections.Generic;

namespace Flowery.Services
{
    /// <summary>
    /// Abstraction for persisting key-value state across platforms.
    /// Desktop uses file storage, Browser/WASM uses localStorage.
    /// </summary>
    public interface IStateStorage
    {
        /// <summary>
        /// Loads state lines from persistent storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <returns>Lines of state data, or empty if not found</returns>
        IReadOnlyList<string> LoadLines(string key);

        /// <summary>
        /// Saves state lines to persistent storage.
        /// </summary>
        /// <param name="key">Storage key</param>
        /// <param name="lines">Lines of state data to persist</param>
        void SaveLines(string key, IEnumerable<string> lines);

        /// <summary>
        /// Deletes stored state for the given key.
        /// </summary>
        /// <param name="key">Storage key</param>
        void Delete(string key);

        /// <summary>
        /// Renames a stored state key.
        /// </summary>
        /// <param name="sourceKey">Existing key</param>
        /// <param name="targetKey">New key</param>
        void Rename(string sourceKey, string targetKey);

        /// <summary>
        /// Enumerates stored keys that start with the provided prefix.
        /// </summary>
        /// <param name="prefix">Key prefix to match (empty to return all keys).</param>
        IEnumerable<string> GetKeys(string prefix);
    }
}
