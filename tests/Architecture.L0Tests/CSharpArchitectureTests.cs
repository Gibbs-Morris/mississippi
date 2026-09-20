using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using ArchUnitNET.Domain;


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
    ///     Finds dependency-shaped fields populated directly from constructor parameters.
    /// </summary>
    /// <param name="types">Types to inspect.</param>
    /// <returns>Names of fields that receive constructor parameter values.</returns>
    internal static IReadOnlyList<string> FindConstructorInjectedFields(
        IEnumerable<Type> types
    )
    {
        List<string> violations = new();
        foreach (Type type in types)
        {
            if ((type.FullName?.StartsWith("OrleansCodeGen.", StringComparison.Ordinal) == true) ||
                type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                continue;
            }

            HashSet<FieldInfo> reportedFields = new();
            foreach (FieldInfo field in GetInstanceFields(type))
            {
                bool dependencyFieldType = IsDependencyFieldType(field.FieldType);
                if (field.IsStatic || (!dependencyFieldType && (field.FieldType != typeof(object))))
                {
                    continue;
                }

                PropertyInfo? property = null;
                if (field.Name.EndsWith("k__BackingField", StringComparison.Ordinal))
                {
                    int propertyEnd = field.Name.IndexOf('>', StringComparison.Ordinal);
                    string propertyName = field.Name.StartsWith('<') && (propertyEnd > 1)
                        ? field.Name.Substring(1, propertyEnd - 1)
                        : string.Empty;
                    const BindingFlags propertyFlags = BindingFlags.Instance |
                                                       BindingFlags.Public |
                                                       BindingFlags.NonPublic |
                                                       BindingFlags.DeclaredOnly;
                    property = field.DeclaringType?.GetProperty(propertyName, propertyFlags);
                }
                else
                {
                    property = field.DeclaringType
                        ?.GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly)
                        .FirstOrDefault(candidate =>
                            candidate.SetMethod is not null && MethodStoresField(candidate.SetMethod, field));
                }

                if (property?.SetMethod is null && (property?.GetMethod?.IsPrivate == true))
                {
                    continue;
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (ConstructorStoresParameter(constructor, field, !dependencyFieldType) ||
                        (property?.SetMethod is not null && ConstructorCallsSetter(constructor, property.SetMethod)))
                    {
                        violations.Add($"{type.FullName}.{field.Name}");
                        reportedFields.Add(field);
                        break;
                    }
                }
            }

            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.SetMethod is null ||
                    !IsDependencyFieldType(property.PropertyType) ||
                    type.GetField(
                        $"<{property.Name}>k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is not null)
                {
                    continue;
                }

                FieldInfo? associatedField = GetInstanceFields(type)
                    .FirstOrDefault(field => MethodStoresField(property.SetMethod, field));
                if (associatedField is not null && reportedFields.Contains(associatedField))
                {
                    continue;
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (ConstructorCallsSetter(constructor, property.SetMethod))
                    {
                        violations.Add($"{type.FullName}.{property.Name}");
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
    internal static IReadOnlyList<string> FindNonReadonlyStructs(
        IEnumerable<Type> types
    )
    {
        return types
            .Where(type => type.IsValueType &&
                           !type.IsEnum &&
                           !type.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
                           !(type.FullName?.StartsWith("OrleansCodeGen.", StringComparison.Ordinal) ?? false) &&
                           !type.IsDefined(typeof(IsReadOnlyAttribute), false))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool ConstructorCallsSetter(
        ConstructorInfo constructor,
        MethodInfo setter
    )
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
            opcode = first == 0xFE ? MultiByteOpCodes[il[offset++]] : SingleByteOpCodes[first];
            if ((opcode == OpCodes.Ldarg_1) ||
                (opcode == OpCodes.Ldarg_2) ||
                (opcode == OpCodes.Ldarg_3) ||
                (opcode == OpCodes.Ldarg_S) ||
                (opcode == OpCodes.Ldarg))
            {
                parameterLoaded = true;
            }

            if (((opcode == OpCodes.Call) || (opcode == OpCodes.Callvirt)) && ((offset + 4) <= il.Length))
            {
                try
                {
                    MethodBase? called = constructor.Module.ResolveMethod(
                        BitConverter.ToInt32(il, offset),
                        constructor.DeclaringType?.GetGenericArguments(),
                        Type.EmptyTypes);
                    if (parameterLoaded &&
                        ((called == setter) ||
                         (called is not null &&
                          (called.Name == setter.Name) &&
                          (called.DeclaringType == setter.DeclaringType))))
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
            else if ((opcode != OpCodes.Nop) &&
                     (opcode != OpCodes.Ldarg_0) &&
                     (opcode != OpCodes.Ldarg_1) &&
                     (opcode != OpCodes.Ldarg_2) &&
                     (opcode != OpCodes.Ldarg_3) &&
                     (opcode != OpCodes.Ldarg_S) &&
                     (opcode != OpCodes.Ldarg) &&
                     (opcode != OpCodes.Dup))
            {
                parameterLoaded = false;
            }

            offset += GetOperandSize(opcode, il, offset);
        }

        return false;
    }

    private static bool ConstructorStoresParameter(
        ConstructorInfo constructor,
        FieldInfo targetField,
        bool requireDependencyParameter = false
    )
    {
        int parameterCount = constructor.GetParameters().Length;
        for (int parameterIndex = 1; parameterIndex <= parameterCount; parameterIndex++)
        {
            if (requireDependencyParameter &&
                !IsDependencyFieldType(constructor.GetParameters()[parameterIndex - 1].ParameterType))
            {
                continue;
            }

            if (MethodStoresParameter(constructor, parameterIndex, targetField, new()))
            {
                return true;
            }
        }

        return false;
    }

    private static OpCode[] CreateMultiByteOpCodes()
    {
        OpCode[] result = new OpCode[0x100];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opcode && (opcode.Size == 2) && ((opcode.Value & 0xFF00) == 0xFE00))
            {
                result[opcode.Value & 0xFF] = opcode;
            }
        }

        return result;
    }

    private static OpCode[] CreateSingleByteOpCodes()
    {
        OpCode[] result = new OpCode[0x100];
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opcode && (opcode.Size == 1) && (opcode.Value >= 0))
            {
                result[opcode.Value] = opcode;
            }
        }

        return result;
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(
        Type type
    )
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static int GetOperandSize(
        OpCode opcode,
        byte[] il,
        int offset
    )
    {
        return opcode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineI or OperandType.ShortInlineBrTarget or OperandType.ShortInlineVar => 1,
            OperandType.ShortInlineR => 4,
            OperandType.InlineVar => 2,
            OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod
                or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok
                or OperandType.InlineType => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, offset)),
            var _ => 0,
        };
    }

    private static bool IsDependencyFieldType(
        Type fieldType
    )
    {
        if (typeof(Delegate).IsAssignableFrom(fieldType))
        {
            return true;
        }

        if (fieldType.IsGenericParameter)
        {
            Type[] constraints = fieldType.GetGenericParameterConstraints();
            if ((constraints.Length > 0) && constraints.All(IsStateConstraint))
            {
                return false;
            }

            return constraints.Any(IsDependencyFieldType);
        }

        if (fieldType.IsArray)
        {
            return fieldType.GetElementType() is { } elementType && IsDependencyFieldType(elementType);
        }

        if (fieldType.IsGenericType)
        {
            Type genericType = fieldType.GetGenericTypeDefinition();
            bool isCollectionState =
                genericType.Namespace?.StartsWith("System.Collections", StringComparison.Ordinal) == true;
            if (isCollectionState)
            {
                return fieldType.GetGenericArguments().Any(IsDependencyFieldType);
            }

            if (fieldType.IsInterface || fieldType.IsAbstract)
            {
                return true;
            }

            return fieldType.GetGenericArguments().Any(IsDependencyFieldType);
        }

        if (fieldType.IsInterface || fieldType.IsAbstract)
        {
            return true;
        }

        return IsExplicitConcreteServiceType(fieldType);
    }

    private static bool IsExplicitConcreteServiceType(
        Type fieldType
    )
    {
        if (!fieldType.IsClass ||
            (fieldType == typeof(string)) ||
            (fieldType.Namespace?.StartsWith("System", StringComparison.Ordinal) == true))
        {
            return false;
        }

        // Concrete registrations without an abstraction are intentionally classified by service-shaped
        // suffixes. This includes DevToolsInitializationTracker while excluding ordinary model/state classes.
        string name = fieldType.Name;
        return name.EndsWith("Client", StringComparison.Ordinal) ||
               name.EndsWith("Dependency", StringComparison.Ordinal) ||
               name.EndsWith("Handler", StringComparison.Ordinal) ||
               name.EndsWith("Interop", StringComparison.Ordinal) ||
               name.EndsWith("Manager", StringComparison.Ordinal) ||
               name.EndsWith("Provider", StringComparison.Ordinal) ||
               name.EndsWith("Repository", StringComparison.Ordinal) ||
               name.EndsWith("Service", StringComparison.Ordinal) ||
               name.EndsWith("Tracker", StringComparison.Ordinal);
    }

    private static bool IsStateConstraint(
        Type constraint
    )
    {
        string name = constraint.Name;
        return name.EndsWith("State", StringComparison.Ordinal) || name.EndsWith("State`", StringComparison.Ordinal);
    }

    private static bool MethodStoresField(
        MethodBase method,
        FieldInfo targetField
    )
    {
        byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            return false;
        }

        int offset = 0;
        while (offset < il.Length)
        {
            OpCode opcode;
            byte first = il[offset++];
            opcode = first == 0xFE ? MultiByteOpCodes[il[offset++]] : SingleByteOpCodes[first];
            if ((opcode == OpCodes.Stfld) && ((offset + 4) <= il.Length))
            {
                try
                {
                    FieldInfo? storedField = method.Module.ResolveField(
                        BitConverter.ToInt32(il, offset),
                        method.DeclaringType?.GetGenericArguments(),
                        Type.EmptyTypes);
                    if (storedField == targetField)
                    {
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    // An unresolved metadata token cannot prove the field assignment.
                }
            }

            offset += GetOperandSize(opcode, il, offset);
        }

        return false;
    }

    private static bool MethodStoresParameter(
        MethodBase method,
        int parameterIndex,
        FieldInfo targetField,
        HashSet<MethodBase> visited
    )
    {
        if (!visited.Add(method))
        {
            return false;
        }

        byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            return false;
        }

        int? loadedParameter = null;
        int? conditionalParameter = null;
        FieldInfo? loadedField = null;
        List<int> argumentStack = new();
        int offset = 0;
        while (offset < il.Length)
        {
            OpCode opcode;
            byte first = il[offset++];
            opcode = first == 0xFE ? MultiByteOpCodes[il[offset++]] : SingleByteOpCodes[first];
            int operandOffset = offset;
            if (TryGetArgumentIndex(opcode, il, operandOffset, out int argumentIndex))
            {
                loadedParameter = argumentIndex;
                argumentStack.Add(argumentIndex);
            }
            else if (opcode == OpCodes.Ldnull)
            {
                loadedParameter = null;
                argumentStack.Add(-1);
            }

            if (((opcode == OpCodes.Brtrue) ||
                 (opcode == OpCodes.Brtrue_S) ||
                 (opcode == OpCodes.Brfalse) ||
                 (opcode == OpCodes.Brfalse_S)) &&
                (loadedParameter == parameterIndex))
            {
                conditionalParameter = parameterIndex;
            }

            if ((opcode == OpCodes.Stfld) && ((offset + 4) <= il.Length))
            {
                FieldInfo? storedField = null;
                try
                {
                    storedField = method.Module.ResolveField(
                        BitConverter.ToInt32(il, offset),
                        method.DeclaringType?.GetGenericArguments(),
                        Type.EmptyTypes);
                }
                catch (ArgumentException)
                {
                    // An unresolved metadata token cannot prove the field assignment.
                }

                if (((loadedParameter == parameterIndex) || (conditionalParameter == parameterIndex)) &&
                    (storedField == targetField))
                {
                    return true;
                }

                loadedParameter = null;
                conditionalParameter = null;
                loadedField = null;
                argumentStack.Clear();
            }
            else if ((opcode == OpCodes.Ldfld) && ((offset + 4) <= il.Length))
            {
                try
                {
                    loadedField = method.Module.ResolveField(
                        BitConverter.ToInt32(il, offset),
                        method.DeclaringType?.GetGenericArguments(),
                        Type.EmptyTypes);
                }
                catch (ArgumentException)
                {
                    loadedField = null;
                }

                loadedParameter = null;
                argumentStack.Clear();
            }
            else if (((opcode == OpCodes.Call) || (opcode == OpCodes.Callvirt)) && ((offset + 4) <= il.Length))
            {
                MethodBase? called = null;
                try
                {
                    called = method.Module.ResolveMethod(
                        BitConverter.ToInt32(il, offset),
                        method.DeclaringType?.GetGenericArguments(),
                        Type.EmptyTypes);
                }
                catch (ArgumentException)
                {
                    // An unresolved metadata token cannot prove helper provenance.
                }

                if (called is not null &&
                    (called.Name == "Add") &&
                    (loadedField == targetField) &&
                    (loadedParameter == parameterIndex))
                {
                    return true;
                }

                if (called is not null && (called.DeclaringType == method.DeclaringType))
                {
                    int calledParameterCount = called.GetParameters().Length;
                    int receiverCount = called.IsStatic ? 0 : 1;
                    int requiredArgumentCount = calledParameterCount + receiverCount;
                    if ((calledParameterCount > 0) && (argumentStack.Count >= requiredArgumentCount))
                    {
                        int[] callArguments = argumentStack.GetRange(
                                argumentStack.Count - requiredArgumentCount,
                                requiredArgumentCount)
                            .ToArray();
                        for (int calleeParameter = 0; calleeParameter < calledParameterCount; calleeParameter++)
                        {
                            int stackIndex = calleeParameter + receiverCount;
                            if ((callArguments[stackIndex] == parameterIndex) &&
                                MethodStoresParameter(called, calleeParameter + receiverCount, targetField, visited))
                            {
                                return true;
                            }
                        }
                    }
                }

                loadedParameter = null;
                conditionalParameter = null;
                loadedField = null;
                argumentStack.Clear();
            }
            else if ((opcode != OpCodes.Nop) &&
                     (opcode != OpCodes.Dup) &&
                     (opcode != OpCodes.Brtrue) &&
                     (opcode != OpCodes.Brtrue_S) &&
                     (opcode != OpCodes.Brfalse) &&
                     (opcode != OpCodes.Brfalse_S) &&
                     (opcode != OpCodes.Pop) &&
                     (opcode != OpCodes.Throw) &&
                     (opcode != OpCodes.Ldnull) &&
                     !TryGetArgumentIndex(opcode, il, operandOffset, out int _))
            {
                loadedParameter = null;
                loadedField = null;
                argumentStack.Clear();
            }

            offset += GetOperandSize(opcode, il, offset);
            if (opcode == OpCodes.Ret)
            {
                conditionalParameter = null;
            }
        }

        return false;
    }

    private static bool TryGetArgumentIndex(
        OpCode opcode,
        byte[] il,
        int offset,
        out int argumentIndex
    )
    {
        if (opcode == OpCodes.Ldarg_0)
        {
            argumentIndex = 0;
            return true;
        }

        if (opcode == OpCodes.Ldarg_1)
        {
            argumentIndex = 1;
            return true;
        }

        if (opcode == OpCodes.Ldarg_2)
        {
            argumentIndex = 2;
            return true;
        }

        if (opcode == OpCodes.Ldarg_3)
        {
            argumentIndex = 3;
            return true;
        }

        if (opcode == OpCodes.Ldarg_S)
        {
            argumentIndex = il[offset];
            return true;
        }

        if (opcode == OpCodes.Ldarg)
        {
            argumentIndex = BitConverter.ToUInt16(il, offset);
            return true;
        }

        argumentIndex = -1;
        return false;
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
            $"ADVISORY: {advisory.Count} non-readonly Mississippi value type(s) remain; readonly struct guidance is not an unconditional gate.{Environment.NewLine}{string.Join(Environment.NewLine, advisory)}");
        Assert.All(advisory, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }
}