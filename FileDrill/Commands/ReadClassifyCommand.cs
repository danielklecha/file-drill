using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Text.Json;

namespace FileDrill.Commands;
internal class ReadClassifyCommand : Command
{
    public ReadClassifyCommand() : base("classify", "Classify content of the file")
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
        IContentReaderService contentExtractorService,
        IContentClassifierService contentClassifierService) : ICommandHandler
    {
        public string? File { get; set; }
        public string? Out { get; set; }

        public int Invoke(InvocationContext context) => 0;
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var cancelationToken = context.GetCancellationToken();
            var filePath = File;
            if (string.IsNullOrEmpty(filePath))
                filePath = await fileSystemDialogs.OpenFileAsync();
            logger.LogDebug("File path: {path}", filePath);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var content = await ansiConsole.Status().StartAsync("Reading file...", ctx => contentExtractorService.GetContentAsync(filePath, cancelationToken));
                if (string.IsNullOrEmpty(content))
                {
                    logger.LogInformation("Content not detected");
                    return -1;
                }
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
