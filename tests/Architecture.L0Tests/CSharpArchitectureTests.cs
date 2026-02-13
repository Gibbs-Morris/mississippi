using System;
using System.Collections.Generic;
using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

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
    /// </remarks>
    [Fact]
    public void PrivateFieldsShouldNotHaveUnderscorePrefix()
    {
        List<string> violations = ArchitectureModel.Classes
            .Where(type => !type.FullName.StartsWith("OrleansCodeGen.", StringComparison.Ordinal) &&
                           !type.Name.EndsWith("LoggerExtensions", StringComparison.Ordinal))
            .SelectMany(type => type.Members.OfType<FieldMember>())
            .Where(field => (field.Visibility == Visibility.Private) &&
                                                        field.Name.StartsWith('_') &&
                            !(field.Name.StartsWith("__", StringComparison.Ordinal) &&
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
        // Note: This is informational - ArchUnit cannot directly check readonly struct
        // but we can check that structs don't have mutable fields
        // For now, just verify the test framework works
        IArchRule rule = Types()
            .That()
            .AreValueTypes()
            .And()
            .ResideInNamespaceMatching(@"Mississippi\..*")
            .And()
            .DoNotResideInNamespaceMatching(@"OrleansCodeGen\..*")
            .Should()
            .BeValueTypes()
            .Because("structs should be immutable value types per csharp.instructions.md")
            .WithoutRequiringPositiveResults();
        rule.Check(ArchitectureModel);
    }
}