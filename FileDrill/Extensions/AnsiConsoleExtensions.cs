using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Humanizer;
using Spectre.Console;

namespace FileDrill.Extensions;

public static class AnsiConsoleExtensions
{
    static int displayNameLimit = 50;
    static bool allowSerializationForDisplayName = true;
    static Assembly[]? assembliesWithForcedInstances;
    static Type[]? typesWithForcedInstances;
    static Assembly[]? assembliesWithComplexObjects;
    static Type[]? typesWithComplexObjects;

    /// <summary>
    /// Configures the display name limit and serialization option.
    /// </summary>
    /// <param name="displayNameLimit">The maximum length for display names. Must be greater than 0.</param>
    /// <param name="allowSerializationForDisplayName">Whether to allow serialization for display names.</param>
    public static void ConfigureDisplayName(int displayNameLimit = 50, bool allowSerializationForDisplayName = true)
    {
        if (displayNameLimit < 1)
            throw new ArgumentException("Display name limit must be greater than 0.", nameof(displayNameLimit));
        AnsiConsoleExtensions.displayNameLimit = displayNameLimit;
        AnsiConsoleExtensions.allowSerializationForDisplayName = allowSerializationForDisplayName;
    }

    /// <summary>
    /// Configures assemblies and types with forced instances.
    /// </summary>
    /// <param name="typesWithForcedInstances">Types with forced instances.</param>
    /// <param name="assembliesWithForcedInstances">Assemblies with forced instances.</param>
    public static void ConfigureForcedInstances(Type[]? typesWithForcedInstances = null, Assembly[]? assembliesWithForcedInstances = null)
    {
        AnsiConsoleExtensions.typesWithForcedInstances = typesWithForcedInstances;
        AnsiConsoleExtensions.assembliesWithForcedInstances = assembliesWithForcedInstances;
    }

    /// <summary>
    /// Configures assemblies and types with complex objects.
    /// </summary>
    /// <param name="typesWithComplexObjects">Types with complex objects.</param>
    /// <param name="assembliesWithComplexObjects">Assemblies with complex objects.</param>
    public static void ConfigureComplexObjects(Type[]? typesWithComplexObjects = null, Assembly[]? assembliesWithComplexObjects = null)
    {
        AnsiConsoleExtensions.typesWithComplexObjects = typesWithComplexObjects;
        AnsiConsoleExtensions.assembliesWithComplexObjects = assembliesWithComplexObjects;
    }

    /// <summary>
    /// Resets all configurations to their default values.
    /// </summary>
    public static void ResetConfigurations()
    {
        displayNameLimit = 50;
        allowSerializationForDisplayName = true;
        assembliesWithForcedInstances = null;
        typesWithForcedInstances = null;
        assembliesWithComplexObjects = null;
        typesWithComplexObjects = null;
    }

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
                return ((PropertyInfo?)p, $"{p.Name}: {ToFormattedString(propertyValue)}".EscapeMarkup());
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

    private static string? ToFormattedString(object? obj)
    {
        switch(obj)
        {
            case null:
                return "null";
            case ICollection collection:
                return $"{collection.Count} items";
            case string stringValue:
                return stringValue.Truncate(displayNameLimit);
            case Enum enumValue:
                return enumValue.ToString();
            default:
                var value = obj.ToString();
                if (value is null)
                    return "is set";
                var typeFullName = obj.GetType().FullName;
                if (typeFullName is null || !value.Equals(typeFullName))
                    return value;
                if (!allowSerializationForDisplayName)
                    return "is set";
                try
                {
                    var serialized = JsonSerializer.Serialize(obj);
                    return serialized.Truncate(displayNameLimit);
                }
                catch (Exception)
                {
                    return "is set";
                }
        }
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
        if (!CanBeNull(typeof(T)))
            return await ansiConsole.AskObjectAsync<T>(prompt, obj, cancellationToken);
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
        if (IsComplexObject(type))
            return (T?)await ansiConsole.AskNullableObjectAsync(type, prompt, default, cancellationToken);
        if (!CanBeNull(type))
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
        if (!CanBeNull(type))
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
        var valueType = typeof(T).IsGenericType ? typeof(T).GetGenericArguments()[0] : typeof(object);
        while (true)
        {
            var options = new List<(int Index, object? Key, string DisplayName)> { (-1, null, "Add") };
            if (obj is not null && obj.Count > 0)
                options.Add((-3, null, "Clear"));
            if (CanBeNull(typeof(T)))
                options.Add((-4, null, "Set Null"));
            options.Add((-5, null, "Back"));
            var values = obj?.Cast<object>().ToList() ?? [];
            for (var i = 0; i < values.Count; i++)
            {
                options.Add((i, values[i], $"[{i}]: {ToFormattedString(values[i])}".EscapeMarkup()));
            }
            var choice = await ansiConsole.PromptAsync(new SelectionPrompt<(int Index, object? Key, string DisplayName)>()
                    .Title(prompt)
                    .UseConverter(x => x.DisplayName)
                    .AddChoices(options), cancellationToken: cancellationToken);
            switch (choice.Index)
            {
                case -5:
                    return obj;
                case -1:
                    obj ??= new();
                    var value = await ansiConsole.AskNullableAsync(valueType, "New value", cancellationToken: cancellationToken);
                    obj.Add(value);
                    break;
                case -4:
                    return default;
                case -3 when obj is not null:
                    obj.Clear();
                    break;
                default:
                    var nestedOptions = new List<(int Index, string Key)> { (-1, "Set") };
                    if (CanBeNull(valueType))
                        nestedOptions.Add((-4, "Set Null"));
                    nestedOptions.Add((-2, "Remove"));
                    var nestedChoice = await ansiConsole.PromptAsync(new SelectionPrompt<(int Index, string Key)>()
                        .Title(prompt)
                        .UseConverter(x => x.Key)
                        .AddChoices(nestedOptions), cancellationToken: cancellationToken);
                    switch (nestedChoice.Index)
                    {
                        case -1 when obj is not null:
                            obj[choice.Index] = await ansiConsole.AskNullableAsync(valueType, "New value", cancellationToken: cancellationToken);
                            break;
                        case -4 when obj is not null:
                            obj[choice.Index] = GetDefault(valueType);
                            break;
                        case -2 when obj is not null:
                            obj.Remove(choice.Key);
                            break;
                    }
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
            var options = new List<(int Index, object? Key, string DisplayName)> { (-1, null, "Add") };
            if (obj is not null && obj.Count > 0)
                options.Add((-3, null, "Clear"));
            if (CanBeNull(typeof(T)))
                options.Add((-4, null, "Set Null"));
            options.Add((-5, null, "Back"));
            var keys = obj?.Keys.Cast<object>().ToList() ?? [];
            for (var i = 0; i < keys.Count; i++)
            {
                options.Add((i, keys[i], $"[{i}]: {ToFormattedString(keys[i])}".EscapeMarkup()));
            }

            var choice = await ansiConsole.PromptAsync(new SelectionPrompt<(int Index, object? Key, string DisplayName)>()
                    .Title(prompt)
                    .UseConverter(x => x.DisplayName)
                    .AddChoices(options), cancellationToken: cancellationToken);
            switch (choice.Index)
            {
                case -5:
                    return obj;
                case -1:
                    obj ??= new();
                    var key = await ansiConsole.AskAsync(keyType, "New key", cancellationToken: cancellationToken);
                    var value = await ansiConsole.AskNullableAsync(valueType, "New value", cancellationToken: cancellationToken);
                    obj.Add(key, value);
                    break;
                case -4:
                    return default;
                case -3 when obj is not null:
                    obj.Clear();
                    break;
                default:
                    var nestedOptions = new List<(int Index, string Key)> { (-1, "Set") };
                    if (CanBeNull(valueType))
                        nestedOptions.Add((-4, "Set Null"));
                    nestedOptions.Add((-2, "Remove"));
                    var nestedChoice = await ansiConsole.PromptAsync(new SelectionPrompt<(int Index, string Key)>()
                        .Title(prompt)
                        .UseConverter(x => x.Key)
                        .AddChoices(nestedOptions), cancellationToken: cancellationToken);
                    switch(nestedChoice.Index)
                    {
                        case -1 when obj is not null:
                            obj[choice.Index] = await ansiConsole.AskNullableAsync(valueType, "New value", cancellationToken: cancellationToken);
                            break;
                        case -4 when obj is not null:
                            obj[choice.Index] = GetDefault(valueType);
                            break;
                        case -2 when obj is not null:
                            obj.Remove(choice.Key);
                            break;
                    }
                    break;
            }
        }
    }

    private static object? GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            var lambda = Expression.Lambda(Expression.Default(type));
            return lambda.Compile().DynamicInvoke();
        }
        return null;
    }

    private static bool CanBeNull(Type type)
    {
        if (!type.IsClass && Nullable.GetUnderlyingType(type) == null)
            return false;
        if (typesWithForcedInstances?.Any(t => t == type || t.IsAssignableFrom(type)) ?? false)
            return false;
        if (assembliesWithForcedInstances?.Contains(type.Assembly) ?? false)
            return false;
        return true;
    }

    private static bool IsComplexObject(Type type)
    {
        if (!type.IsClass)
            return false;
        if (typesWithComplexObjects?.Any(t => t == type || t.IsAssignableFrom(type)) ?? false)
            return true;
        if (assembliesWithComplexObjects?.Contains(type.Assembly) ?? false)
            return true;
        return false;
    }

    public static async Task<T> AskDictionaryAsync<T>(this IAnsiConsole ansiConsole, string prompt, T? obj, CancellationToken cancellationToken = default) where T : IDictionary, new()
    {
        obj = await ansiConsole.AskNullableDictionaryAsync(prompt, obj, cancellationToken);
        return obj ?? new T();
    }
}
