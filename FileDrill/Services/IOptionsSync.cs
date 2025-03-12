using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDrill.Services;
public interface IOptionsSync<T> where T : class
{
    public Task SyncAsync(T options, CancellationToken cancelationToken = default);
    public Task SaveAsync(CancellationToken cancelationToken = default);
    void Bind(T options, JsonDocument jsonDocument);
    Dictionary<string, string?> GetFlattened(T options);
    Dictionary<string, string?> GetFlattened(JsonDocument document);
    void Bind(T options, Dictionary<string, string?> values);
    T Merge(T options, JsonDocument jsonDocument);
}