
namespace FileDrill.Services;

public interface IContentReader
{
    string[] Extensions { get; }
    Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default);
    
    //bool IsExtensionSupported(string extension);
}