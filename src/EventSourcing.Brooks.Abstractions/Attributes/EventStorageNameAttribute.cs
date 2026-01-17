using System;
using System.Text.RegularExpressions;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

/// <summary>
///     Attribute used to define the storage name and version of an event in the event sourcing system.
/// </summary>
/// <remarks>
///     <para>
///         This attribute provides a stable string identity for event types that survives
///         type renames and namespace changes. When persisting events to storage, the
///         <see cref="StorageName" /> is used instead of the CLR type name, enabling safe refactoring.
///     </para>
///     <para>
///         The naming convention follows the pattern: {AppName}.{ModuleName}.{Name}.V{Version}.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed partial class EventStorageNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStorageNameAttribute" /> class.
    /// </summary>
    /// <param name="appName">
    ///     The application name component of the storage name. Must contain only uppercase alphanumeric
    ///     characters.
    /// </param>
    /// <param name="moduleName">
    ///     The module name component of the storage name. Must contain only uppercase alphanumeric
    ///     characters.
    /// </param>
    /// <param name="name">The specific name component of the event. Must contain only uppercase alphanumeric characters.</param>
    /// <param name="version">The version number of the event. Must be a positive integer.</param>
    public EventStorageNameAttribute(
        string appName,
        string moduleName,
        string name,
        int version = 1
    )
    {
        ValidateParameter(appName, nameof(appName));
        ValidateParameter(moduleName, nameof(moduleName));
        ValidateParameter(name, nameof(name));
        ValidateVersion(version);
        AppName = appName;
        ModuleName = moduleName;
        Name = name;
        Version = version;
    }

    /// <summary>
    ///     Gets the application name component of the storage name.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    ///     Gets the module name component of the storage name.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     Gets the specific name component of the event.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the fully qualified storage name in the format {AppName}.{ModuleName}.{Name}.V{Version}.
    /// </summary>
    public string StorageName => $"{AppName}.{ModuleName}.{Name}.V{Version}";

    /// <summary>
    ///     Gets the version number of the event.
    /// </summary>
    public int Version { get; }

    [GeneratedRegex("^[A-Z0-9]+$", RegexOptions.Compiled)]
    private static partial Regex MyValidationRegex();

    private static void ValidateParameter(
        string value,
        string paramName
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace", paramName);
        }

        Regex validationRegex = MyValidationRegex();
        if (!validationRegex.IsMatch(value))
        {
            throw new ArgumentException("Value must contain only uppercase alphanumeric characters", paramName);
        }
    }

    private static void ValidateVersion(
        int version
    )
    {
        if (version <= 0)
        {
            throw new ArgumentException("Version must be a positive integer", nameof(version));
        }
    }
}