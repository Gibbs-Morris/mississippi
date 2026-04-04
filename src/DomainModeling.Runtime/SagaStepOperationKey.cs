using System;
using System.Globalization;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Computes stable framework operation identities for saga step execution.
/// </summary>
internal static class SagaStepOperationKey
{
    /// <summary>
    ///     Computes the stable operation key for the specified saga step operation.
    /// </summary>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="direction">The execution direction.</param>
    /// <returns>The stable operation key for the step operation.</returns>
    public static string Compute(
        Guid sagaId,
        int stepIndex,
        SagaExecutionDirection direction
    ) =>
        string.Create(CultureInfo.InvariantCulture, $"{sagaId:N}:{direction}:{stepIndex}");
}