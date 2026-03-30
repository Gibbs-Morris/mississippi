using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Abstractions;

/// <summary>
///     Declares the stable, versioned external contract identity for a replica payload.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ReplicaContractNameAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaContractNameAttribute" /> class.
    /// </summary>
    /// <param name="appName">The application name segment.</param>
    /// <param name="moduleName">The module name segment.</param>
    /// <param name="name">The contract name segment.</param>
    /// <param name="version">The contract version.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="appName" />, <paramref name="moduleName" />, or <paramref name="name" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="appName" />, <paramref name="moduleName" />, or <paramref name="name" /> is empty or
    ///     whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="version" /> is less than 1.</exception>
    public ReplicaContractNameAttribute(
        string appName,
        string moduleName,
        string name,
        int version = 1
    )
    {
        ArgumentNullException.ThrowIfNull(appName);
        ArgumentNullException.ThrowIfNull(moduleName);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        AppName = appName;
        ModuleName = moduleName;
        Name = name;
        Version = version;
    }

    /// <summary>
    ///     Gets the application name segment.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    ///     Gets the stable versioned contract identity string.
    /// </summary>
    public string ContractIdentity => $"{AppName}.{ModuleName}.{Name}.V{Version}";

    /// <summary>
    ///     Gets the module name segment.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    ///     Gets the contract name segment.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the contract version.
    /// </summary>
    public int Version { get; }
}