using IOPath = System.IO.Path;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>
    /// A process to start: the file and its arguments, never a shell string.
    /// <see cref="RawArguments"/> replaces <see cref="Args"/> for the few Windows tools that parse their own command line.
    /// </summary>
    public sealed record CommandSpec(string File, params string[] Args)
    {
        public string? RawArguments { get; init; }

        public static CommandSpec Raw(string file, string arguments) => new(file) { RawArguments = arguments };

        public override string ToString() => RawArguments != null ? $"{File} {RawArguments}" : string.Join(' ', new[] { File }.Concat(Args));
    }

    public readonly record struct CommandResult(int ExitCode, string Output)
    {
        public bool Success => ExitCode == 0;
    }

    /// <summary>Seam over <see cref="Process"/> so command selection can be tested without running anything.</summary>
    public interface ICommandRunner
    {
        CommandResult Run(CommandSpec command, TimeSpan? timeout = null);

        /// <summary>Starts a process and does not wait for it.</summary>
        bool Start(CommandSpec command);

        /// <summary>True when the tool can be found (absolute path that exists, or a name on PATH).</summary>
        bool IsAvailable(string tool);
    }

    public sealed class ProcessCommandRunner : ICommandRunner
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        public CommandResult Run(CommandSpec command, TimeSpan? timeout = null)
        {
            try
            {
                using var process = Process.Start(CreateStartInfo(command, redirect: true));
                if (process == null)
                    return new CommandResult(-1, "process did not start");

                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit((int)(timeout ?? DefaultTimeout).TotalMilliseconds))
                {
                    process.Kill(entireProcessTree: true);
                    return new CommandResult(-1, "timeout");
                }

                // Streams are complete once the process exited.
                return new CommandResult(process.ExitCode, stdout.Result + stderr.Result);
            }
            catch (Exception ex)
            {
                return new CommandResult(-1, ex.Message);
            }
        }

        public bool Start(CommandSpec command)
        {
            try
            {
                using var process = Process.Start(CreateStartInfo(command, redirect: false));
                return process != null;
            }
            catch
            {
                return false;
            }
        }

        // Looking through PATH touches the disk for every directory; the answer does not change during a run.
        private readonly ConcurrentDictionary<string, bool> _available = new(StringComparer.Ordinal);

        public bool IsAvailable(string tool) => _available.GetOrAdd(tool, FindTool);

        private static bool FindTool(string tool)
        {
            if (IOPath.IsPathRooted(tool))
                return File.Exists(tool);

            var separator = OperatingSystem.IsWindows() ? ';' : ':';
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return path.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Any(dir => File.Exists(IOPath.Combine(dir, tool)));
        }

        private static ProcessStartInfo CreateStartInfo(CommandSpec command, bool redirect)
        {
            var info = new ProcessStartInfo(command.File)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = redirect,
                RedirectStandardError = redirect,
            };
            if (command.RawArguments != null)
            {
                info.Arguments = command.RawArguments;
                return info;
            }

            foreach (var arg in command.Args)
                info.ArgumentList.Add(arg);
            return info;
        }
    }
}
