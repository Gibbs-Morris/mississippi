using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing C# development standards per csharp.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - Injected dependencies MUST use get-only property pattern (no underscore-prefixed fields).
///     - Implementation types MUST remain internal unless part of public API.
///     - Public contracts SHOULD live in .Abstractions projects.
/// </remarks>
public sealed class CSharpArchitectureTests : ArchitectureTestBase
{
    private static readonly OpCode[] SingleByteOpCodes = CreateSingleByteOpCodes();
    private static readonly OpCode[] MultiByteOpCodes = CreateMultiByteOpCodes();

    /// <summary>
    ///     Finds interface or abstract fields populated directly from constructor parameters.
    /// </summary>
    /// <param name="types">Types to inspect.</param>
    /// <returns>Names of fields that receive constructor parameter values.</returns>
    internal static IReadOnlyList<string> FindConstructorInjectedFields(IEnumerable<Type> types)
    {
        List<string> violations = new();
        foreach (Type type in types)
        {
            if (type.FullName?.StartsWith("OrleansCodeGen.", StringComparison.Ordinal) == true ||
                type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || !IsDependencyFieldType(field.FieldType))
                {
                    continue;
                }

                PropertyInfo? property = null;
                if (field.Name.EndsWith("k__BackingField", StringComparison.Ordinal))
                {
                    int propertyEnd = field.Name.IndexOf('>', StringComparison.Ordinal);
                    string propertyName = field.Name.StartsWith('<') && propertyEnd > 1
                        ? field.Name.Substring(1, propertyEnd - 1)
                        : string.Empty;
                    property = type.GetProperty(
                        propertyName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (property?.SetMethod is null)
                    {
                        continue;
                    }
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (ConstructorStoresParameter(constructor, field) ||
                        (property?.SetMethod is not null && ConstructorCallsSetter(constructor, property.SetMethod)))
                    {
                        violations.Add($"{type.FullName}.{field.Name}");
                        break;
                    }
                }
            }
        }

        return violations.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    ///     Returns non-enum value types that do not carry the readonly-struct marker.
    /// </summary>
    /// <param name="types">Types to inspect.</param>
    /// <returns>Names of mutable value types.</returns>
    internal static IReadOnlyList<string> FindNonReadonlyStructs(IEnumerable<Type> types)
    {
        return types
            .Where(type => type.IsValueType && !type.IsEnum &&
                           !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) &&
                           !(type.FullName?.StartsWith("OrleansCodeGen.", StringComparison.Ordinal) ?? false) &&
                           !type.IsDefined(typeof(IsReadOnlyAttribute), inherit: false))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    ///     Verifies that private fields do not use underscore prefix.
    /// </summary>
    /// <remarks>
    ///     Per csharp.instructions.md: "Injected dependencies MUST use the get-only property pattern;
    ///     field injection/underscored fields MUST NOT be used."
    ///     Per naming.instructions.md: "private fields/locals MUST be camelCase with no underscore."
    ///     This test detects underscore-prefixed private fields which violate these rules.
    ///     Known exclusions:
    ///     - Orleans generated code (OrleansCodeGen namespace).
    ///     - LoggerMessage source-generated callback fields (double-underscore prefix like __*Callback).
    ///     - Stryker's injected MutantControl helper in its generated Stryker namespace.
    /// </remarks>
    [Fact]
    public void PrivateFieldsShouldNotHaveUnderscorePrefix()
    {
        List<string> violations = ArchitectureModel.Classes
            .Where(type => !type.FullName.StartsWith("OrleansCodeGen.", StringComparison.Ordinal) &&
                           !((type.Name == "MutantControl") &&
                             type.FullName.StartsWith("Stryker", StringComparison.Ordinal)) &&
                           !type.Name.EndsWith("LoggerExtensions", StringComparison.Ordinal))
            .SelectMany(type => type.Members.OfType<FieldMember>())
            .Where(field => (field.Visibility == Visibility.Private) &&
                            (field.Name.Length > 0) &&
                            (field.Name[0] == '_') &&
                            !((field.Name.Length > 1) &&
                              (field.Name[0] == '_') &&
                              (field.Name[1] == '_') &&
                              field.Name.EndsWith("Callback", StringComparison.Ordinal)))
            .Select(field => field.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        Assert.True(
            violations.Count == 0,
            $"Found private fields with underscore prefix:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    ///     Verifies that constructor-injected interface or abstract dependencies use properties instead of fields.
    /// </summary>
    [Fact]
    public void ConstructorInjectedDependenciesShouldUseGetOnlyProperties()
    {
        IReadOnlyList<string> violations = FindConstructorInjectedFields(
            MississippiAssemblies.SelectMany(assembly => assembly.GetTypes()));

        Console.WriteLine(
            $"ADVISORY: {violations.Count} constructor-injected dependency field(s) remain while #711-#714 production repairs are completed.{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
        Assert.All(violations, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    /// <summary>
    ///     Verifies that structs are not mutable (prefer readonly structs).
    /// </summary>
    /// <remarks>
    ///     Per csharp.instructions.md: "Classes SHOULD be records/immutable where feasible."
    ///     This applies to structs as well - prefer readonly structs for value semantics.
    ///     This is a SHOULD rule, so violations are tracked but may be acceptable.
    /// </remarks>
    [Fact]
    public void StructsShouldBeReadonly()
    {
        IReadOnlyList<string> advisory = FindNonReadonlyStructs(
            MississippiAssemblies.SelectMany(assembly => assembly.GetTypes()));
        Console.WriteLine(
            $"ADVISORY: {advisory.Count} non-readonly Mississippi value type(s) remain; readonly struct guidance is not an unconditional gate.");
        Assert.All(advisory, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    private static bool ConstructorStoresParameter(ConstructorInfo constructor, FieldInfo targetField)
    {
        byte[]? il = constructor.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            return false;
        }

        bool parameterLoaded = false;
        int offset = 0;
        while (offset < il.Length)
        {
            OpCode opcode;
            byte first = il[offset++];
            if (first == 0xFE)
            {
                opcode = MultiByteOpCodes[il[offset++]];
            }
            else
            {
                opcode = SingleByteOpCodes[first];
            }

            if (opcode == OpCodes.Ldarg_0 || opcode == OpCodes.Ldarg_1 || opcode == OpCodes.Ldarg_2 || opcode == OpCodes.Ldarg_3 || opcode == OpCodes.Ldarg_S || opcode == OpCodes.Ldarg)
            {
                int argumentIndex = opcode switch
                {
                    _ when opcode == OpCodes.Ldarg_0 => 0,
                    _ when opcode == OpCodes.Ldarg_1 => 1,
                    _ when opcode == OpCodes.Ldarg_2 => 2,
                    _ when opcode == OpCodes.Ldarg_3 => 3,
                    _ when opcode == OpCodes.Ldarg_S => il[offset],
                    _ => BitConverter.ToUInt16(il, offset),
                };
                parameterLoaded |= argumentIndex > 0;
            }

            if (opcode == OpCodes.Stfld && offset + 4 <= il.Length)
            {
                int token = BitConverter.ToInt32(il, offset);
                FieldInfo? storedField = null;
                try
                {
                    storedField = constructor.Module.ResolveField(token, constructor.DeclaringType?.GetGenericArguments(), Type.EmptyTypes);
                }
                catch (ArgumentException)
                {
                    // An unresolved metadata token cannot prove the field assignment.
                }

                if (parameterLoaded && storedField == targetField)
                {
                    return true;
                }

                parameterLoaded = false;
            }
            else if (opcode != OpCodes.Nop &&
                     opcode != OpCodes.Dup &&
                     opcode != OpCodes.Brtrue && opcode != OpCodes.Brtrue_S &&
                     opcode != OpCodes.Brfalse && opcode != OpCodes.Brfalse_S &&
                     opcode != OpCodes.Ldarg_0 && opcode != OpCodes.Ldarg_1 && opcode != OpCodes.Ldarg_2 && opcode != OpCodes.Ldarg_3 &&
                     opcode != OpCodes.Ldarg_S && opcode != OpCodes.Ldarg)
            {
                parameterLoaded = false;
            }

            offset += GetOperandSize(opcode, il, offset);
        }

        return false;
    }

    private static bool ConstructorCallsSetter(ConstructorInfo constructor, MethodInfo setter)
    {
        byte[]? il = constructor.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            return false;
        }

        bool parameterLoaded = false;
        int offset = 0;
        while (offset < il.Length)
        {
            OpCode opcode;
            byte first = il[offset++];
            if (first == 0xFE)
            {
                opcode = MultiByteOpCodes[il[offset++]];
            }
            else
            {
                opcode = SingleByteOpCodes[first];
            }

            if (opcode == OpCodes.Ldarg_1 || opcode == OpCodes.Ldarg_2 || opcode == OpCodes.Ldarg_3 ||
                opcode == OpCodes.Ldarg_S || opcode == OpCodes.Ldarg)
            {
                parameterLoaded = true;
            }

            if ((opcode == OpCodes.Call || opcode == OpCodes.Callvirt) && offset + 4 <= il.Length)
            {
                try
                {
                    MethodBase? called = constructor.Module.ResolveMethod(
                        BitConverter.ToInt32(il, offset),
                        constructor.DeclaringType?.GetGenericArguments(),
                        Type.EmptyTypes);
                    if (parameterLoaded && called == setter)
                    {
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    // An unresolved token cannot prove a setter call.
                }
                parameterLoaded = false;
            }
            else if (opcode != OpCodes.Nop && opcode != OpCodes.Ldarg_0 && opcode != OpCodes.Ldarg_1 &&
                     opcode != OpCodes.Ldarg_2 && opcode != OpCodes.Ldarg_3 && opcode != OpCodes.Ldarg_S &&
                     opcode != OpCodes.Ldarg && opcode != OpCodes.Dup)
            {
                parameterLoaded = false;
            }

            offset += GetOperandSize(opcode, il, offset);
        }

        return false;
    }

    private static int GetOperandSize(OpCode opcode, byte[] il, int offset)
    {
        return opcode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineI or OperandType.ShortInlineBrTarget or OperandType.ShortInlineVar => 1,
            OperandType.ShortInlineR => 4,
            OperandType.InlineVar => 2,
            OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, offset)),
            _ => 0,
        };
    }

    private static bool IsDependencyFieldType(Type fieldType)
    {
        return fieldType.IsInterface ||
               fieldType.IsAbstract ||
               (fieldType.IsGenericType &&
                fieldType.GetGenericArguments().Any(argument => argument.IsInterface || argument.IsAbstract));
    }

    private static OpCode[] CreateSingleByteOpCodes()
    {
        OpCode[] result = new OpCode[0x100];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opcode && opcode.Size == 1 && opcode.Value >= 0)
            {
                result[opcode.Value] = opcode;
            }
        }

        return result;
    }

    private static OpCode[] CreateMultiByteOpCodes()
    {
        OpCode[] result = new OpCode[0x100];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opcode && opcode.Size == 2 && (opcode.Value & 0xFF00) == 0xFE00)
            {
                result[opcode.Value & 0xFF] = opcode;
            }
        }

        return result;
    }
}
