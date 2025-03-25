using FileDrill.Models;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using System.CommandLine;
using FileDrill.Services;
using Spectre.Console;
using FileDrill.Extensions;
using Microsoft.Extensions.Options;

namespace FileDrill.Commands;

internal class ConfigSetWizardCommand() : Command("wizard", "Replaces configuration with wizard")
{
    public new class Handler(
        ILogger<Handler> logger,
        IAnsiConsole ansiConsole,
        IOptionsSync<WritableOptions> optionsSync) : ICommandHandler
    {
        public int Invoke(InvocationContext context) => 0;

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            Extensions.AnsiConsoleExtensions.ConfigureForcedInstances([typeof(System.Collections.ICollection)], [GetType().Assembly]);
            Extensions.AnsiConsoleExtensions.ConfigureComplexObjects(null, [GetType().Assembly]);
            var optionsValue = await ansiConsole.AskObjectAsync<WritableOptions>("New settings");
            await optionsSync.SyncAsync(optionsValue);
            await optionsSync.SaveAsync();
            logger.LogInformation("Options were updated");
            return 0;
        }
    }
}
