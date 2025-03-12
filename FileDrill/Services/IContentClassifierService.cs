
namespace FileDrill.Services;

public interface IContentClassifierService
{
    Task<string?> ClassifyAsync(string content, string path, CancellationToken cancellationToken);
    Task<string?> ClassifyAsync(byte[] content, string path, CancellationToken cancellationToken);
}