using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using FileDrill.Extensions;
using System.IO.Abstractions;

namespace FileDrill.Commands;
internal class ConfigSetKeyCommand : Command
{
    public ConfigSetKeyCommand() : base("key")
    {
        AddArgument(new Argument<string>("key", "Key name"));
        AddArgument(new Argument<string>("value", "serialized value"));
    }

    public new class Handler(
        ILogger<Handler> logger,
        IOptions<WritableOptions> options,
        IFileSystem fileSystem,
        IOptionsSync<WritableOptions> optionsSync) : ICommandHandler
    {
        public string? Key { get; set; }

        public string? Value { get; set; }

        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            WritableOptions optionsValue = options.Value;
            if (string.IsNullOrEmpty(Key))
                throw new ArgumentNullException("key");
            if (Value is null)
                throw new ArgumentNullException("value");
            if (fileSystem.Path.Exists(Value))
            {
                try
                {
                    using FileSystemStream stream = fileSystem.File.OpenRead(Value);
                    using JsonDocument jsonDocument = JsonDocument.Parse(stream);
                    //using var emptyJsonDocument = JsonDocument.Parse("null");
                    using var nestedJsonDocument = jsonDocument.ToNestedJsonDocument(Key);
                    //using var emptyNestedJsonDocument = emptyJsonDocument.ToNestedJsonDocument(Key);
                    //optionsSync.Bind(optionsValue, emptyNestedJsonDocument);
                    optionsSync.Bind(optionsValue, nestedJsonDocument);
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
                    using var jsonDocument = JsonDocument.Parse(Value);
                    //var emptyJsonDocument = JsonDocument.Parse("null");
                    using var nestedJsonDocument = jsonDocument.ToNestedJsonDocument(Key);
                    //using var emptyNestedJsonDocument = emptyJsonDocument.ToNestedJsonDocument(Key);
                    //optionsSync.Bind(optionsValue, emptyNestedJsonDocument);
                    optionsSync.Bind(optionsValue, nestedJsonDocument);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to process inline json");
                    return -1;
                }
            }
            await optionsSync.SyncAsync(optionsValue);
            await optionsSync.SaveAsync();
            logger.LogInformation("Option {name} was updated", Key);
            return 0;
        }
    }
}