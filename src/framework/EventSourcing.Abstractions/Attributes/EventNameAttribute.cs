using System.Text.RegularExpressions;


namespace Mississippi.EventSourcing.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventNameAttribute : Attribute
{
    private static readonly Regex ValidationRegex = new("^[A-Z0-9]+$", RegexOptions.Compiled);

    public EventNameAttribute(
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

    public string AppName { get; }

    public string ModuleName { get; }

    public string Name { get; }

    public int Version { get; }

    public string GetEventName() => $"{AppName}.{ModuleName}.{Name}V{Version}";

    private static void ValidateParameter(
        string value,
        string paramName
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace", paramName);
        }

        if (!ValidationRegex.IsMatch(value))
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