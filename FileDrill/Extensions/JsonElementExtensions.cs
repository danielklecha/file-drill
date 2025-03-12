using System.Dynamic;
using System.Text.Json;

namespace FileDrill.Extensions;
public static class JsonElementExtensions
{
    public static object? ToInferredType(this JsonElement element, bool useExpandoObjectAsObjectFormat = false, bool allowFallbackValue = true)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt16(out var shortValue))
                    return shortValue;
                if (element.TryGetUInt16(out var ushortValue))
                    return ushortValue;
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                if (element.TryGetUInt32(out var uintValue))
                    return uintValue;
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetUInt64(out var ulongValue))
                    return ulongValue;
                if (element.TryGetSingle(out var floatValue))
                    return floatValue;
                if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;
                if (allowFallbackValue)
                    return element.Clone();
                throw new JsonException($"Cannot parse number: {element}");
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(x => x.ToInferredType(useExpandoObjectAsObjectFormat, allowFallbackValue)).ToArray();
            case JsonValueKind.Object:
                IDictionary<string, object?> dictionary = useExpandoObjectAsObjectFormat ? new ExpandoObject() : new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                    dictionary.Add(property.Name, property.Value.ToInferredType(useExpandoObjectAsObjectFormat, allowFallbackValue));
                return dictionary;
            default:
                throw new JsonException($"Unknown JsonElement ValueKind: {element.ValueKind}");
        }
    }
}