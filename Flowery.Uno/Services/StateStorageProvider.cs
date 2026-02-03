using System;

namespace Flowery.Services
{
    /// <summary>
    /// Provides access to the platform-appropriate state storage.
    /// </summary>
    public static class StateStorageProvider
    {
        private static IStateStorage? _instance;
        private static readonly System.Threading.Lock Lock = new();

        /// <summary>
        /// Gets the current state storage instance.
        /// Returns <see cref="FileStateStorage"/> by default if not configured.
        /// </summary>
        public static IStateStorage Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (Lock)
                {
                    _instance ??= new FileStateStorage();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Configures the state storage implementation.
        /// Call this at app startup before any state operations.
        /// </summary>
        public static void Configure(IStateStorage storage)
        {
            ArgumentNullException.ThrowIfNull(storage);

            lock (Lock)
            {
                _instance = storage;
            }
        }

        /// <summary>
        /// Resets the provider to uninitialized state. For testing purposes.
        /// </summary>
        internal static void Reset()
        {
            lock (Lock)
            {
                _instance = null;
            }
        }
    }
}
