namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Module-owned default values for Aqueduct stream/backplane settings.
/// </summary>
public static class AqueductStreamDefaults
{
    /// <summary>
    ///     Default stream namespace for hub-wide broadcasts to all clients.
    /// </summary>
    public const string AllClientsStreamNamespace = "mississippi-all-clients";

    /// <summary>
    ///     Default stream namespace for server-targeted messages.
    /// </summary>
    public const string ServerStreamNamespace = "mississippi-server";

    /// <summary>
    ///     Default Orleans stream provider name used by Aqueduct.
    /// </summary>
    public const string StreamProviderName = "mississippi-streaming";
}