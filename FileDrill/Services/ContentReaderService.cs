using Microsoft.Extensions.Logging;

namespace FileDrill.Services;
public class ContentReaderService(
    ILogger<ContentReaderService> logger,
    System.IO.Abstractions.IFileSystem fileSystem,
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
            return await extractor.GetContentAsync(path, cancellationToken);
        }
        finally
        {
            watch.Stop();
            logger.LogDebug("Content extraction took {ms}ms", watch.ElapsedMilliseconds);
        }
    }
}
