using FileDrill.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
public class AnsiConsoleLogger(
    string categoryName,
    Func<AnsiConsoleLoggerConfiguration, LogLevel, bool> filter,
    AnsiConsoleLoggerConfiguration config) : ILogger
{
    private readonly AnsiConsoleLoggerConfiguration _config = config;
    private static readonly AsyncLocal<ConcurrentStack<string>> _scopes = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        ConcurrentStack<string>? scopeStack = _scopes.Value;
        if (scopeStack == null)
        {
            scopeStack = new ConcurrentStack<string>();
            _scopes.Value = scopeStack;
        }
        scopeStack.Push(state?.ToString() ?? throw new Exception("State is null"));
        return new AnsiConsoleLoggerScope(() =>
        {
            _ = scopeStack.TryPop(out _);
        });
    }

    public bool IsEnabled(LogLevel logLevel) => filter(_config, logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        ArgumentNullException.ThrowIfNull(formatter);
        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
            return;
        if (_config.EventId == 0 || _config.EventId == eventId.Id)
        {
            WriteMessage(logLevel, message, exception);
        }
    }

    private void WriteMessage(LogLevel logLevel, string message, Exception? exception)
    {
        ConcurrentStack<string>? scopeStack = _scopes.Value;
        string scopeInfo = scopeStack != null && !scopeStack.IsEmpty ? $"[{string.Join(" => ", scopeStack)}] " : string.Empty;
        string categoryInfo = Debugger.IsAttached ? $"[{categoryName}] " : string.Empty;
        string? logMessage = $"[{DateTime.Now:HH:mm:ss} {logLevel.ToString()[..3].ToUpper()}] {scopeInfo}{categoryInfo}{message}";
        IAnsiConsole console = _config.Console ?? AnsiConsole.Console;
        if (logMessage is not null)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    console.MarkupLineInterpolated($"[grey]{logMessage}[/]");
                    break;
                //case LogLevel.Information:
                //    console.MarkupLineInterpolated($"[blue]{logMessage}[/]");
                //    break;
                case LogLevel.Warning:
                    console.MarkupLineInterpolated($"[yellow]{logMessage}[/]");
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    console.MarkupLineInterpolated($"[red]{logMessage}[/]");
                    break;
                default:
                    console.MarkupLineInterpolated($"{logMessage}");
                    break;
            }
        }
        if (exception != null)
        {
            console.WriteException(exception);
            //console.MarkupLineInterpolated($"[red]{exception}[/]");
        }
    }
}