using FileDrill.Models;
using Azure.AI.Inference;
using Azure;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Mscc.GenerativeAI.Microsoft;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace FileDrill.Services;
public class ChatClientFactory(
    ILogger<ChatClientFactory> logger,
    IOptions<WritableOptions> options) : IChatClientFactory
{
    public IChatClient CreateClient(string? serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            serviceName = options.Value.FallbackAIService ?? throw new Exception("FallbackAIService is not set");
        if (!(options.Value.AIServices?.TryGetValue(serviceName, out var chatClientOptions) ?? false))
            throw new Exception($"Unable to find {serviceName}");
        logger.LogDebug("{name} AI service was used", serviceName);
        return chatClientOptions.Type switch
        {
            ChatClientType.Ollama => CreateOllamaClient(chatClientOptions),
            ChatClientType.Azure => CreateAzureClient(chatClientOptions),
            ChatClientType.Gemini => CreateGeminiClient(chatClientOptions),
            ChatClientType.OpenAI => CreateOpenAIClient(chatClientOptions),
            _ => throw new NotImplementedException(),
        };
    }

    private static OllamaApiClient CreateOllamaClient(ChatClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Url, nameof(ChatClientOptions.Url));
        ArgumentNullException.ThrowIfNull(options.ModelName, nameof(ChatClientOptions.ModelName));
        OllamaApiClient ollamaApiClient = new(new Uri(options.Url))
        {
            SelectedModel = options.ModelName
        };
        return ollamaApiClient;
    }

    private static IChatClient CreateAzureClient(ChatClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Url, nameof(ChatClientOptions.Url));
        ArgumentNullException.ThrowIfNull(options.ModelName, nameof(ChatClientOptions.ModelName));
        ArgumentNullException.ThrowIfNull(options.Key, nameof(ChatClientOptions.Key));
        ChatCompletionsClient chatCompletionsClient = new(new Uri(options.Url), new AzureKeyCredential(options.Key));
        var chatClient = chatCompletionsClient.AsChatClient(options.ModelName);
        return chatClient;
    }

    private static GeminiChatClient CreateGeminiClient(ChatClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.ModelName, nameof(ChatClientOptions.ModelName));
        ArgumentNullException.ThrowIfNull(options.Key, nameof(ChatClientOptions.Key));
        GeminiChatClient geminiChatClient = new(options.Key, options.ModelName);
        return geminiChatClient;
    }

    private static IChatClient CreateOpenAIClient(ChatClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Url, nameof(ChatClientOptions.Url));
        ArgumentNullException.ThrowIfNull(options.ModelName, nameof(ChatClientOptions.ModelName));
        ArgumentNullException.ThrowIfNull(options.Key, nameof(ChatClientOptions.Key));
        OpenAIClient chatCompletionsClient = new(options.Key);
        var chatClient = chatCompletionsClient.AsChatClient(options.ModelName);
        return chatClient;
    }
}
