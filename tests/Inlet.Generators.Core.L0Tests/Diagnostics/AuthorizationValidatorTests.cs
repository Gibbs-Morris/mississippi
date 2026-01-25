using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mississippi.Inlet.Generators.Core.Diagnostics;

using NSubstitute;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Diagnostics;

/// <summary>
///     Tests for <see cref="AuthorizationValidator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Authorization Validator")]
public class AuthorizationValidatorTests
{
    /// <summary>
    ///     Creates a mock named type symbol with specified name and locations.
    /// </summary>
    private static INamedTypeSymbol CreateCommandSymbol(
        string name,
        string displayString
    )
    {
        INamedTypeSymbol symbol = Substitute.For<INamedTypeSymbol>();
        symbol.Name.Returns(name);
        symbol.ToDisplayString().Returns(displayString);
        symbol.Locations.Returns([]);
        return symbol;
    }

    /// <summary>
    ///     GetSecurityEnforcementSettings should return null when attribute not found.
    /// </summary>
    [Fact]
    public void GetSecurityEnforcementSettingsReturnsNullWhenAttributeNotFound()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        SecurityEnforcementSettings? result = AuthorizationValidator.GetSecurityEnforcementSettings(compilation);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetSecurityEnforcementSettings should throw when compilation is null.
    /// </summary>
    [Fact]
    public void GetSecurityEnforcementSettingsThrowsWhenCompilationIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AuthorizationValidator.GetSecurityEnforcementSettings(null!));
    }

    /// <summary>
    ///     ValidateCommandAuthorization should return empty when no security settings and no conflict.
    /// </summary>
    [Fact]
    public void ValidateCommandAuthorizationReturnsEmptyWhenNoSecuritySettings()
    {
        INamedTypeSymbol commandSymbol = CreateCommandSymbol("DepositFunds", "MyApp.DepositFunds");
        AttributeData commandAttr = Substitute.For<AttributeData>();
        AttributeData aggregateAttr = Substitute.For<AttributeData>();
        commandAttr.NamedArguments.Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        aggregateAttr.NamedArguments.Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        IEnumerable<Diagnostic> diagnostics = AuthorizationValidator.ValidateCommandAuthorization(
            commandSymbol,
            "BankAccount",
            commandAttr,
            aggregateAttr,
            null);
        Assert.Empty(diagnostics);
    }

    /// <summary>
    ///     ValidateCommandAuthorization should throw when commandSymbol is null.
    /// </summary>
    [Fact]
    public void ValidateCommandAuthorizationThrowsWhenCommandSymbolIsNull()
    {
        AttributeData commandAttr = Substitute.For<AttributeData>();
        AttributeData aggregateAttr = Substitute.For<AttributeData>();
        Assert.Throws<ArgumentNullException>(() =>
            AuthorizationValidator
                .ValidateCommandAuthorization(null!, "TestAggregate", commandAttr, aggregateAttr, null)
                .ToList());
    }

    /// <summary>
    ///     ValidateProjectionAuthorization should return empty when no security settings.
    /// </summary>
    [Fact]
    public void ValidateProjectionAuthorizationReturnsEmptyWhenNoSecuritySettings()
    {
        INamedTypeSymbol projectionSymbol = CreateCommandSymbol("BalanceProjection", "MyApp.BalanceProjection");
        AttributeData projectionAttr = Substitute.For<AttributeData>();
        projectionAttr.NamedArguments.Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        IEnumerable<Diagnostic> diagnostics = AuthorizationValidator.ValidateProjectionAuthorization(
            projectionSymbol,
            projectionAttr,
            null);
        Assert.Empty(diagnostics);
    }

    /// <summary>
    ///     ValidateProjectionAuthorization should throw when projectionSymbol is null.
    /// </summary>
    [Fact]
    public void ValidateProjectionAuthorizationThrowsWhenProjectionSymbolIsNull()
    {
        AttributeData projectionAttr = Substitute.For<AttributeData>();
        Assert.Throws<ArgumentNullException>(() =>
            AuthorizationValidator.ValidateProjectionAuthorization(null!, projectionAttr, null).ToList());
    }
}