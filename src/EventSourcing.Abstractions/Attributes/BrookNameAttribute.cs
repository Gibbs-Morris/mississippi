using System.Text.RegularExpressions;


namespace Mississippi.EventSourcing.Abstractions.Attributes;

/// <summary>
///     Attribute that specifies the hierarchical naming convention for a brook (event stream).
///     Ensures consistent naming across the application with validation for proper format.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed partial class BrookNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookNameAttribute" /> class.
    ///     Creates a hierarchical brook name from the specified application, module, and specific name components.
    /// </summary>
    /// <param name="appName">The application name component. Must contain only uppercase alphanumeric characters.</param>
    /// <param name="moduleName">The module name component. Must contain only uppercase alphanumeric characters.</param>
    /// <param name="name">The specific brook name component. Must contain only uppercase alphanumeric characters.</param>
    /// <exception cref="ArgumentException">Thrown when any parameter is null, whitespace, or contains invalid characters.</exception>
    public BrookNameAttribute(
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
    ///     Gets the application name component of the brook name.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    ///     Gets the module name component of the brook name.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     Gets the specific name component of the brook.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the fully qualified brook name in the format {AppName}.{ModuleName}.{Name}.
    /// </summary>
    public string BrookName => $"{AppName}.{ModuleName}.{Name}";

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
