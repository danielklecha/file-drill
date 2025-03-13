using FileDrill.Models;
using HeyRed.Mime;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FileDrill.Services;
public class ContentClassifierService(
    IChatClientFactory chatClientFactory,
    IOptions<WritableOptions> options,
    ILogger<ContentClassifierService> logger) : IContentClassifierService
{
    public async Task<string?> ClassifyAsync(string content, string path, CancellationToken cancellationToken)
    {
        var schemas = options.Value.Schemas;
        if(schemas is null || schemas.Count == 0)
        {
            logger.LogDebug("List of schemas is empty");
            return null;
        }
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var fileName = Path.GetFileName(path);
        IChatClient chatClient = chatClientFactory.CreateClient(options.Value.ContentClassifier?.AIService);
        StringBuilder sb = new();
        sb.AppendLine("Classify the following document into one of the predefined content types.");
        if (!string.IsNullOrEmpty(fileName))
            sb.AppendLine($"File is named: {fileName}");
        sb.AppendLine("Use the attached JSON schema, which includes available types and descriptions:");
        sb.AppendLine(JsonSerializer.Serialize(schemas.ToDictionary(x => x.Key, x => x.Value.Description)));
        sb.AppendLine("Return a JSON object with the following structure:");
        sb.AppendLine(@"{");
        sb.AppendLine(@"  ""Result"": ""<selected content type>"",");
        sb.AppendLine(@"  ""Explanation"": ""<explanation of why this type was chosen>"",");
        sb.AppendLine(@"  ""ConfidenceScore"": <integer value>");
        sb.AppendLine(@"}");
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Ensure the selected content type exists in the provided schema.");
        sb.AppendLine("- Provide a clear and logical explanation.");
        sb.AppendLine("- ConfidenceScore should be an integer between 0 and 100.");
        sb.AppendLine("Document content:");
        sb.AppendLine(content);
        try
        {
            var response = await chatClient.GetResponseAsync(sb.ToString(), new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json
            }, cancellationToken: cancellationToken);
            logger.LogDebug("Input token count: {InputTokenCount}", response.Usage?.InputTokenCount);
            logger.LogDebug("Output token count: {OutputTokenCount}", response.Usage?.OutputTokenCount);
            if (string.IsNullOrEmpty(response.Text))
            {
                logger.LogError("Response is empty");
                return null;
            }
            var responseMessage = Regex.Replace(response.Text, @"^\s*```json\s*|\s*```\s*$", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(300));
            var jsonDocument = JsonDocument.Parse(responseMessage);
            var result = jsonDocument.RootElement.GetProperty("Result").GetString();
            if (jsonDocument.RootElement.TryGetProperty("Explanation", out JsonElement explanationJsonElement))
            {
                logger.LogDebug("Explanation: {Explanation}", explanationJsonElement.ToString());
            }
            if (jsonDocument.RootElement.TryGetProperty("ConfidenceScore", out JsonElement confidenceScoreJsonElement))
            {
                logger.LogDebug("Confidence score: {ConfidenceScore}", confidenceScoreJsonElement.ToString());
            }
            if (result is null)
            {
                return null;
            }
            var matchedKey = schemas.Keys.FirstOrDefault(k => k.Equals(result, StringComparison.OrdinalIgnoreCase));
            if(matchedKey is null)
            {
                logger.LogDebug("Response does not exist in schema list.");
            }
            return matchedKey;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Unable to parse response");
            return null;
        }
        finally
        {
            watch.Stop();
            logger.LogDebug("Content classification took {ms}ms", watch.ElapsedMilliseconds);
        }
    }

    public async Task<string?> ClassifyAsync(byte[] content, string path, CancellationToken cancellationToken)
    {
        var schemas = options.Value.Schemas;
        if (schemas is null)
            return null;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var fileName = Path.GetFileName(path);
        var base64 = Convert.ToBase64String(content);
        string mimeType = MimeTypesMap.GetMimeType(Path.GetFileName(path));
        var dataUrl = $"data:{mimeType};base64,{base64}";
        IChatClient chatClient = chatClientFactory.CreateClient(options.Value.ContentClassifier?.AIService);
        StringBuilder sb = new();
        sb.AppendLine("Classify the following document into one of the predefined content types.");
        if(!string.IsNullOrEmpty(fileName))
            sb.AppendLine($"File is named: {fileName}");
        sb.AppendLine("Use the attached JSON schema, which includes available types and descriptions:");
        sb.AppendLine(JsonSerializer.Serialize(schemas));
        sb.AppendLine("Return a JSON object with the following structure:");
        sb.AppendLine(@"{");
        sb.AppendLine(@"  ""Result"": ""<selected content type>"",");
        sb.AppendLine(@"  ""Explanation"": ""<explanation of why this type was chosen>"",");
        sb.AppendLine(@"  ""ConfidenceScore"": <integer value>");
        sb.AppendLine(@"}");
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Ensure the selected content type exists in the provided schema.");
        sb.AppendLine("- Provide a clear and logical explanation.");
        sb.AppendLine("- ConfidenceScore should be an integer between 0 and 100.");
        try
        {
            List<AIContent> aiContents = [
                new TextContent(sb.ToString()),
                new DataContent(dataUrl, mimeType)
            ];
            var response = await chatClient.GetResponseAsync([new(ChatRole.User, aiContents)], new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json
            },  cancellationToken: cancellationToken);
            logger.LogDebug("Input token count: {InputTokenCount}", response.Usage?.InputTokenCount);
            logger.LogDebug("Output token count: {OutputTokenCount}", response.Usage?.OutputTokenCount);
            if (string.IsNullOrEmpty(response.Text))
            {
                logger.LogError("Response is empty");
                return null;
            }
            var responseMessage = Regex.Replace(response.Text, @"^\s*```json\s*|\s*```\s*$", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(300));
            var jsonDocument = JsonDocument.Parse(responseMessage);
            var result = jsonDocument.RootElement.GetProperty("Result").GetString();
            if (jsonDocument.RootElement.TryGetProperty("Explanation", out JsonElement explanationJsonElement))
            {
                logger.LogDebug("Explanation: {Explanation}", explanationJsonElement.ToString());
            }
            if (jsonDocument.RootElement.TryGetProperty("ConfidenceScore", out JsonElement confidenceScoreJsonElement))
            {
                logger.LogDebug("Confidence score: {ConfidenceScore}", confidenceScoreJsonElement.ToString());
            }
            if (result is null)
            {
                logger.LogError("Result is empty");
                return null;
            }
            var matchedKey = schemas.Keys.FirstOrDefault(k => k.Equals(result, StringComparison.OrdinalIgnoreCase));
            return matchedKey;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Unable to parse response");
            return null;
        }
        finally
        {
            watch.Stop();
            logger.LogDebug("Content classification took {ms}ms", watch.ElapsedMilliseconds);
        }
    }
}
