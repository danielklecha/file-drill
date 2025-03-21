using System.Collections;
using System.Reflection;
using Humanizer;
using Spectre.Console;

namespace FileDrill.Extensions;

public static class AnsiConsoleExtensions
{
    public static async Task<object> AskAsync(this IAnsiConsole ansiConsole, Type type, string prompt, CancellationToken cancellationToken = default)
    {
        var method = (typeof(Spectre.Console.AnsiConsoleExtensions)
            .GetMethod(nameof(Spectre.Console.AnsiConsoleExtensions.AskAsync), [typeof(IAnsiConsole), typeof(string), typeof(CancellationToken)])
            ?? throw new InvalidOperationException("Could not find generic AskObjectAsync method.")).MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task) ?? throw new Exception("AskObjectAsync returned null");
    }


    public static async Task<object> AskObjectAsync(this IAnsiConsole ansiConsole, Type type, string prompt, object? obj = default, CancellationToken cancellationToken = default)
    {
        var method = (typeof(AnsiConsoleExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Equals(nameof(AskObjectAsync)) && x.IsGenericMethod)
            ?? throw new InvalidOperationException("Could not find generic AskObjectAsync method.")).MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, obj, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task) ?? throw new Exception("AskObjectAsync returned null");
    }

    public static async Task<T> AskObjectAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj = default, CancellationToken cancellationToken = default) where T : class, new()
    {
        obj ??= new T();
        while (true)
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var choices = properties.Select(p =>
            {
                string propertyType = p.PropertyType.ToString();
                object? propertyValue = p.GetValue(obj);
                string? formattedPropertyValue = propertyValue switch
                {
                    null => "null",
                    ICollection collection => $"{collection.Count} items",
                    string stringValue => stringValue.Truncate(50),
                    Enum enumValue => enumValue.ToString(),
                    Uri uri => uri.OriginalString.Truncate(50),
                    _ => "is set"
                };
                return ((PropertyInfo?)p, $"{p.Name}: {formattedPropertyValue}".EscapeMarkup());
            }).ToList();
            choices.Add((null, "Back"));
            (PropertyInfo? PropertyInfo, string DisplayName) selectedChoice = await ansiConsole.PromptAsync(
                new SelectionPrompt<(PropertyInfo? PropertyInfo, string DisplayName)>()
                    .Title($"{prompt} - select a property to edit")
                    .UseConverter(x => x.DisplayName)
                    .AddChoices(choices));
            if (selectedChoice.DisplayName == "Back" || selectedChoice.PropertyInfo is null)
                break;
            var propertyPrompt = $"New value for {selectedChoice.PropertyInfo.Name}".EscapeMarkup();
            if (typeof(IList).IsAssignableFrom(selectedChoice.PropertyInfo.PropertyType))
                selectedChoice.PropertyInfo.SetValue(obj, await ansiConsole.AskNullableListAsync(selectedChoice.PropertyInfo.PropertyType, propertyPrompt, selectedChoice.PropertyInfo.GetValue(obj), cancellationToken));
            else if (typeof(IDictionary).IsAssignableFrom(selectedChoice.PropertyInfo.PropertyType))
                selectedChoice.PropertyInfo.SetValue(obj, await ansiConsole.AskNullableDictionaryAsync(selectedChoice.PropertyInfo.PropertyType, propertyPrompt, selectedChoice.PropertyInfo.GetValue(obj), cancellationToken));
            else if (selectedChoice.PropertyInfo.PropertyType.IsEnum)
                selectedChoice.PropertyInfo.SetValue(obj, await ansiConsole.AskNullableEnumAsync(selectedChoice.PropertyInfo.PropertyType, propertyPrompt, cancellationToken));
            else if (selectedChoice.PropertyInfo.PropertyType.IsClass && selectedChoice.PropertyInfo.PropertyType != typeof(string))
                selectedChoice.PropertyInfo.SetValue(obj, await ansiConsole.AskObjectAsync(selectedChoice.PropertyInfo.PropertyType, propertyPrompt, selectedChoice.PropertyInfo.GetValue(obj), cancellationToken));
            else
                selectedChoice.PropertyInfo.SetValue(obj, await ansiConsole.AskNullableAsync(selectedChoice.PropertyInfo.PropertyType, propertyPrompt, cancellationToken));
        }
        return obj;
    }

    public static async Task<object?> AskNullableObjectAsync(this IAnsiConsole ansiConsole, Type type, string prompt, object? obj = default, CancellationToken cancellationToken = default)
    {
        var method = (typeof(AnsiConsoleExtensions)
             .GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Equals(nameof(AskNullableObjectAsync)) && x.IsGenericMethod)
             ?? throw new InvalidOperationException("Could not find generic AskNullableObjectAsync method.")).MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, obj, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    public static async Task<T?> AskNullableObjectAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj = default, CancellationToken cancellationToken = default) where T : class, new()
    {
        var choice = await ansiConsole.PromptAsync(new SelectionPrompt<string>().Title(prompt).AddChoices(["Set", "Set Null"]), cancellationToken: cancellationToken);
        return choice == "Set"
            ? await ansiConsole.AskObjectAsync<T>(prompt, obj, cancellationToken)
            : default;
    }

    public static async Task<object?> AskNullableAsync(this IAnsiConsole ansiConsole, Type type, string prompt, CancellationToken cancellationToken = default)
    {
        var method = typeof(AnsiConsoleExtensions).GetMethod("AskNullableAsync", [typeof(IAnsiConsole), typeof(string), typeof(CancellationToken)])
            ?? throw new InvalidOperationException("Method AskNullableAsync<T> not found.");
        var genericMethod = method.MakeGenericMethod(type);
        var task = (Task?)genericMethod.Invoke(null, [ansiConsole, prompt, cancellationToken])
            ?? throw new Exception("Method AskNullableAsync<T> doesn't return task.");
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    public static async Task<T?> AskNullableAsync<T>(this IAnsiConsole ansiConsole, string prompt, CancellationToken cancellationToken = default) //where T : new()
    {
        var type = typeof(T);
        if (type.IsClass && type != typeof(string))
            return (T?)await ansiConsole.AskNullableObjectAsync(type, prompt, default, cancellationToken);
        if (!type.IsClass && Nullable.GetUnderlyingType(type) == null)
            return await ansiConsole.AskAsync<T>(prompt, cancellationToken);
        var choice = await ansiConsole.PromptAsync(new SelectionPrompt<string>().Title(prompt).AddChoices(["Set", "Set Null"]), cancellationToken: cancellationToken);
        return choice == "Set"
            ? await ansiConsole.AskAsync<T>(prompt, cancellationToken)
            : default;
    }

    public static async Task<object?> AskNullableEnumAsync(this IAnsiConsole ansiConsole, Type type, string prompt, CancellationToken cancellationToken = default)
    {
        if (!type.IsEnum)
            throw new ArgumentException($"Type {type} must be enum.", nameof(type));
        var method = (typeof(AnsiConsoleExtensions)
            .GetMethod(nameof(AskNullableEnumAsync), [typeof(IAnsiConsole), typeof(string), typeof(CancellationToken)])
            ?? throw new InvalidOperationException("Could not find generic AskNullableEnumAsync method."))
            .MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    public static async Task<T?> AskNullableEnumAsync<T>(this IAnsiConsole ansiConsole, string prompt, CancellationToken cancellationToken = default) where T : Enum
    {
        var type = typeof(T);
        if (Nullable.GetUnderlyingType(type) == null)
            return await ansiConsole.AskEnumAsync<T>(prompt, cancellationToken);
        var choice = await ansiConsole.PromptAsync(new SelectionPrompt<string>().Title(prompt).AddChoices(["Set", "Set Null"]), cancellationToken: cancellationToken);
        return choice == "Set"
            ? await ansiConsole.AskEnumAsync<T>(prompt, cancellationToken)
            : default;
    }

    public static async Task<object?> AskEnumAsync(this IAnsiConsole ansiConsole, Type type, string prompt, CancellationToken cancellationToken = default)
    {
        if (!type.IsEnum)
            throw new ArgumentException($"Type {type} must be enum.", nameof(type));
        var method = (typeof(AnsiConsoleExtensions)
            .GetMethod(nameof(AskEnumAsync), [typeof(IAnsiConsole), typeof(string), typeof(CancellationToken)])
            ?? throw new InvalidOperationException("Could not find generic AskEnumAsync method."))
            .MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    public static async Task<T> AskEnumAsync<T>(this IAnsiConsole ansiConsole, string prompt, CancellationToken cancellationToken = default) where T : Enum
    {
        var enumValues = Enum.GetNames(typeof(T)).ToList();
        var selectedValue = await ansiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title(prompt)
                .UseConverter(x => x.EscapeMarkup())
                .AddChoices(enumValues));
        return (T)Enum.Parse(typeof(T), selectedValue);
    }

    public static async Task<object?> AskNullableListAsync(this IAnsiConsole ansiConsole, Type type, string prompt, object? obj = default, CancellationToken cancellationToken = default)
    {
        if (!typeof(IList).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type} must implement IList.", nameof(type));
        if (obj is not null && obj.GetType() != type)
            obj = null;
        var method = (typeof(AnsiConsoleExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Equals(nameof(AskNullableListAsync)) && x.IsGenericMethod)
            ?? throw new InvalidOperationException("Could not find generic AskNullableListAsync method."))
            .MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, obj, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    public static async Task<T?> AskNullableListAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj, CancellationToken cancellationToken = default) where T : IList, new()
    {
        var elementType = typeof(T).IsGenericType ? typeof(T).GetGenericArguments()[0] : typeof(object);
        while (true)
        {
            var options = new List<string> { "Add" };
            if (obj?.Count > 0)
            {
                options.Add("Remove");
                options.Add("Clear");
            }
            if (Nullable.GetUnderlyingType(typeof(T)) is not null)
                options.Add("Set Null");
            options.Add("Back");
            var choice = await ansiConsole.PromptAsync(new SelectionPrompt<string>().Title(prompt).AddChoices(options), cancellationToken: cancellationToken);
            switch (choice)
            {
                case "Back":
                    return obj;
                case "Add":
                    obj ??= new();
                    await ansiConsole.AskObjectAsync(elementType, "New value", cancellationToken);
                    break;
                case "Remove" when obj is not null:
                    var itemToRemove = await ansiConsole.PromptAsync(
                        new SelectionPrompt<object>()
                            .Title("Select value to remove:")
                            .UseConverter(x => x.ToString().EscapeMarkup())
                            .AddChoices(obj.Cast<object>().ToList()));
                    obj.Remove(itemToRemove);
                    break;
                case "Set Null":
                    return default;
                case "Clear" when obj is not null:
                    obj.Clear();
                    break;
            }
        }
    }

    public static async Task<T> AskListAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj, CancellationToken cancellationToken = default) where T : IList, new()
    {
        obj = await ansiConsole.AskNullableListAsync(prompt, obj, cancellationToken);
        return obj ?? new T();
    }

    public static async Task<object?> AskNullableDictionaryAsync(this IAnsiConsole ansiConsole, Type type, string prompt, object? obj = default, CancellationToken cancellationToken = default)
    {
        if (!typeof(IDictionary).IsAssignableFrom(type))
            throw new ArgumentException($"Type {type} must implement IDictionary.", nameof(type));
        if (obj is not null && obj.GetType() != type)
            obj = null;
        var method = (typeof(AnsiConsoleExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Equals(nameof(AskNullableDictionaryAsync)) && x.IsGenericMethod)
            ?? throw new InvalidOperationException("Could not find generic AskNullableDictionaryAsync method."))
            .MakeGenericMethod(type);
        var task = (Task)method.Invoke(null, [ansiConsole, prompt, obj, cancellationToken])!;
        await task.ConfigureAwait(false);
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    public static async Task<T?> AskNullableDictionaryAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj, CancellationToken cancellationToken = default) where T : IDictionary, new()
    {
        var keyType = typeof(T).GetGenericArguments()[0];
        var valueType = typeof(T).GetGenericArguments()[1];
        while (true)
        {
            var options = new List<string> { "Add" };
            if (obj is not null && obj.Count > 0)
                options.Add("Remove");
            options.Add("Clear");
            if (Nullable.GetUnderlyingType(typeof(T)) is not null)
                options.Add("Set Null");
            options.Add("Back");

            var choice = await ansiConsole.PromptAsync(new SelectionPrompt<string>()
                    .Title(prompt)
                    .AddChoices(options), cancellationToken: cancellationToken);
            switch (choice)
            {
                case "Back":
                    return obj;
                case "Add":
                    obj ??= new();
                    var key = await ansiConsole.AskAsync(keyType, "New key", cancellationToken: cancellationToken);
                    var value = await ansiConsole.AskNullableAsync(valueType, "New value", cancellationToken: cancellationToken);
                    obj.Add(key, value);
                    break;
                case "Remove" when obj is not null:
                    var itemToRemove = await ansiConsole.PromptAsync(
                        new SelectionPrompt<object>()
                            .Title("Select key to remove:")
                            .UseConverter(x => x.ToString().EscapeMarkup())
                            .AddChoices(obj.Keys.Cast<object>().ToList()));
                    obj.Remove(itemToRemove);
                    break;
                case "Set Null":
                    return default;
                case "Clear" when obj is not null:
                    obj.Clear();
                    break;
            }
        }
    }

    public static async Task<T> AskDictionaryAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj, CancellationToken cancellationToken = default) where T : IDictionary, new()
    {
        obj = await ansiConsole.AskNullableDictionaryAsync(prompt, obj, cancellationToken);
        return obj ?? new T();
    }
}
