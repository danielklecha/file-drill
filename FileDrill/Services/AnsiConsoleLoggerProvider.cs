using FileDrill.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
public class AnsiConsoleLoggerProvider(AnsiConsoleLoggerConfiguration config) : ILoggerProvider
{
    private readonly AnsiConsoleLoggerConfiguration _config = config;
    private readonly ConcurrentDictionary<string, AnsiConsoleLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new AnsiConsoleLogger(name, GetFilter(), _config));

    private Func<AnsiConsoleLoggerConfiguration, LogLevel, bool> GetFilter() => (config, logLevel) => true;

    public void Dispose() => _loggers.Clear();
}