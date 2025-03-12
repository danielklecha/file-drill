using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

namespace FileDrill.Services;
/// <summary>
/// Writable Json configuration provider
/// based on: https://stackoverflow.com/questions/57978535/save-changes-of-iconfigurationroot-sections-to-its-json-file-in-net-core-2-2
/// </summary>
public class WritableJsonConfigurationProvider : JsonConfigurationProvider, IWritableConfigurationProvider
{
    public WritableJsonConfigurationProvider(JsonConfigurationSource source) : base(source)
    {

    }

    private static Dictionary<string, object?> GetUnflattened(IDictionary<string, string?> input)
    {
        Dictionary<string, object?> output = [];
        foreach (var key in input.Keys)
        {
            if (input[key] is null)
                continue;
            Dictionary<string, object?> currentDictionary = output;
            IEnumerable<string> keysInPath = key.Split(ConfigurationPath.KeyDelimiter).AsEnumerable();
            while (keysInPath.Any())
            {
                string firstKeyInPath = keysInPath.First();
                if (keysInPath.Count() == 1)
                {
                    currentDictionary[firstKeyInPath] = input[key];
                    break;
                }
                if (!currentDictionary.TryGetValue(firstKeyInPath, out object? value) || value is not Dictionary<string, object?>)
                {
                    Dictionary<string, object?> node = [];
                    currentDictionary[firstKeyInPath] = node;
                    currentDictionary = node;
                }
                else
                {
                    currentDictionary = (Dictionary<string, object?>)value;
                }
                keysInPath = keysInPath.Skip(1);
            }
        }
        return output;
    }

    public async Task SaveAsync(CancellationToken cancelationToken)
    {
        string json = JsonSerializer.Serialize(GetUnflattened(Data), new JsonSerializerOptions() { WriteIndented = true });
        string? root = (Source.FileProvider as PhysicalFileProvider)?.Root;
        if (Source.Path == null)
            throw new Exception("Source settings for this provider are null");
        string path = root != null ? Path.Combine(root, Source.Path) : Source.Path;
        await File.WriteAllTextAsync(path, json, cancelationToken);
    }
}