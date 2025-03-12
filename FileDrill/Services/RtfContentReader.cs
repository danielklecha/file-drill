using HtmlAgilityPack;
using RtfPipe;
using System.Text;

namespace FileDrill.Services;
internal class RtfContentReader : IContentReader
{
    private readonly System.IO.Abstractions.IFileSystem _fileSystem;
    public RtfContentReader(System.IO.Abstractions.IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string[] Extensions { get; } = [".rtf"];

    public Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = _fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        var rtf = new RtfSource(reader);
        var html = Rtf.ToHtml(rtf);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var plainText = ExtractPlainText(doc.DocumentNode);
        return Task.FromResult<string?>(plainText);
    }

    private static string ExtractPlainText(HtmlNode node)
    {
        if (node == null)
            return string.Empty;
        if (node.NodeType == HtmlNodeType.Text)
            return HtmlEntity.DeEntitize(node.InnerText);
        var sb = new StringBuilder();
        foreach (var child in node.ChildNodes)
        {
            sb.Append(ExtractPlainText(child));
            if (child.Name == "p" || child.Name == "br")
                sb.Append("\r\n");
        }
        return sb.ToString();
    }
}
