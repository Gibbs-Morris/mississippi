namespace Mississippi.DomainModeling.Abstractions.Builders;

/// <summary>
///     Builder contract for composing saga registrations in the runtime.
/// </summary>
/// <remarks>
///     <para>
///         Sagas are long-running process orchestrations. Each saga needs its orchestration
///         state type, steps, reducers, and snapshot support registered.
///     </para>
///     <para>
///         Usage: <c>runtime.Sagas(sagas =&gt; sagas.AddMoneyTransferSaga())</c>.
///     </para>
/// </remarks>
public interface ISagaBuilder
{
    /// <summary>
    ///     Validates the current saga builder configuration.
    /// </summary>
    void Validate();
}