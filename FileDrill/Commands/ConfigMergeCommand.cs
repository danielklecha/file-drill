using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.IO.Abstractions;
using System.Text.Json;
using System.IO;

namespace FileDrill.Commands;
internal class ConfigMergeCommand : Command
{
    public ConfigMergeCommand() : base("merge", "Merges values from JSON, keeping existing ones")
    {
        AddArgument(new Argument<string>("json", "json file or inline json"));
        AddCommand(new ConfigMergeKeyCommand());
    }

    public new class Handler(
        ILogger<Handler> logger,
        IFileSystem fileSystem,
        IOptions<WritableOptions> options,
        IOptionsSync<WritableOptions> optionsSync) : ICommandHandler
    {
        public string? Json { get; set; }
        public IEnumerable<string>? Pairs { get; set; }

        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            if (string.IsNullOrEmpty(Json))
                throw new ArgumentNullException(nameof(Json));
            WritableOptions optionsValue = options.Value;
            if (fileSystem.Path.Exists(Json))
            {
                try
                {
                    using FileSystemStream stream = fileSystem.File.OpenRead(Json);
                    using JsonDocument jsonDocument = JsonDocument.Parse(stream);
                    optionsValue = optionsSync.Merge(optionsValue, jsonDocument);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to process file");
                    return -1;
                }
            }
            else
            {
                try
                {
                    using JsonDocument jsonDocument = JsonDocument.Parse(Json);
                    optionsValue = optionsSync.Merge(optionsValue, jsonDocument);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to process inline json");
                }
            }
            await optionsSync.SyncAsync(optionsValue);
            await optionsSync.SaveAsync();
            logger.LogInformation("Options were updated");
            return 0;
        }
    }
}
