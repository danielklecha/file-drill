using FileDrill.Models;
using HeyRed.Mime;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;

namespace FileDrill.Services;
internal class ImageContentReader(
    IChatClientFactory chatClientFactory,
    IFileSystem fileSystem,
    IOptions<WritableOptions> options,
    ILogger<ImageContentReader> logger) : IContentReader
{
    public string[] Extensions { get; } = [".png", ".jpeg"];

    public async Task<string?> GetContentAsync(string path, CancellationToken cancellationToken = default)
    {
        return await GetContentAsync(await fileSystem.File.ReadAllBytesAsync(path, cancellationToken), path, cancellationToken);
    }

    public async Task<string?> GetContentAsync(byte[] bytes, string path, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = chatClientFactory.CreateClient(options.Value.ContentReader?.AIService);
        var base64Image = Convert.ToBase64String(bytes);
        string mimeType = MimeTypesMap.GetMimeType(Path.GetFileName(path));
        var dataUrl = $"data:{mimeType};base64,{base64Image}";
        StringBuilder sb = new();
        sb
            .AppendLine("Extract and return only the text content from the attached image.")
            .AppendLine("- Return a plain text string.")
            .AppendLine("- Do not include metadata, comments, or explanations.")
            .AppendLine("- If no text is found, return an empty string (\"\").")
            .AppendLine("- Maintain the original text order.");
        var response = await chatClient.CompleteAsync([
            new(ChatRole.User, [
                new TextContent(sb.ToString()),
                new ImageContent(dataUrl)
            ])], cancellationToken: cancellationToken);
        logger.LogDebug("Input token count: {InputTokenCount}", response.Usage?.InputTokenCount);
        logger.LogDebug("Output token count: {OutputTokenCount}", response.Usage?.OutputTokenCount);
        if (string.IsNullOrEmpty(response.Message.Text))
        {
            logger.LogError("Response is empty");
            return null;
        }
        var responseMessage = Regex.Replace(response.Message.Text, @"^\s*```plaintext\s*|\s*```\s*$", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(300));
        return responseMessage;
    }
}
