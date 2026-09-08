using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.Sagas;

/// <summary>
///     Computes the persisted identity of an ordered saga workflow.
/// </summary>
internal static class SagaStepHash
{
    /// <summary>
    ///     Computes the hash used at saga start and before continuing execution.
    /// </summary>
    /// <param name="steps">The ordered saga step metadata.</param>
    /// <returns>The hexadecimal workflow hash.</returns>
    public static string Compute(
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        ArgumentNullException.ThrowIfNull(steps);
        StringBuilder builder = new();
        for (int i = 0; i < steps.Count; i++)
        {
            SagaStepInfo step = steps[i];
            if (i > 0)
            {
                builder.Append('|');
            }

            string stepTypeName = step.StepType.FullName ?? step.StepType.Name;
            builder.Append(step.StepIndex)
                .Append(':')
                .Append(step.StepName)
                .Append(':')
                .Append(stepTypeName)
                .Append(':')
                .Append(step.HasCompensation);
        }

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }
}