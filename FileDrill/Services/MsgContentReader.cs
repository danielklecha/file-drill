using System.Globalization;
using System.Text;

namespace FileDrill.Services;

public class MsgContentReader(System.IO.Abstractions.IFileSystem fileSystem) : IContentReader
{
    public string[] Extensions { get; } = [".msg"];

    public Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read);
        var eml = MsgReader.Mime.Message.Load(stream);
        var sb = new StringBuilder();
        if(eml.Headers is not null)
        {
            if (eml.Headers.Sender is not null)
                sb.Append("Sender: ").AppendLine(eml.Headers.Sender.ToString());
            if (eml.Headers.Date is not null)
                sb.Append("Sender: ").AppendLine(eml.Headers.DateSent.ToString(CultureInfo.InvariantCulture));
            if (eml.Headers.To is not null)
                sb.Append("To: ").AppendLine(eml.Headers.To.ToString());
            if (eml.Headers.Cc is not null)
                sb.Append("CC: ").AppendLine(eml.Headers.Cc.ToString());
            if (eml.Headers.Bcc is not null)
                sb.Append("BCC: ").AppendLine(eml.Headers.Bcc.ToString());
            if (eml.Headers.Subject is not null)
                sb.Append("Subject: ").AppendLine(eml.Headers.Subject);
        }
        if (eml.TextBody is not null)
        {
            sb.Append("Body: ").AppendLine(Encoding.UTF8.GetString(eml.TextBody.Body));
        }
        return Task.FromResult<string?>(sb.ToString());
    }
}
