using System;
using System.Text.RegularExpressions;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

/// <summary>
///     Attribute used to define the name and version of a snapshot/state/projection in the event sourcing system.
/// </summary>
/// <remarks>
///     <para>
///         This attribute provides a stable string identity for snapshot types that survives
///         type renames and namespace changes. When persisting snapshots to storage, the
///         <see cref="SnapshotName" /> is used instead of the CLR type name, enabling safe refactoring.
///     </para>
///     <para>
///         The naming convention follows the pattern: {AppName}.{ModuleName}.{Name}.V{Version}.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed partial class SnapshotNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotNameAttribute" /> class.
    /// </summary>
    /// <param name="appName">
    ///     The application name component of the snapshot name. Must contain only uppercase alphanumeric
    ///     characters.
    /// </param>
    /// <param name="moduleName">
    ///     The module name component of the snapshot name. Must contain only uppercase alphanumeric
    ///     characters.
    /// </param>
    /// <param name="name">The specific name component of the snapshot. Must contain only uppercase alphanumeric characters.</param>
    /// <param name="version">The version number of the snapshot schema. Must be a positive integer.</param>
    public SnapshotNameAttribute(
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
    ///     Gets the application name component of the snapshot name.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    ///     Gets the module name component of the snapshot name.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     Gets the specific name component of the snapshot.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the fully qualified snapshot name in the format {AppName}.{ModuleName}.{Name}.V{Version}.
    /// </summary>
    public string SnapshotName => $"{AppName}.{ModuleName}.{Name}.V{Version}";

    /// <summary>
    ///     Gets the version number of the snapshot schema.
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