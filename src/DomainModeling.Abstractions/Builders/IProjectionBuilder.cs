namespace Mississippi.DomainModeling.Abstractions.Builders;

/// <summary>
///     Builder contract for composing projection registrations in the runtime.
/// </summary>
/// <remarks>
///     <para>
///         Projections are the read-side domain surfaces. Each projection needs its root type,
///         reducers, and snapshot support registered.
///     </para>
///     <para>
///         Usage: <c>runtime.Projections(projections =&gt; projections.AddBankAccountBalanceProjection())</c>.
///     </para>
/// </remarks>
public interface IProjectionBuilder
{
    /// <summary>
    ///     Validates the current projection builder configuration.
    /// </summary>
    void Validate();
}