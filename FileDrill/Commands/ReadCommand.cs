using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
internal class ReadCommand : Command
{
    public ReadCommand() : base("read", "Read content of the file")
    {
        AddAlias("r");
        AddOption(new Option<string>("--file", "File path"));
        AddOption(new Option<string>("--out", "Output file path"));
        AddCommand(new ReadClassifyCommand());
        AddCommand(new ReadExtractCommand());
    }

    public new class Handler(
        ILogger<Handler> logger,
        IFileSystem fileSystem,
        IFileSystemDialogs fileSystemDialogs,
        IContentReaderService contentExtractorService) : ICommandHandler
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
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var content = await contentExtractorService.GetContentAsync(filePath, cancelationToken);
                if (string.IsNullOrEmpty(content))
                {
                    logger.LogInformation("Content not detected");
                    return 0;
                }
                if (!string.IsNullOrEmpty(Out))
                {
                    fileSystem.File.WriteAllText(Out, content);
                    logger.LogInformation("Content has been saved to {path}", Out);
                }
                else
                {
                    logger.LogInformation(content);
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
