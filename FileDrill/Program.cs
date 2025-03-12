using FileDrill.Commands;
using FileDrill.Extensions;
using FileDrill.Models;
using FileDrill.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OpenAI;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;

Assembly assembly = Assembly.GetExecutingAssembly();
string assemblyFolder = Path.GetDirectoryName(assembly.Location) ?? throw new Exception("Unable to get assembly location");
MainCommand rootCommand = [];

#region update handlers
foreach (Command? command in Enumerable.Repeat(rootCommand, 1).Concat(rootCommand.GetDescendantCommands().Where(x => x.Handler is null)))
    command.SetHandler(() => { });
#endregion

Parser builder = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseHost(Host.CreateDefaultBuilder, builder =>
    {
        _ = builder.ConfigureLogging(builder =>
        {
            _ = builder.ClearProviders();
            _ = builder.AddProvider(new AnsiConsoleLoggerProvider(new AnsiConsoleLoggerConfiguration()));
            _ = builder.AddDebug();
        });
        #region load command handlers
        foreach (Type? commandType in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Command))))
        {
            Type? handlerType = commandType.GetNestedTypes().FirstOrDefault(t => typeof(ICommandHandler).IsAssignableFrom(t));
            if (handlerType == null)
                continue;
            _ = builder.UseCommandHandler(commandType, handlerType);

        }
        #endregion
        _ = builder.ConfigureAppConfiguration(builder =>
        {
            _ = builder.SetBasePath(assemblyFolder);
            _ = builder.AddWritableJsonFile("appdynamicsettings.json");
        });
        _ = builder.ConfigureServices((context, services) =>
        {
            _ = services
                .AddTransient<IDateTimeProvider, DateTimeProvider>()
                .AddTransient<IDateTimeOffsetProvider, DateTimeOffsetProvider>()
                .AddSingleton<System.IO.Abstractions.IFileSystem, System.IO.Abstractions.FileSystem>()
                .AddSingleton<IFileSystemDialogs, FileSystemDialogs>()
                .AddSingleton<IContentReaderService, ContentReaderService>()
                .AddSingleton<IContentClassifierService, ContentClassifierService>()
                .AddSingleton<IFieldExtractorService, FieldExtractorService>()
                .AddSingleton<IChatClientFactory, ChatClientFactory>()
                .AddSingleton(AnsiConsole.Console)
                .Configure<WritableOptions>(context.Configuration.GetSection(nameof(WritableOptions)))
                .PostConfigure<WritableOptions>(options =>
                {
                    options.ContentReader ??= new();
                    options.ContentClassifier ??= new();
                    options.FieldExtractor ??= new();
                    options.AIServices ??= [];
                    options.Schemas ??= [];
                })
                .AddSingleton<IOptionsSync<WritableOptions>>(new OptionsSync<WritableOptions>(
                    context.Configuration.GetSection(nameof(WritableOptions)),
                    ((IConfigurationRoot)context.Configuration).Providers.OfType<IWritableConfigurationProvider>()));
            typeof(Program).Assembly.GetTypes()
                .Where(x => !x.IsAbstract && x.IsClass && typeof(IContentReader).IsAssignableFrom(x))
                .ToList()
                .ForEach(x => services.AddSingleton(typeof(IContentReader), x));

        });
    })
    .Build();
return await builder.InvokeAsync(args);