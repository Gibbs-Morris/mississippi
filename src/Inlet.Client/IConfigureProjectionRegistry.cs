using Mississippi.Inlet.Client.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Interface for projection path registration during startup.
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
