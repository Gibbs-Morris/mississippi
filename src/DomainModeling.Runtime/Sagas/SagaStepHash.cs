using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Mississippi.DomainModeling.Abstractions;

using Orleans.Serialization.TypeSystem;


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

            string stepTypeName = RuntimeTypeNameFormatter.Format(step.StepType);
            builder.Append(
                CultureInfo.InvariantCulture,
                $"{step.StepIndex}:{step.StepName.Length}:{step.StepName}:{stepTypeName.Length}:{stepTypeName}:");
            AppendAssemblyIdentity(builder, step.StepType);
            builder.Append(':').Append(step.HasCompensation);
        }

        byte[] bytes = SHA256.HashData(new UTF8Encoding(false, true).GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }

    private static void AppendAssemblyIdentity(
        StringBuilder builder,
        Type type
    )
    {
        AssemblyName assemblyName = type.Assembly.GetName();
        assemblyName.Version = null;
        string identity = assemblyName.FullName;
        builder.Append(CultureInfo.InvariantCulture, $"{identity.Length}:{identity}");
        foreach (Type argument in type.GenericTypeArguments)
        {
            AppendAssemblyIdentity(builder, argument);
        }

        if (type.GetElementType() is { } element)
        {
            AppendAssemblyIdentity(builder, element);
        }
    }
}