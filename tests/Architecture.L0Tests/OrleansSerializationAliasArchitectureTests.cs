using System;
using System.Collections.Generic;
using System.Linq;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing Orleans alias and type name alignment.
/// </summary>
public sealed class OrleansSerializationAliasArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    ///     Verifies that Orleans type aliases align to the current fully qualified type name.
    /// </summary>
    [Fact]
    public void OrleansTypeAliasesShouldMatchCurrentFullTypeName()
    {
        List<string> mismatches = MississippiAssemblies.SelectMany(assembly => assembly.GetTypes())
            .Select(type => new
            {
                Type = type,
                Alias = type.GetCustomAttributesData()
                    .Where(attribute => attribute.AttributeType.FullName == "Orleans.AliasAttribute")
                    .Select(attribute => attribute.ConstructorArguments[0].Value as string)
                    .SingleOrDefault(),
            })
            .Where(result =>
                !string.IsNullOrWhiteSpace(result.Alias) && result.Alias.Contains('.', StringComparison.Ordinal))
            .Where(result => result.Alias != result.Type.FullName)
            .Select(result => $"{result.Type.FullName} => {result.Alias}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
        Assert.True(
            mismatches.Count == 0,
            $"Found Orleans alias mismatches:{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
    }
}