using FileDrill.Models;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.Text.Json;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace FileDrill.Commands;
public class ConfigExportCommand : Command
{
    public ConfigExportCommand() : base("export", "Exports configuration to a file")
    {
        AddArgument(new Argument<string>("out", "output path"));
    }

    public new class Handler(
        ILogger<Handler> logger,
        IFileSystem fileSystem,
        IOptions<WritableOptions> options) : ICommandHandler
    {
        public string? Out { get; set; }
        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            if (string.IsNullOrEmpty(Out))
                throw new ArgumentNullException(nameof(Out));
            var serialized = JsonSerializer.Serialize(options.Value, new JsonSerializerOptions()
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });
            await fileSystem.File.WriteAllTextAsync(Out, serialized);
            logger.LogInformation("Configuration has been saved in {path}", Out);
            return 0;
        }
    }
}