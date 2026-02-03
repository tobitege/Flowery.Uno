using System;
using System.Threading;

namespace Flowery.Services
{
    /// <summary>
    /// Tracks active PasswordVault operations for diagnostic filtering.
    /// </summary>
    public static class PasswordVaultOperationScope
    {
        private static readonly AsyncLocal<int> Depth = new();

        public static bool IsActive => Depth.Value > 0;

        public static IDisposable Begin()
        {
            Depth.Value++;
            return new Scope();
        }

        private sealed class Scope : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                var current = Depth.Value;
                if (current > 0)
                {
                    Depth.Value = current - 1;
                }
            }
        }
    }
}
