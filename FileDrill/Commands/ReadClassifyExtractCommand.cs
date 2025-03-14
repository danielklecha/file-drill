using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
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
internal class ReadClassifyExtractCommand : Command
{
    public ReadClassifyExtractCommand() : base("extract", "Extract fields from the file")
    {
        AddAlias("e");
        AddOption(new Option<string>("--file", "File path"));
        AddOption(new Option<string>("--out", "Output file path"));
    }

    public new class Handler(
        ILogger<Handler> logger,
        IFileSystem fileSystem,
        IAnsiConsole ansiConsole,
        IFileSystemDialogs fileSystemDialogs,
        IContentReaderService contentExtractorService,
        IContentClassifierService contentClassifierService,
        IFieldExtractorService fieldExtractorService) : ICommandHandler
    {
        public string? File { get; set; }
        public string? Schema { get; set; }
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
                    return 0;
                }
                var schema = await ansiConsole.Status().StartAsync("Classifying content...", ctx => contentClassifierService.ClassifyAsync(content, filePath, cancelationToken));
                if (string.IsNullOrEmpty(schema))
                {
                    logger.LogInformation("Content type not detected");
                    return 0;
                }
                logger.LogInformation("Content type: {content type}", schema);
                var fields = await ansiConsole.Status().StartAsync("Extractiong fields...", ctx => fieldExtractorService.ExtractFieldsAsync(content, schema, cancelationToken));
                var serialized = JsonSerializer.Serialize(fields, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                logger.LogInformation(serialized);
                if (!string.IsNullOrEmpty(Out))
                {
                    await fileSystem.File.AppendAllTextAsync(Out, serialized);
                    logger.LogInformation("Fields have been saved to {path}", Out);
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
