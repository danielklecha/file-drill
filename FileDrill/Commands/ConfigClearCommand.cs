using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.Text.Json;

namespace FileDrill.Commands;
internal class ConfigClearCommand() : Command("clear", "Clears configuration")
{
    public new class Handler(
        ILogger<Handler> logger,
        IOptionsSync<WritableOptions> optionsSync) : ICommandHandler
    {
        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var cancelationToken = context.GetCancellationToken();
            await optionsSync.SyncAsync(new WritableOptions(), cancelationToken);
            logger.LogInformation("Configuration has been cleared");
            await optionsSync.SaveAsync(cancelationToken);
            return 0;
        }
    }
}
