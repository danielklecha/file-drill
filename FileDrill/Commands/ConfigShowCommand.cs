using FileDrill.Models;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.Text.Json;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FileDrill.Commands;
public class ConfigShowCommand : Command
{
    public ConfigShowCommand() : base("show", "Shows configuration")
    {
    }

    public new class Handler(
        IAnsiConsole ansiConsole,
        IOptions<WritableOptions> options) : ICommandHandler
    {
        public int Invoke(InvocationContext context) => 0;

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var serialized = JsonSerializer.Serialize(options.Value, new JsonSerializerOptions()
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });
            ansiConsole.Write(serialized);
            return Task.FromResult(0);
        }
    }
}