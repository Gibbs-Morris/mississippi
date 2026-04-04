using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Computes the deterministic workflow hash for a saga's recovery and step metadata.
/// </summary>
internal static class SagaStepHash
{
    /// <summary>
    ///     Computes the deterministic workflow hash for the supplied recovery metadata and ordered steps.
    /// </summary>
    /// <param name="recovery">The saga-level recovery metadata.</param>
    /// <param name="steps">The ordered saga step metadata.</param>
    /// <returns>The stable uppercase hexadecimal workflow hash.</returns>
    public static string Compute(
        SagaRecoveryInfo recovery,
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        ArgumentNullException.ThrowIfNull(recovery);
        ArgumentNullException.ThrowIfNull(steps);
        StringBuilder builder = new();
        builder.Append("Recovery=")
            .Append(recovery.Mode)
            .Append(':');

        if (recovery.Profile is null)
        {
            builder.Append("Profile:null");
        }
        else
        {
            builder.Append("Profile:value:")
                .Append(recovery.Profile.Length)
                .Append(':')
                .Append(recovery.Profile);
        }

        for (int i = 0; i < steps.Count; i++)
        {
            SagaStepInfo step = steps[i];
            builder.Append('|');

            string stepTypeName = step.StepType.FullName ?? step.StepType.Name;
            builder.Append(step.StepIndex)
                .Append(':')
                .Append(step.StepName)
                .Append(':')
                .Append(stepTypeName)
                .Append(':')
                .Append(step.HasCompensation)
                .Append(':')
                .Append(step.ForwardRecoveryPolicy)
                .Append(':')
                .Append(step.CompensationRecoveryPolicy?.ToString() ?? "NONE");
        }

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }
}