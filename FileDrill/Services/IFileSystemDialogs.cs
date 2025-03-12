
namespace FileDrill.Services;

public interface IFileSystemDialogs
{
    Task<string> OpenFileAsync(string filter = "*.*");
}