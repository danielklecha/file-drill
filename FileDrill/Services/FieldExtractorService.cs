using FileDrill.Extensions;
using FileDrill.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FileDrill.Services;
public class FieldExtractorService(
    IChatClientFactory chatClientFactory,
    IOptions<WritableOptions> options,
    ILogger<FieldExtractorService> logger) : IFieldExtractorService
{
    protected JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Dictionary<string, object?>?> ExtractFieldsAsync(string content, string schemaName, CancellationToken cancellationToken)
    {
        var optionsValue = options.Value;
        //todo: case-insensitive
        if (optionsValue.Schemas is null || !optionsValue.Schemas.TryGetValue(schemaName, out var schema))
            return null;
        if(schema is null || schema.Fields is null || schema.Fields.Count == 0)
            return null;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        IChatClient chatClient = chatClientFactory.CreateClient(options.Value.FieldExtractor?.AIService);
        StringBuilder sb = new();
        /*
        sb.AppendLine("Please extract the values of the specified fields from the following document.");
        sb.AppendLine("The attached JSON contains the available fields, their descriptions, and types:");
        sb.AppendLine(JsonSerializer.Serialize(fields, JsonSerializerOptions));
        sb.AppendLine("Return a JSON object with the following structure:");
        sb.AppendLine(@"{");
        var fieldKeys = fields.Keys.ToList();
        for (int i = 0; i < fieldKeys.Count; i++)
        {
            var fieldKey = fieldKeys[i];
            sb.Append($@"  ""{fieldKeys[i]}"": ");
            switch (fields[fieldKey].Type)
            {
                case FieldType.Int32:
                    sb.Append("<integer (e.g., 1234) or null if empty, without any percentage or currency signs>");
                    break;
                case FieldType.Float:
                    sb.Append("<float (e.g., 1234.56) or null if empty, without any percentage or currency signs>");
                    break;
                case FieldType.Double:
                    sb.Append("<double (e.g., 1234.56) or null if empty, without any percentage or currency signs>");
                    break;
                case FieldType.Decimal:
                    sb.Append("<decimal (e.g., 1234.56) or null if empty, without any percentage or currency signs>");
                    break;
                case FieldType.String:
                    sb.Append(@"""<string (e.g., ""example"")>""");
                    break;
                case FieldType.DateTime:
                    sb.Append(@"""<dateTime (format: yyyy-MM-ddTHH:mm:ss, e.g., ""2023-01-01T12:00:00"") or null if empty>""");
                    break;
                case FieldType.Bool:
                    sb.Append("<boolean (e.g., true) or null if empty>");
                    break;
            }
            sb.AppendLine(i != fieldKeys.Count - 1 ? "," : "");
        }
        sb.AppendLine(@"}");
        sb.AppendLine("Document content:");
        sb.AppendLine(content);
        */
        sb.AppendLine("Extract the values of the specified fields from the following document.");
        sb.AppendLine("The attached JSON defines the available fields, including descriptions and data types:");
        sb.AppendLine(JsonSerializer.Serialize(schema.Fields, JsonSerializerOptions));
        sb.AppendLine("Return a JSON object with the extracted values in the following structure:");
        sb.AppendLine(@"{");
        var fieldKeys = schema.Fields.Keys.ToList() ?? [];
        for (int i = 0; i < fieldKeys.Count; i++)
        {
            var fieldKey = fieldKeys[i];
            sb.Append($@"  ""{fieldKey}"": ");

            switch (schema.Fields[fieldKey].Type)
            {
                case FieldType.Int16:
                case FieldType.UInt16:
                case FieldType.Int32:
                case FieldType.UInt32:
                case FieldType.Int64:
                case FieldType.UInt64:
                    sb.Append("<integer>");
                    break;
                case FieldType.Float:
                case FieldType.Double:
                case FieldType.Decimal:
                    sb.Append("<number>");
                    break;
                case FieldType.String:
                    sb.Append(@"""<string>""");
                    break;
                case FieldType.DateTime:
                    sb.Append(@"""<dateTime>""");
                    break;
                case FieldType.Bool:
                    sb.Append("<boolean>");
                    break;
            }
            sb.AppendLine(i != fieldKeys.Count - 1 ? "," : "");
        }
        sb.AppendLine(@"}");
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Ensure values strictly match the expected types.");
        sb.AppendLine("- Exclude percentage signs, currency symbols, or extraneous characters from numeric fields.");
        sb.AppendLine("- Return `null` if a field is missing or empty.");
        sb.AppendLine("- Integer values must be whole numbers (e.g., `1234`).");
        sb.AppendLine("- Number values should be numeric (e.g., `1234.56`).");
        sb.AppendLine("- String values should be enclosed in double quotes (e.g., `\"example\"`).");
        sb.AppendLine("- DateTime values must follow the format `yyyy-MM-ddTHH:mm:ss` (e.g., `\"2023-01-01T12:00:00\"`).");
        sb.AppendLine("- Boolean values should be `true` or `false`.");
        sb.AppendLine("Document content:");
        sb.AppendLine(content);
        try
        {
            var response = await chatClient.CompleteAsync(sb.ToString(), new ChatOptions
            {
                ResponseFormat = ChatResponseFormat.Json
            }, cancellationToken);
            logger.LogDebug("Input token count: {InputTokenCount}", response.Usage?.InputTokenCount);
            logger.LogDebug("Output token count: {OutputTokenCount}", response.Usage?.OutputTokenCount);
            if (string.IsNullOrEmpty(response.Message.Text))
                return null;
            var responseMessage = Regex.Replace(response.Message.Text, @"^\s*```json\s*|\s*```\s*$", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(300));
            var jsonDocument = JsonDocument.Parse(responseMessage);
            var result = new Dictionary<string, object?>();
            foreach (var fieldKey in fieldKeys)
            {
                result[fieldKey] = GetFieldValue(jsonDocument.RootElement, fieldKey, schema.Fields[fieldKey]);
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to extract fields");
            return null;
        }
        finally
        {
            watch.Stop();
            logger.LogDebug("Fields extraction took {ms}ms", watch.ElapsedMilliseconds);
        }
    }

    private object? GetFieldValue(JsonElement parent, string fieldName, FieldOptions field)
    {
        var property = parent.EnumerateObject().FirstOrDefault(p => string.Compare(p.Name, fieldName, StringComparison.OrdinalIgnoreCase) == 0);
        if (property.Equals(default(JsonProperty)))
        {
            logger.LogDebug("Output does not contain field {name}", fieldName);
            return null;
        }
        var fieldValue = property.Value.ToInferredType();
        if (fieldValue is null)
            return null;
        var cultureInfo = CultureInfo.InvariantCulture;
        switch (fieldValue)
        {
            case string when field.Type == FieldType.String:
            case int when field.Type == FieldType.Int32:
            case float when field.Type == FieldType.Float:
            case bool when field.Type == FieldType.Bool:
            case DateTime when field.Type == FieldType.DateTime:
                return fieldValue;
            case string @string:
                (bool isSuccess, object? result) = field.Type switch
                {
                    FieldType.DateTime => DateTime.TryParse(@string, cultureInfo, out DateTime @dateTime) ? (true, @dateTime) : (false, null),
                    FieldType.Int16 => short.TryParse(TrimNumber(@string), cultureInfo, out short int16) ? (true, int16) : (false, null),
                    FieldType.UInt16 => ushort.TryParse(TrimNumber(@string), cultureInfo, out ushort uint16) ? (true, uint16) : (false, null),
                    FieldType.Int32 => int.TryParse(TrimNumber(@string), cultureInfo, out int int32) ? (true, int32) : (false, null),
                    FieldType.UInt32 => uint.TryParse(TrimNumber(@string), cultureInfo, out uint uint32) ? (true, uint32) : (false, null),
                    FieldType.Int64 => long.TryParse(TrimNumber(@string), cultureInfo, out long int64) ? (true, int64) : (false, null),
                    FieldType.UInt64 => ulong.TryParse(TrimNumber(@string), cultureInfo, out ulong int64) ? (true, int64) : (false, null),
                    FieldType.Float => float.TryParse(TrimNumber(@string), cultureInfo, out float @float) ? (true, @float) : (false, null),
                    FieldType.Decimal => decimal.TryParse(TrimNumber(@string), cultureInfo, out decimal @decimal) ? (true, @decimal) : (false, null),
                    FieldType.Double => double.TryParse(TrimNumber(@string), cultureInfo, out double @double) ? (true, @double) : (false, null),
                    FieldType.Bool => bool.TryParse(@string, out bool @bool) ? (true, @bool) : (false, null),
                    _ => (false, (object?)null)
                };
                if (isSuccess)
                    return result;
                logger.LogDebug("Unable to convert {result}", fieldValue);
                return fieldValue;
        }
        try
        {
            return field.Type switch
            {
                FieldType.DateTime => Convert.ToDateTime(fieldValue, cultureInfo),
                FieldType.Int16 => Convert.ToInt16(fieldValue, cultureInfo),
                FieldType.UInt16 => Convert.ToUInt16(fieldValue, cultureInfo),
                FieldType.Int32 => Convert.ToInt32(fieldValue, cultureInfo),
                FieldType.UInt32 => Convert.ToUInt32(fieldValue, cultureInfo),
                FieldType.Int64 => Convert.ToInt64(fieldValue, cultureInfo),
                FieldType.UInt64 => Convert.ToUInt64(fieldValue, cultureInfo),
                FieldType.Float => Convert.ToSingle(fieldValue, cultureInfo),
                FieldType.Decimal => Convert.ToDecimal(fieldValue, cultureInfo),
                FieldType.Double => Convert.ToDouble(fieldValue, cultureInfo),
                FieldType.Bool => Convert.ToBoolean(fieldValue, cultureInfo),
                _ => throw new NotSupportedException()
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to convert {result}", fieldValue);
        }
        return fieldValue;
    }

    private static string TrimNumber(string input)
    {
        return Regex.Replace(input, @"^[^0-9,.-]+|[^0-9,.]+$", string.Empty);
    }
}
