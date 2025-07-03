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
internal class ReadExtractCommand : Command
{
    public ReadExtractCommand() : base("extract", "Extract fields from the file")
    {
        AddAlias("e");
        AddArgument(new Argument<string>("schema", "Schema"));
        AddOption(new Option<string>("--file", "File path"));
        AddOption(new Option<string>("--out", "Output file path"));
    }

    public new class Handler(
        ILogger<Handler> logger,
        IAnsiConsole ansiConsole,
        IFileSystem fileSystem,
        IFileSystemDialogs fileSystemDialogs,
        IContentReaderService contentExtractorService,
        IFieldExtractorService fieldExtractorService) : ICommandHandler
    {
        public string? File { get; set; }
        public string? Schema { get; set; }
        public string? Out { get; set; }

        public int Invoke(InvocationContext context) => 0;
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            if (string.IsNullOrEmpty(Schema))
                throw new Exception("Schema is not set");
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
                    logger.LogError("Extracted content is empty");
                    return -1;
                }   
                var fields = await ansiConsole.Status().StartAsync("Extractiong fields...", ctx => fieldExtractorService.ExtractFieldsAsync(content, Schema, cancelationToken));
                if (fields is null)
                {
                    logger.LogError("Extracted field list is empty");
                    return -1;
                }   
                var serialized = JsonSerializer.Serialize(fields, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                if (!string.IsNullOrEmpty(Out))
                {
                    await fileSystem.File.WriteAllTextAsync(Out, serialized);
                    logger.LogInformation("Fields have been saved to {path}", Out);
                }
                else
                {
                    logger.LogInformation(serialized);
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
