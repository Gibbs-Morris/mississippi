using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet;

/// <summary>
///     Interface for projection route registration during startup.
/// </summary>
public interface IConfigureProjectionRegistry
{
    /// <summary>
    ///     Configures the projection registry.
    /// </summary>
    /// <param name="registry">The registry to configure.</param>
    void Configure(
        IProjectionRegistry registry
    );
}