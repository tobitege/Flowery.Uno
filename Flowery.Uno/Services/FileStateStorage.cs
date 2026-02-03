using System;
using System.Collections.Generic;
using System.IO;

namespace Flowery.Services
{
    /// <summary>
    /// File-based state storage for Desktop platforms.
    /// Stores state in LocalApplicationData folder.
    /// </summary>
    public partial class FileStateStorage : IStateStorage
    {
        private readonly string _baseDir;

        public FileStateStorage(string appName = "FloweryGallery")
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _baseDir = string.IsNullOrWhiteSpace(localAppData) ? appName : System.IO.Path.Combine(localAppData, appName);
        }

        public IReadOnlyList<string> LoadLines(string key)
        {
            try
            {
                var filePath = GetFilePath(key);
                if (!File.Exists(filePath))
                    return new ReadOnlyStringList();

                return new ReadOnlyStringList(File.ReadLines(filePath));
            }
            catch
            {
                return new ReadOnlyStringList();
            }
        }

        public void SaveLines(string key, IEnumerable<string> lines)
        {
            try
            {
                Directory.CreateDirectory(_baseDir);
                var filePath = GetFilePath(key);
                File.WriteAllLines(filePath, lines);
            }
            catch
            {
            }
        }

        public void Delete(string key)
        {
            try
            {
                var filePath = GetFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }

        public void Rename(string sourceKey, string targetKey)
        {
            try
            {
                var sourcePath = GetFilePath(sourceKey);
                var targetPath = GetFilePath(targetKey);
                if (!File.Exists(sourcePath))
                {
                    return;
                }

                Directory.CreateDirectory(_baseDir);
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                File.Move(sourcePath, targetPath);
            }
            catch
            {
            }
        }

        public IEnumerable<string> GetKeys(string prefix)
        {
            try
            {
                if (!Directory.Exists(_baseDir))
                    return Array.Empty<string>();

                var results = new List<string>();
                var prefixValue = prefix ?? string.Empty;
                foreach (var filePath in Directory.EnumerateFiles(_baseDir, "*.state"))
                {
                    var key = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (!string.IsNullOrEmpty(prefixValue) &&
                        !key.StartsWith(prefixValue, StringComparison.Ordinal))
                        continue;

                    if (key.EndsWith(".tmp", StringComparison.Ordinal))
                        continue;

                    results.Add(key);
                }

                return new ReadOnlyStringList(results);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private string GetFilePath(string key)
        {
            var safeKey = string.Join("_", key.Split(System.IO.Path.GetInvalidFileNameChars()));
            return System.IO.Path.Combine(_baseDir, safeKey + ".state");
        }
    }
}
