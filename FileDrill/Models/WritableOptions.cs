namespace FileDrill.Models;
public class WritableOptions
{
    public string? FallbackAIService { get; set; }
    public Dictionary<string, ChatClientOptions>? AIServices { get; set; }
    public ContentReaderOptions? ContentReader { get; set; }
    public ContentClassifierOptions? ContentClassifier { get; set; }
    public FieldExtractorOptions? FieldExtractor { get; set; }
    public Dictionary<string, SchemaOptions>? Schemas { get; set; }
}
