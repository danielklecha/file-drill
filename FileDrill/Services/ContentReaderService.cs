using FileDrill.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileDrill.Services;

public class ContentReaderService(
    ILogger<ContentReaderService> logger,
    System.IO.Abstractions.IFileSystem fileSystem,
    IOptions<WritableOptions> options,
    IEnumerable<IContentReader> extractors) : IContentReaderService
{
    public async Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var extension = fileSystem.Path.GetExtension(path);
        try
        {
            var extractor = extractors.FirstOrDefault(x => x.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
            if (extractor is null)
            {
                logger.LogDebug("Unable to find extractor for this file type");
                return null;
            }
            var content = await extractor.GetContentAsync(path, cancellationToken);
            var optionsValue = options.Value;
            var limit = optionsValue.ContentReader?.Limit;
            if (limit.HasValue && content is not null && content.Length > limit.Value)
            {
                content = content[..limit.Value];
                logger.LogWarning("Content length exceeded the limit. Characters beyond the limit have been truncated.");
            }
            return content;
        }
        finally
        {
            watch.Stop();
            logger.LogDebug("Content extraction took {ms}ms", watch.ElapsedMilliseconds);
        }
    }
}
