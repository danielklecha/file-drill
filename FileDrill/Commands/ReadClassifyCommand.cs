using FileDrill.Services;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.IO.Abstractions;

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
                var content = await contentExtractorService.GetContentAsync(filePath, cancelationToken);
                if (string.IsNullOrEmpty(content))
                {
                    logger.LogInformation("Content not detected");
                    return 0;
                }
                var schema = await contentClassifierService.ClassifyAsync(content, filePath, cancelationToken);
                if (string.IsNullOrEmpty(schema))
                {
                    logger.LogInformation("Content type not detected");
                    return 0;
                }
                logger.LogInformation("Content type: {content type}", schema);
                if (!string.IsNullOrEmpty(Out))
                {
                    fileSystem.File.WriteAllText(Out, schema);
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
