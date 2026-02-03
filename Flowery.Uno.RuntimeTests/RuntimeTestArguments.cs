using System;
using System.Collections.Generic;

namespace Flowery.Uno.RuntimeTests
{
    public static class RuntimeTestArguments
    {
        public static bool TryGetRuntimeTestsPath(string? arguments, string[]? fallbackArgs, out string resultPath)
        {
            if (TryGetRuntimeTestsPathFromEnvironment(out resultPath))
            {
                return true;
            }

            if (TryGetRuntimeTestsPath(arguments, out resultPath))
            {
                return true;
            }

            if (fallbackArgs != null && fallbackArgs.Length > 0)
            {
                return TryGetRuntimeTestsPath(string.Join(' ', fallbackArgs), out resultPath);
            }

            return false;
        }

        public static bool TryGetRuntimeTestsPath(string? arguments, out string resultPath)
        {
            if (TryGetRuntimeTestsPathFromEnvironment(out resultPath))
            {
                return true;
            }

            resultPath = string.Empty;
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return false;
            }

            var tokens = Tokenize(arguments);
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.StartsWith("--runtime-tests=", StringComparison.OrdinalIgnoreCase))
                {
                    resultPath = TrimQuotes(token.Substring("--runtime-tests=".Length));
                    return !string.IsNullOrWhiteSpace(resultPath);
                }

                if (string.Equals(token, "--runtime-tests", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Count)
                {
                    resultPath = TrimQuotes(tokens[i + 1]);
                    return !string.IsNullOrWhiteSpace(resultPath);
                }
            }

            return false;
        }

        private static bool TryGetRuntimeTestsPathFromEnvironment(out string resultPath)
        {
            resultPath = Environment.GetEnvironmentVariable("FLOWERY_RUNTIME_TESTS_PATH") ?? string.Empty;
            return !string.IsNullOrWhiteSpace(resultPath);
        }

        private static List<string> Tokenize(string arguments)
        {
            var tokens = new List<string>();
            var current = new List<char>();
            var inQuotes = false;

            foreach (var ch in arguments)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(ch))
                {
                    if (current.Count > 0)
                    {
                        tokens.Add(new string(current.ToArray()));
                        current.Clear();
                    }
                    continue;
                }

                current.Add(ch);
            }

            if (current.Count > 0)
            {
                tokens.Add(new string(current.ToArray()));
            }

            return tokens;
        }

        private static string TrimQuotes(string value)
        {
            return value.Trim().Trim('"');
        }
    }
}
