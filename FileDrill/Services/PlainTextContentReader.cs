namespace FileDrill.Services;
internal class PlainTextContentReader(
    System.IO.Abstractions.IFileSystem fileSystem) : IContentReader
{
    public string[] Extensions { get; } = [".txt", ".md"];

    public async Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        return await fileSystem.File.ReadAllTextAsync(path, cancellationToken);
    }
}
