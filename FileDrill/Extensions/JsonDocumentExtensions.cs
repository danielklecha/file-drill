using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDrill.Extensions;

public static class JsonDocumentExtensions
{
    public static JsonDocument ToNestedJsonDocument(this JsonDocument originalDoc, string key)
    {
        string[] sections = key.Split(':');
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        for (int i = 0; i < sections.Length; i++)
        {
            writer.WritePropertyName(sections[i]);
            if (i < sections.Length - 1)
            {
                writer.WriteStartObject();
            }
        }
        originalDoc.RootElement.WriteTo(writer);
        for (int i = 0; i < sections.Length - 1; i++)
        {
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.Flush();
        stream.Position = 0;
        return JsonDocument.Parse(stream);
    }
}
