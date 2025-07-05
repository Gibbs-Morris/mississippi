using System.Text.RegularExpressions;


namespace Mississippi.EventSourcing.Abstractions.Attributes;

/// <summary>
///     Attribute used to define the name of Projection stream in the event sourcing system.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed partial class StreamNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes Projection new instance of the <see cref="StreamNameAttribute" /> class.
    /// </summary>
    /// <param name="appName">
    ///     The application name component of the stream name. Must contain only uppercase alphanumeric
    ///     characters.
    /// </param>
    /// <param name="moduleName">
    ///     The module name component of the stream name. Must contain only uppercase alphanumeric
    ///     characters.
    /// </param>
    /// <param name="name">The specific name component of the stream. Must contain only uppercase alphanumeric characters.</param>
    public StreamNameAttribute(
        string appName,
        string moduleName,
        string name
    )
    {
        ValidateParameter(appName, nameof(appName));
        ValidateParameter(moduleName, nameof(moduleName));
        ValidateParameter(name, nameof(name));
        AppName = appName;
        ModuleName = moduleName;
        Name = name;
    }

    /// <summary>
    ///     Gets the application name component of the stream name.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    ///     Gets the module name component of the stream name.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     Gets the specific name component of the stream.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the fully qualified stream name in the format {AppName}.{ModuleName}.{Name}.
    /// </summary>
    public string StreamName => $"{AppName}.{ModuleName}.{Name}";

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

    [GeneratedRegex("^[A-Z0-9]+$", RegexOptions.Compiled)]
    private static partial Regex MyValidationRegex();
}