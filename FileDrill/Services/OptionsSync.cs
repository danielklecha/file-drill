using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FileDrill.Services;
public class OptionsSync<T>(IConfigurationSection configurationSection, IEnumerable<IConfigurationProvider> providers) : IOptionsSync<T> where T : class, new()
{
    private readonly IConfigurationSection _configurationSection = configurationSection ?? throw new ArgumentNullException(nameof(configurationSection));
    private readonly IEnumerable<IConfigurationProvider> _providers = providers ?? Enumerable.Empty<IConfigurationProvider>();

    private IEnumerable<(string Path, JsonProperty P)> GetLeaves(string? path, JsonProperty p)
        => p.Value.ValueKind != JsonValueKind.Object
            ? [(Path: path == null ? p.Name : ConfigurationPath.Combine(path, p.Name), p)]
            : p.Value.EnumerateObject().SelectMany(child => GetLeaves(path == null ? p.Name : ConfigurationPath.Combine(path, p.Name), child));

    public Dictionary<string, string?> GetFlattened(T options)
    {
        using JsonDocument document = JsonSerializer.SerializeToDocument(options, typeof(T));
        return GetFlattened(document);
    }

    public Dictionary<string, string?> GetFlattened(JsonDocument document)
    {
        return document.RootElement
            .EnumerateObject()
            .SelectMany(p => GetLeaves(null, p))
            .ToDictionary(k => k.Path, v => v.P.Value.ValueKind == JsonValueKind.Null ? null : v.P.Value.ToString());
    }

    public Task SyncAsync(T options, CancellationToken cancelationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var paths = GetChildrenPathsRecursive(_configurationSection);
        var dictionary = GetFlattened(options)
            .ToDictionary(kvp => ConfigurationPath.Combine(_configurationSection.Path, kvp.Key), kvp => kvp.Value);

        if (!_providers.Any())
        {
            foreach (var path in paths.Except(dictionary.Keys))
                _configurationSection[path] = null;
            foreach (var kvp in dictionary)
                _configurationSection[kvp.Key] = kvp.Value;
        }
        else
        {
            foreach (IConfigurationProvider provider in _providers)
            {
                foreach (var path in paths.Except(dictionary.Keys))
                    provider.Set(path, null);
                foreach (var kvp in dictionary)
                    provider.Set(kvp.Key, kvp.Value);
            }
        }

        return Task.CompletedTask;
    }

    static IEnumerable<string> GetChildrenPathsRecursive(IConfigurationSection configuration)
    {
        return GetChildrenPathsRecursive(configuration, configuration.Path);
    }

    static IEnumerable<string> GetChildrenPathsRecursive(IConfigurationSection configuration, string parentPath)
    {
        foreach (var child in configuration.GetChildren())
        {
            string path = string.IsNullOrEmpty(parentPath) ? child.Key : $"{parentPath}{ConfigurationPath.KeyDelimiter}{child.Key}";
            yield return path;
            foreach (var subKey in GetChildrenPathsRecursive(child, path))
            {
                yield return subKey;
            }
        }
    }

    public async Task SaveAsync(CancellationToken cancelationToken = default)
    {
        foreach (IWritableConfigurationProvider provider in _providers.OfType<IWritableConfigurationProvider>())
        {
            cancelationToken.ThrowIfCancellationRequested();
            await provider.SaveAsync(cancelationToken);
        }
    }

    public void Bind(T options, JsonDocument jsonDocument)
    {
        Bind(options, GetFlattened(jsonDocument));
    }

    public void Bind(T options, Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        configuration.Bind(options);
    }

    public T Merge(T options, JsonDocument jsonDocument)
    {
        var dict1 = GetFlattened(options);
        var dict2 = GetFlattened(jsonDocument);
        var nullKeyPrefixes = dict2.Where(kvp => kvp.Value is null)
                                   .Select(kvp => kvp.Key + ConfigurationPath.KeyDelimiter)
                                   .ToHashSet();
        var mergedDict = dict1.Concat(dict2)
                              .Where(kvp => !nullKeyPrefixes.Any(nk => kvp.Key.StartsWith(nk, StringComparison.OrdinalIgnoreCase)))
                              .GroupBy(kvp => kvp.Key)
                              .ToDictionary(g => g.Key, g => g.Last().Value);

        T instance = new();
        new ConfigurationBuilder()
            .AddInMemoryCollection(mergedDict)
            .Build()
            .Bind(instance);
        return instance;
    }
}