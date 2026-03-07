namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Identifies the high-level category of an alias-bearing type.
/// </summary>
public enum AliasTypeCategory
{
    /// <summary>
    ///     The type is an aggregate contract or aggregate state type.
    /// </summary>
    Aggregate,

    /// <summary>
    ///     The type is a command contract.
    /// </summary>
    Command,

    /// <summary>
    ///     The type is a generic contract or DTO/record support type.
    /// </summary>
    Contract,

    /// <summary>
    ///     The type is an event contract.
    /// </summary>
    Event,

    /// <summary>
    ///     The type is an aliased grain implementation.
    /// </summary>
    GrainImplementation,

    /// <summary>
    ///     The type is a grain interface.
    /// </summary>
    GrainInterface,

    /// <summary>
    ///     The type is a projection contract or projection state type.
    /// </summary>
    Projection,
}