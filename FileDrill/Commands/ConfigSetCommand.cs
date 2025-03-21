using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileDrill.Commands;
internal class ConfigSetCommand : Command
{
    public ConfigSetCommand() : base("set", "Replaces configuration")
    {
        AddArgument(new Argument<string>("json", "json file or inline json"));
        AddCommand(new ConfigSetKeyCommand());
        AddCommand(new ConfigSetWizardCommand());
    }

    public new class Handler(
        ILogger<Handler> logger,
        IFileSystem fileSystem,
        IOptionsSync<WritableOptions> optionsSync) : ICommandHandler
    {
        public string? Json { get; set; }

        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            if (string.IsNullOrEmpty(Json))
                throw new ArgumentNullException(nameof(Json));
            if (fileSystem.Path.Exists(Json))
            {
                try
                {
                    using FileSystemStream stream = fileSystem.File.OpenRead(Json);
                    JsonSerializerOptions jsonSerializerOptions = new()
                    {
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    };
                    var newOptions = JsonSerializer.Deserialize<WritableOptions>(stream, jsonSerializerOptions) ?? throw new Exception("New options are null");
                    await optionsSync.SyncAsync(newOptions);
                    await optionsSync.SaveAsync();
                    logger.LogInformation("Options were updated");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to process file");
                }
            }
            else
            {
                try
                {
                    var newOptions = JsonSerializer.Deserialize<WritableOptions>(Json) ?? throw new Exception("New options are null");
                    await optionsSync.SyncAsync(newOptions);
                    await optionsSync.SaveAsync();
                    logger.LogInformation("Options were updated");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to process inline json");
                }
            }
            return 0;
        }
    }
}
