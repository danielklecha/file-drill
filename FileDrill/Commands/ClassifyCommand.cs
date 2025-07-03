using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Extensions;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDrill.Commands;
internal class ClassifyCommand : Command
{
    public ClassifyCommand() : base("classify", "Classify content of the file")
    {
        AddAlias("c");
        AddOption(new Option<string>("--file", "File path"));
        AddOption(new Option<string>("--out", "output path"));
        AddCommand(new ReadClassifyExtractCommand());
    }

    public new class Handler(
        ILogger<Handler> logger,
        IAnsiConsole ansiConsole,
        IFileSystem fileSystem,
        IFileSystemDialogs fileSystemDialogs,
        IContentClassifierService contentClassifierService) : ICommandHandler
    {
        public string? File { get; set; }

        public string? Out { get; set; }

        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var cancelationToken = context.GetCancellationToken();
            var filePath = File;
            if(string.IsNullOrEmpty(filePath))
                filePath = await fileSystemDialogs.OpenFileAsync();
            logger.LogDebug("File path: {path}", filePath);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                byte[] content = await ansiConsole.Status().StartAsync("Reading file...", ctx => fileSystem.File.ReadAllBytesAsync(filePath, cancelationToken));
                var schema = await ansiConsole.Status().StartAsync("Classifying content...", ctx => contentClassifierService.ClassifyAsync(content, filePath, cancelationToken));
                if (string.IsNullOrEmpty(schema))
                {
                    logger.LogInformation("Content type not detected");
                    return -1;
                }
                var data = new
                {
                    Schema = schema
                };
                var serialized = JsonSerializer.Serialize(data, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                logger.LogInformation(serialized);
                if (!string.IsNullOrEmpty(Out))
                {
                    fileSystem.File.WriteAllText(Out, serialized);
                    logger.LogInformation("Scehma has been saved to {path}", Out);
                }
            }
            finally
            {
                watch.Stop();
                logger.LogDebug("Total execution took {ms}ms", watch.ElapsedMilliseconds);
            }
            return 0;
        }
    }
}
