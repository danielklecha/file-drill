namespace FileDrill.Models;

public class SchemaOptions
{
    public string? Description { get; set; }
    public Dictionary<string, FieldOptions>? Fields { get; set; }
}
