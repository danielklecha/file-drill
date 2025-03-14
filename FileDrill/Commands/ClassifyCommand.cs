using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console.Extensions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Commands;
internal class ClassifyCommand : Command
{
    public ClassifyCommand() : base("classify", "Classify content of the file")
    {
        AddAlias("c");
        AddOption(new Option<string>("--file", "File path"));
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
                    return 0;
                }
                logger.LogInformation("Content type: {content type}", schema);
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
