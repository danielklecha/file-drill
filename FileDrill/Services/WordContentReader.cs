using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
internal class WordContentReader(System.IO.Abstractions.IFileSystem fileSystem) : IContentReader
{
    public string[] Extensions { get; } = [".docx", ".dotx", ".docm", ".dotm"];

    public Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = fileSystem.File.OpenRead(path);
        using var wordDocument = WordprocessingDocument.Open(stream, false);
        var body = wordDocument.MainDocumentPart?.Document.Body;
        if (body is null)
            return Task.FromResult<string?>(null);
        StringBuilder sb = new();
        foreach (var text in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            sb.AppendLine(text.Text);
        return Task.FromResult<string?>(sb.ToString());
    }
}
