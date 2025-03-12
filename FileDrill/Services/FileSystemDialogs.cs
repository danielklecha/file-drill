using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
public class FileSystemDialogs(
    IAnsiConsole ansiConsole,
    IFileSystem fileSystem) : IFileSystemDialogs
{
    public async Task<string> OpenFileAsync(string filter = "*.*")
    {
        var currentDirectory = fileSystem.Directory.GetCurrentDirectory();
        var previousDirectory = currentDirectory;
        while (true)
        {
            try
            {
                IEnumerable<(bool IsDirectory, string Value, string? Path)> entries = currentDirectory == null
                    ? fileSystem.Directory.GetLogicalDrives().Select(x => (true, $"📁 {x}", (string?)x))
                    : Enumerable.Repeat((true, $"🔼 ..", fileSystem.Directory.GetParent(currentDirectory)?.FullName), 1)
                        .Concat(fileSystem.Directory.GetDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly).Select(x => (true, $"📁 {x[currentDirectory.Length..].TrimStart('\\')}", (string?)x)))
                        .Concat(fileSystem.Directory.GetFiles(currentDirectory, filter, SearchOption.TopDirectoryOnly).Select(x => (false, $"📄 {x[currentDirectory.Length..].TrimStart('\\')}", (string?)x)));
                var entry = await new SelectionPrompt<(bool IsDirectory, string Value, string? Path)>()
                    .Title(currentDirectory == null ? "Select drive" : $"Select file ({currentDirectory})")
                    .UseConverter(x => x.Value)
                    .AddChoices(entries)
                    .ShowAsync(ansiConsole, CancellationToken.None);
                if (entry.IsDirectory)
                {
                    previousDirectory = currentDirectory;
                    currentDirectory = entry.Path;
                    continue;
                }
                return entry.Path;
            }
            catch (Exception ex)
            {
                if (currentDirectory == previousDirectory)
                    throw;
                ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                currentDirectory = previousDirectory;
            }
        }
    }
}
