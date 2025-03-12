using FileDrill.Models;
using Microsoft.Extensions.AI;

namespace FileDrill.Services;
public interface IChatClientFactory
{
    IChatClient CreateClient(string? serviceName);
}