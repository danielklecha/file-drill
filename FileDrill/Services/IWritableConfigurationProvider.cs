using Microsoft.Extensions.Configuration;

namespace FileDrill.Services;
public interface IWritableConfigurationProvider : IConfigurationProvider
{
    public Task SaveAsync(CancellationToken cancelationToken = default);
}
