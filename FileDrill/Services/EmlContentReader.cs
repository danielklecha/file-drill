using System.Globalization;
using System.Text;

namespace FileDrill.Services;

public class EmlContentReader(System.IO.Abstractions.IFileSystem fileSystem) : IContentReader
{
    public string[] Extensions { get; } = [".eml"];

    public Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        using var stream = fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read);
        using var msg = new MsgReader.Outlook.Storage.Message(stream);
        var sb = new StringBuilder()
            .Append("Sender: ")
            .AppendLine(msg.GetEmailSender(false, false))
            .Append("Date: ")
            .AppendLine(msg.SentOn?.ToString(CultureInfo.InvariantCulture))
            .Append("To: ")
            .AppendLine(msg.GetEmailRecipients(MsgReader.Outlook.RecipientType.To, false, false))
            .Append("CC: ")
            .AppendLine(msg.GetEmailRecipients(MsgReader.Outlook.RecipientType.Cc, false, false))
            .Append("BCC: ")
            .AppendLine(msg.GetEmailRecipients(MsgReader.Outlook.RecipientType.Bcc, false, false))
            .Append("Subject: ")
            .AppendLine(msg.Subject)
            .Append("Body: ")
            .AppendLine(msg.BodyText);
        return Task.FromResult<string?>(sb.ToString());
    }
}
