using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests.WindowsRunner
{
    [TestClass]
    public class Given_RuntimeTestsRunner
    {
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public async Task When_Running_WindowsRuntimeTests_Succeeds()
        {
            var repoRoot = FindRepoRoot();
            var projectPath = Path.Combine(repoRoot, "Flowery.Uno.Gallery", "Flowery.Uno.Gallery.csproj");
            var runDirectory = TestContext?.TestRunDirectory ?? Path.Combine(repoRoot, "TestResults", "runtime-tests");
            var resultPath = Path.Combine(runDirectory, "runtime-tests-windows.xml");
            var tfm = "net9.0-windows10.0.19041";
            var configuration = "Debug";

            Directory.CreateDirectory(runDirectory);
            using var buildLock = AcquireBuildLock(TimeSpan.FromMinutes(10));
            var buildPaths = CreateBuildPaths(runDirectory, tfm);

            var buildExitCode = await RunProcessAsync(
                "dotnet",
                $"build \"{projectPath}\" -c {configuration} -f {tfm} -m:1 -p:BuildInParallel=false -p:EnableCoreMrtTooling=false -p:RestoreForce=true -p:UseSharedCompilation=false -p:BaseOutputPath={QuoteForCommandLine(buildPaths.BaseOutputPath)} -p:FloweryRuntimeIntermediateRoot={QuoteForCommandLine(buildPaths.IntermediateRoot)}",
                repoRoot,
                TimeSpan.FromMinutes(8));

            if (buildExitCode != 0)
            {
                Assert.Fail($"Runtime test build failed with exit code {buildExitCode}.");
            }

            var dllPath = await GetTargetPathAsync(projectPath, tfm, repoRoot, configuration, buildPaths);
            var exePath = Path.ChangeExtension(dllPath, ".exe");
            var useExe = File.Exists(exePath);

            var exitCode = await RunProcessAsync(
                useExe ? exePath : "dotnet",
                useExe
                    ? $"--runtime-tests \"{resultPath}\""
                    : $"\"{dllPath}\" --runtime-tests \"{resultPath}\"",
                repoRoot,
                TimeSpan.FromMinutes(8),
                new Dictionary<string, string>
                {
                    ["FLOWERY_RUNTIME_TESTS_PATH"] = resultPath
                });

            if (exitCode != 0)
            {
                Assert.Fail($"Runtime tests failed with exit code {exitCode}.");
            }

            if (!File.Exists(resultPath))
            {
                Assert.Fail($"Runtime tests did not produce results file at {resultPath}.");
            }
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Flowery.Uno.sln")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate repository root.");
        }

        private sealed class BuildLock : IDisposable
        {
            private readonly Mutex _mutex;

            public BuildLock(Mutex mutex)
            {
                _mutex = mutex;
            }

            public void Dispose()
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch
                {
                    // Ignore release failures.
                }

                _mutex.Dispose();
            }
        }

        private static BuildLock AcquireBuildLock(TimeSpan timeout)
        {
            var mutex = new Mutex(false, @"Local\Flowery.Uno.RuntimeTests.BuildLock");
            var entered = false;

            try
            {
                entered = mutex.WaitOne(timeout);
            }
            catch (AbandonedMutexException)
            {
                entered = true;
            }

            if (!entered)
            {
                mutex.Dispose();
                throw new TimeoutException("Timed out waiting for the runtime test build lock.");
            }

            return new BuildLock(mutex);
        }

        private sealed class BuildPaths
        {
            public BuildPaths(string baseOutputPath, string intermediateRoot)
            {
                BaseOutputPath = baseOutputPath;
                IntermediateRoot = intermediateRoot;
            }

            public string BaseOutputPath { get; }

            public string IntermediateRoot { get; }
        }

        private static BuildPaths CreateBuildPaths(string runDirectory, string targetFramework)
        {
            var root = Path.Combine(runDirectory, "runtime-build", targetFramework, Guid.NewGuid().ToString("N"));
            var baseOutputPath = Path.Combine(root, "bin") + Path.DirectorySeparatorChar;
            var intermediateRoot = Path.Combine(root, "obj") + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(baseOutputPath);
            Directory.CreateDirectory(intermediateRoot);

            return new BuildPaths(baseOutputPath, intermediateRoot);
        }

        private static string QuoteForCommandLine(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var escaped = value.Replace("\"", "\\\"");
            if (escaped.EndsWith("\\", StringComparison.Ordinal))
            {
                escaped += "\\";
            }

            return $"\"{escaped}\"";
        }

        private static async Task<int> RunProcessAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            TimeSpan timeout,
            IReadOnlyDictionary<string, string>? environmentVariables = null)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (environmentVariables != null)
            {
                foreach (var entry in environmentVariables)
                {
                    process.StartInfo.Environment[entry.Key] = entry.Value;
                }
            }

            process.StartInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
            process.StartInfo.Environment["DOTNET_CLI_DO_NOT_USE_PERSISTENT_BUILD_SERVER"] = "1";
            process.StartInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var exitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(exitTask, Task.Delay(timeout));
            if (completed != exitTask)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore process kill failures.
                }

                throw new TimeoutException("Runtime tests timed out.");
            }

            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine(error);
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }

            return process.ExitCode;
        }

        private static async Task<string> GetTargetPathAsync(
            string projectPath,
            string targetFramework,
            string workingDirectory,
            string configuration,
            BuildPaths buildPaths)
        {
            var arguments = $"msbuild \"{projectPath}\" -nologo -getProperty:TargetPath -p:Configuration={configuration} -p:TargetFramework={targetFramework} -p:EnableCoreMrtTooling=false -p:BaseOutputPath={QuoteForCommandLine(buildPaths.BaseOutputPath)} -p:FloweryRuntimeIntermediateRoot={QuoteForCommandLine(buildPaths.IntermediateRoot)}";
            var (exitCode, output, error) = await RunProcessCaptureAsync(
                "dotnet",
                arguments,
                workingDirectory,
                TimeSpan.FromMinutes(2));

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Failed to query TargetPath (exit code {exitCode}).");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine(error);
            }

            var targetPath = TryReadMsBuildProperty(output, "TargetPath");
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                var fallback = GetLastNonEmptyLine(output);
                if (string.IsNullOrWhiteSpace(fallback))
                {
                    throw new InvalidOperationException("Failed to read TargetPath from msbuild output.");
                }

                return fallback;
            }

            return targetPath;
        }

        private static string? TryReadMsBuildProperty(string output, string propertyName)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string? GetLastNonEmptyLine(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var trimmed = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    return trimmed.Trim('"');
                }
            }

            return null;
        }

        private static async Task<(int ExitCode, string Output, string Error)> RunProcessCaptureAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            TimeSpan timeout)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.StartInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
            process.StartInfo.Environment["DOTNET_CLI_DO_NOT_USE_PERSISTENT_BUILD_SERVER"] = "1";
            process.StartInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var exitTask = process.WaitForExitAsync();
            var completed = await Task.WhenAny(exitTask, Task.Delay(timeout));
            if (completed != exitTask)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore process kill failures.
                }

                throw new TimeoutException("Runtime tests timed out.");
            }

            var output = await outputTask;
            var error = await errorTask;

            return (process.ExitCode, output, error);
        }
    }
}
