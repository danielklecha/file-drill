
namespace FileDrill.Services;

public interface IContentReaderService
{
    Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default);
}