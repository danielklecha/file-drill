
namespace FileDrill.Services;

public interface IFieldExtractorService
{
    Task<Dictionary<string, object?>?> ExtractFieldsAsync(string content, string contentType, CancellationToken cancellationToken);
}