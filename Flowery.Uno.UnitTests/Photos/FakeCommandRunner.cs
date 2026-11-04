using Flowery.Uno.Photos.Services.Platform;

namespace Flowery.Uno.UnitTests.Photos
{
    /// <summary>Records the commands and answers from a script; nothing is started.</summary>
    internal sealed class FakeCommandRunner : ICommandRunner
    {
        private readonly Dictionary<string, CommandResult> _results = [];
        private readonly HashSet<string> _tools;

        public FakeCommandRunner(params string[] availableTools) => _tools = new(availableTools, StringComparer.Ordinal);

        public List<CommandSpec> Ran { get; } = [];

        public List<CommandSpec> Started { get; } = [];

        public bool StartSucceeds { get; set; } = true;

        public FakeCommandRunner Returns(string command, int exitCode, string output = "")
        {
            _results[command] = new CommandResult(exitCode, output);
            return this;
        }

        public CommandResult Run(CommandSpec command, TimeSpan? timeout = null)
        {
            Ran.Add(command);
            return _results.TryGetValue(command.ToString(), out var result) ? result : new CommandResult(0, string.Empty);
        }

        public bool Start(CommandSpec command)
        {
            Started.Add(command);
            return StartSucceeds;
        }

        public bool IsAvailable(string tool) => _tools.Contains(tool);
    }
}
