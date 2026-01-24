using Allure.Xunit.Attributes;

using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Naming;

/// <summary>
///     Tests for <see cref="NamingConventions" /> string manipulation utilities.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Naming Conventions")]
public class NamingConventionsTests
{
    /// <summary>
    ///     GetAggregateNameFromNamespace should extract aggregate name from valid namespace pattern.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceExtractsFromValidPattern()
    {
        string result =
            NamingConventions.GetAggregateNameFromNamespace("Spring.Domain.Aggregates.BankAccount.Commands")!;
        Assert.Equal("BankAccount", result);
    }

    /// <summary>
    ///     GetAggregateNameFromNamespace should handle nested aggregate names.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceHandlesNestedAggregateName()
    {
        string result = NamingConventions.GetAggregateNameFromNamespace(
            "MyApp.Domain.Aggregates.Customer.Order.Commands")!;
        Assert.Equal("Customer.Order", result);
    }

    /// <summary>
    ///     GetAggregateNameFromNamespace should return null for empty input.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceReturnsNullForEmptyInput()
    {
        string? result = NamingConventions.GetAggregateNameFromNamespace(string.Empty);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetAggregateNameFromNamespace should return null for null input.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceReturnsNullForNullInput()
    {
        string? result = NamingConventions.GetAggregateNameFromNamespace(null!);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetAggregateNameFromNamespace should return null when missing .Commands suffix.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceReturnsNullWhenMissingCommandsSuffix()
    {
        string? result = NamingConventions.GetAggregateNameFromNamespace("Spring.Domain.Aggregates.BankAccount");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetAggregateNameFromNamespace should return null when missing .Domain.Aggregates. segment.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceReturnsNullWhenMissingDomainAggregatesSegment()
    {
        string? result = NamingConventions.GetAggregateNameFromNamespace("Spring.BankAccount.Commands");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetAggregateNameFromNamespace should return null for namespace with only .Aggregates. but no .Domain.
    /// </summary>
    [Fact]
    public void GetAggregateNameFromNamespaceReturnsNullWhenMissingDomainPrefix()
    {
        string? result = NamingConventions.GetAggregateNameFromNamespace("Spring.Aggregates.BankAccount.Commands");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetClientActionEffectsNamespace should convert domain namespace to client action effects namespace.
    /// </summary>
    [Fact]
    public void GetClientActionEffectsNamespaceConvertsValidPattern()
    {
        string result =
            NamingConventions.GetClientActionEffectsNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.ActionEffects", result);
    }

    /// <summary>
    ///     GetClientActionEffectsNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientActionEffectsNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientActionEffectsNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate.ActionEffects", result);
    }

    /// <summary>
    ///     GetClientActionsNamespace should convert domain namespace to client actions namespace.
    /// </summary>
    [Fact]
    public void GetClientActionsNamespaceConvertsValidPattern()
    {
        string result = NamingConventions.GetClientActionsNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.Actions", result);
    }

    /// <summary>
    ///     GetClientActionsNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientActionsNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientActionsNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate.Actions", result);
    }

    /// <summary>
    ///     GetClientCommandDtoNamespace should convert valid domain pattern.
    /// </summary>
    [Fact]
    public void GetClientCommandDtoNamespaceConvertsValidPattern()
    {
        string result = NamingConventions.GetClientCommandDtoNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.Dtos", result);
    }

    /// <summary>
    ///     GetClientCommandDtoNamespace should return empty for empty input.
    /// </summary>
    [Fact]
    public void GetClientCommandDtoNamespaceReturnsEmptyForEmptyInput()
    {
        string result = NamingConventions.GetClientCommandDtoNamespace(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetClientCommandDtoNamespace should return null for null input.
    /// </summary>
    [Fact]
    public void GetClientCommandDtoNamespaceReturnsNullForNullInput()
    {
        string result = NamingConventions.GetClientCommandDtoNamespace(null!);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetClientCommandDtoNamespace should use fallback for non-matching pattern with .Commands.
    /// </summary>
    [Fact]
    public void GetClientCommandDtoNamespaceUsesFallbackForNonMatchingPatternWithCommands()
    {
        string result = NamingConventions.GetClientCommandDtoNamespace("MyApp.Domain.BankAccount.Commands");
        Assert.Equal("MyApp.Client.BankAccount.Dtos", result);
    }

    /// <summary>
    ///     GetClientCommandDtoNamespace should use last resort for unrecognized pattern.
    /// </summary>
    [Fact]
    public void GetClientCommandDtoNamespaceUsesLastResortForUnrecognizedPattern()
    {
        string result = NamingConventions.GetClientCommandDtoNamespace("SomeRandomNamespace");
        Assert.Equal("SomeRandomNamespace.Client.Dtos", result);
    }

    /// <summary>
    ///     GetClientCommandDtoNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientCommandDtoNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientCommandDtoNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate.Dtos", result);
    }

    /// <summary>
    ///     GetClientFeatureRootNamespace should return root namespace without sub-namespace.
    /// </summary>
    [Fact]
    public void GetClientFeatureRootNamespaceReturnsRootWithoutSubNamespace()
    {
        string result = NamingConventions.GetClientFeatureRootNamespace(
            "Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate", result);
    }

    /// <summary>
    ///     GetClientFeatureRootNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientFeatureRootNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientFeatureRootNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate", result);
    }

    /// <summary>
    ///     GetClientMappersNamespace should convert domain namespace to client mappers namespace.
    /// </summary>
    [Fact]
    public void GetClientMappersNamespaceConvertsValidPattern()
    {
        string result = NamingConventions.GetClientMappersNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.Mappers", result);
    }

    /// <summary>
    ///     GetClientMappersNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientMappersNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientMappersNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate.Mappers", result);
    }

    /// <summary>
    ///     GetClientNamespace should convert projection namespace.
    /// </summary>
    [Fact]
    public void GetClientNamespaceConvertsProjectionNamespace()
    {
        string result = NamingConventions.GetClientNamespace("Spring.Domain.Projections.BankAccountBalance");
        Assert.Equal("Spring.Client.Features.BankAccountBalance.Dtos", result);
    }

    /// <summary>
    ///     GetClientNamespace should handle namespace with .Domain. segment.
    /// </summary>
    [Fact]
    public void GetClientNamespaceHandlesDomainSegment()
    {
        string result = NamingConventions.GetClientNamespace("MyApp.Domain.Something");
        Assert.Equal("MyApp.Client.Something", result);
    }

    /// <summary>
    ///     GetClientNamespace should handle namespace ending with .Domain suffix.
    /// </summary>
    [Fact]
    public void GetClientNamespaceHandlesDomainSuffix()
    {
        string result = NamingConventions.GetClientNamespace("MyApp.Domain");
        Assert.Equal("MyApp.Client", result);
    }

    /// <summary>
    ///     GetClientNamespace should return empty for empty input.
    /// </summary>
    [Fact]
    public void GetClientNamespaceReturnsEmptyForEmptyInput()
    {
        string result = NamingConventions.GetClientNamespace(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetClientNamespace should return null for null input.
    /// </summary>
    [Fact]
    public void GetClientNamespaceReturnsNullForNullInput()
    {
        string result = NamingConventions.GetClientNamespace(null!);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetClientNamespace should use last resort for unrecognized pattern.
    /// </summary>
    [Fact]
    public void GetClientNamespaceUsesLastResortForUnrecognizedPattern()
    {
        string result = NamingConventions.GetClientNamespace("SomeRandomNamespace");
        Assert.Equal("SomeRandomNamespace.Client", result);
    }

    /// <summary>
    ///     GetClientNamespace with targetRootNamespace should return fallback for empty source.
    /// </summary>
    [Fact]
    public void GetClientNamespaceWithTargetReturnsFallbackForEmptySource()
    {
        string result = NamingConventions.GetClientNamespace(string.Empty, "MyApp.BlazorClient");
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetClientNamespace with targetRootNamespace should use target namespace for projections.
    /// </summary>
    [Fact]
    public void GetClientNamespaceWithTargetUsesTargetNamespaceForProjections()
    {
        string result = NamingConventions.GetClientNamespace(
            "MyApp.CoreDomainLogic.Projections.BankAccountBalance",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountBalance.Dtos", result);
    }

    /// <summary>
    ///     GetClientReducersNamespace should convert domain namespace to client reducers namespace.
    /// </summary>
    [Fact]
    public void GetClientReducersNamespaceConvertsValidPattern()
    {
        string result = NamingConventions.GetClientReducersNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.Reducers", result);
    }

    /// <summary>
    ///     GetClientReducersNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientReducersNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientReducersNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate.Reducers", result);
    }

    /// <summary>
    ///     GetClientStateNamespace should convert domain namespace to client state namespace.
    /// </summary>
    [Fact]
    public void GetClientStateNamespaceConvertsValidPattern()
    {
        string result = NamingConventions.GetClientStateNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.State", result);
    }

    /// <summary>
    ///     GetClientStateNamespace with targetRootNamespace should use target namespace.
    /// </summary>
    [Fact]
    public void GetClientStateNamespaceWithTargetUsesTargetNamespace()
    {
        string result = NamingConventions.GetClientStateNamespace(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands",
            "MyApp.BlazorClient");
        Assert.Equal("MyApp.BlazorClient.Features.BankAccountAggregate.State", result);
    }

    /// <summary>
    ///     GetCommandDtoName should append Dto suffix.
    /// </summary>
    [Fact]
    public void GetCommandDtoNameAppendsDtoSuffix()
    {
        string result = NamingConventions.GetCommandDtoName("DepositFunds");
        Assert.Equal("DepositFundsDto", result);
    }

    /// <summary>
    ///     GetCommandRequestDtoName should append RequestDto suffix.
    /// </summary>
    [Fact]
    public void GetCommandRequestDtoNameAppendsRequestDtoSuffix()
    {
        string result = NamingConventions.GetCommandRequestDtoName("DepositFunds");
        Assert.Equal("DepositFundsRequestDto", result);
    }

    /// <summary>
    ///     GetDtoName should append Dto suffix.
    /// </summary>
    [Fact]
    public void GetDtoNameAppendsDtoSuffix()
    {
        string result = NamingConventions.GetDtoName("BankAccountBalanceProjection");
        Assert.Equal("BankAccountBalanceProjectionDto", result);
    }

    /// <summary>
    ///     GetFeatureKey should handle type name without suffix.
    /// </summary>
    [Fact]
    public void GetFeatureKeyHandlesTypeNameWithoutSuffix()
    {
        string result = NamingConventions.GetFeatureKey("Customer");
        Assert.Equal("customer", result);
    }

    /// <summary>
    ///     GetFeatureKey should remove Aggregate suffix and convert to camelCase.
    /// </summary>
    [Fact]
    public void GetFeatureKeyRemovesAggregateSuffixAndConvertsToCamelCase()
    {
        string result = NamingConventions.GetFeatureKey("BankAccountAggregate");
        Assert.Equal("bankAccount", result);
    }

    /// <summary>
    ///     GetFeatureKey should remove Projection suffix and convert to camelCase.
    /// </summary>
    [Fact]
    public void GetFeatureKeyRemovesProjectionSuffixAndConvertsToCamelCase()
    {
        string result = NamingConventions.GetFeatureKey("BankAccountBalanceProjection");
        Assert.Equal("bankAccountBalance", result);
    }

    /// <summary>
    ///     GetRoutePrefix should remove Aggregate suffix and convert to kebab-case.
    /// </summary>
    [Fact]
    public void GetRoutePrefixRemovesAggregateSuffixAndConvertsToKebabCase()
    {
        string result = NamingConventions.GetRoutePrefix("BankAccountAggregate");
        Assert.Equal("bank-account", result);
    }

    /// <summary>
    ///     GetRoutePrefix should remove Projection suffix and convert to kebab-case.
    /// </summary>
    [Fact]
    public void GetRoutePrefixRemovesProjectionSuffixAndConvertsToKebabCase()
    {
        string result = NamingConventions.GetRoutePrefix("BankAccountBalanceProjection");
        Assert.Equal("bank-account-balance", result);
    }

    /// <summary>
    ///     GetServerCommandDtoNamespace should convert valid domain pattern.
    /// </summary>
    [Fact]
    public void GetServerCommandDtoNamespaceConvertsValidPattern()
    {
        string result = NamingConventions.GetServerCommandDtoNamespace("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Spring.Server.Controllers.Aggregates", result);
    }

    /// <summary>
    ///     GetServerCommandDtoNamespace should handle namespace with .Domain. segment.
    /// </summary>
    [Fact]
    public void GetServerCommandDtoNamespaceHandlesDomainSegment()
    {
        string result = NamingConventions.GetServerCommandDtoNamespace("MyApp.Domain.Something");
        Assert.Equal("MyApp.Server.Something", result);
    }

    /// <summary>
    ///     GetServerCommandDtoNamespace should handle namespace ending with .Domain suffix.
    /// </summary>
    [Fact]
    public void GetServerCommandDtoNamespaceHandlesDomainSuffix()
    {
        string result = NamingConventions.GetServerCommandDtoNamespace("MyApp.Domain");
        Assert.Equal("MyApp.Server", result);
    }

    /// <summary>
    ///     GetServerCommandDtoNamespace should return empty for empty input.
    /// </summary>
    [Fact]
    public void GetServerCommandDtoNamespaceReturnsEmptyForEmptyInput()
    {
        string result = NamingConventions.GetServerCommandDtoNamespace(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetServerCommandDtoNamespace should return null for null input.
    /// </summary>
    [Fact]
    public void GetServerCommandDtoNamespaceReturnsNullForNullInput()
    {
        string result = NamingConventions.GetServerCommandDtoNamespace(null!);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetServerCommandDtoNamespace should use last resort for unrecognized pattern.
    /// </summary>
    [Fact]
    public void GetServerCommandDtoNamespaceUsesLastResortForUnrecognizedPattern()
    {
        string result = NamingConventions.GetServerCommandDtoNamespace("SomeRandomNamespace");
        Assert.Equal("SomeRandomNamespace.Server", result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should convert aggregate namespace.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceConvertsAggregateNamespace()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace("Spring.Domain.Aggregates.BankAccount");
        Assert.Equal("Spring.Silo.Registrations", result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should convert projection namespace.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceConvertsProjectionNamespace()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace("Spring.Domain.Projections.BankAccountBalance");
        Assert.Equal("Spring.Silo.Registrations", result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should handle namespace with .Domain. segment.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceHandlesDomainSegment()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace("MyApp.Domain.Something");
        Assert.Equal("MyApp.Silo.Registrations", result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should handle namespace ending with .Domain suffix.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceHandlesDomainSuffix()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace("MyApp.Domain");
        Assert.Equal("MyApp.Silo.Registrations", result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should return empty for empty input.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceReturnsEmptyForEmptyInput()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should return null for null input.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceReturnsNullForNullInput()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace(null!);
        Assert.Null(result);
    }

    /// <summary>
    ///     GetSiloRegistrationNamespace should use last resort for unrecognized pattern.
    /// </summary>
    [Fact]
    public void GetSiloRegistrationNamespaceUsesLastResortForUnrecognizedPattern()
    {
        string result = NamingConventions.GetSiloRegistrationNamespace("SomeRandomNamespace");
        Assert.Equal("SomeRandomNamespace.Silo.Registrations", result);
    }

    /// <summary>
    ///     RemoveSuffix should remove suffix when present.
    /// </summary>
    [Fact]
    public void RemoveSuffixRemovesSuffixWhenPresent()
    {
        string result = NamingConventions.RemoveSuffix("BankAccountProjection", "Projection");
        Assert.Equal("BankAccount", result);
    }

    /// <summary>
    ///     RemoveSuffix should return empty for empty type name.
    /// </summary>
    [Fact]
    public void RemoveSuffixReturnsEmptyForEmptyTypeName()
    {
        string result = NamingConventions.RemoveSuffix(string.Empty, "Suffix");
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     RemoveSuffix should return input when suffix is empty.
    /// </summary>
    [Fact]
    public void RemoveSuffixReturnsInputWhenSuffixIsEmpty()
    {
        string result = NamingConventions.RemoveSuffix("BankAccount", string.Empty);
        Assert.Equal("BankAccount", result);
    }

    /// <summary>
    ///     RemoveSuffix should return input when suffix is null.
    /// </summary>
    [Fact]
    public void RemoveSuffixReturnsInputWhenSuffixIsNull()
    {
        string result = NamingConventions.RemoveSuffix("BankAccount", null!);
        Assert.Equal("BankAccount", result);
    }

    /// <summary>
    ///     RemoveSuffix should return null for null type name.
    /// </summary>
    [Fact]
    public void RemoveSuffixReturnsNullForNullTypeName()
    {
        string result = NamingConventions.RemoveSuffix(null!, "Suffix");
        Assert.Null(result);
    }

    /// <summary>
    ///     RemoveSuffix should return original when suffix not present.
    /// </summary>
    [Fact]
    public void RemoveSuffixReturnsOriginalWhenSuffixNotPresent()
    {
        string result = NamingConventions.RemoveSuffix("BankAccount", "Projection");
        Assert.Equal("BankAccount", result);
    }

    /// <summary>
    ///     Target-aware methods should fall back to legacy behavior when target is empty.
    /// </summary>
    [Fact]
    public void TargetAwareMethodsFallBackToLegacyWhenTargetEmpty()
    {
        // When target is empty, fall back to legacy behavior to avoid invalid namespaces
        string result = NamingConventions.GetClientFeatureRootNamespace(
            "Spring.Domain.Aggregates.BankAccount.Commands",
            string.Empty);

        // Falls back to legacy behavior: Spring.Domain → Spring.Client
        Assert.Equal("Spring.Client.Features.BankAccountAggregate", result);
    }

    /// <summary>
    ///     Target-aware methods should return empty when source is empty.
    /// </summary>
    [Fact]
    public void TargetAwareMethodsReturnEmptyWhenSourceEmpty()
    {
        string result = NamingConventions.GetClientActionsNamespace(string.Empty, "MyApp.BlazorClient");
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     Target-aware methods should work with standard Domain pattern.
    /// </summary>
    [Fact]
    public void TargetAwareMethodsWorkWithStandardDomainPattern()
    {
        string result = NamingConventions.GetClientActionsNamespace(
            "Spring.Domain.Aggregates.BankAccount.Commands",
            "Spring.Client");
        Assert.Equal("Spring.Client.Features.BankAccountAggregate.Actions", result);
    }

    /// <summary>
    ///     ToCamelCase should convert PascalCase to camelCase.
    /// </summary>
    [Fact]
    public void ToCamelCaseConvertsPascalCaseToCamelCase()
    {
        string result = NamingConventions.ToCamelCase("BankAccount");
        Assert.Equal("bankAccount", result);
    }

    /// <summary>
    ///     ToCamelCase should handle already camelCase.
    /// </summary>
    [Fact]
    public void ToCamelCaseHandlesAlreadyCamelCase()
    {
        string result = NamingConventions.ToCamelCase("bankAccount");
        Assert.Equal("bankAccount", result);
    }

    /// <summary>
    ///     ToCamelCase should handle single lowercase character.
    /// </summary>
    [Fact]
    public void ToCamelCaseHandlesSingleLowercaseCharacter()
    {
        string result = NamingConventions.ToCamelCase("b");
        Assert.Equal("b", result);
    }

    /// <summary>
    ///     ToCamelCase should handle single non-letter character.
    /// </summary>
    [Fact]
    public void ToCamelCaseHandlesSingleNonLetterCharacter()
    {
        string result = NamingConventions.ToCamelCase("1");
        Assert.Equal("1", result);
    }

    /// <summary>
    ///     ToCamelCase should handle single uppercase character.
    /// </summary>
    [Fact]
    public void ToCamelCaseHandlesSingleUppercaseCharacter()
    {
        string result = NamingConventions.ToCamelCase("B");
        Assert.Equal("b", result);
    }

    /// <summary>
    ///     ToCamelCase should return empty for empty input.
    /// </summary>
    [Fact]
    public void ToCamelCaseReturnsEmptyForEmptyInput()
    {
        string result = NamingConventions.ToCamelCase(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     ToCamelCase should return null for null input.
    /// </summary>
    [Fact]
    public void ToCamelCaseReturnsNullForNullInput()
    {
        string result = NamingConventions.ToCamelCase(null!);
        Assert.Null(result);
    }

    /// <summary>
    ///     ToKebabCase should convert PascalCase to kebab-case.
    /// </summary>
    [Fact]
    public void ToKebabCaseConvertsPascalCaseToKebabCase()
    {
        string result = NamingConventions.ToKebabCase("BankAccountBalance");
        Assert.Equal("bank-account-balance", result);
    }

    /// <summary>
    ///     ToKebabCase should handle already lowercase.
    /// </summary>
    [Fact]
    public void ToKebabCaseHandlesAlreadyLowercase()
    {
        string result = NamingConventions.ToKebabCase("bankaccount");
        Assert.Equal("bankaccount", result);
    }

    /// <summary>
    ///     ToKebabCase should handle camelCase.
    /// </summary>
    [Fact]
    public void ToKebabCaseHandlesCamelCase()
    {
        string result = NamingConventions.ToKebabCase("bankAccountBalance");
        Assert.Equal("bank-account-balance", result);
    }

    /// <summary>
    ///     ToKebabCase should handle single uppercase character.
    /// </summary>
    [Fact]
    public void ToKebabCaseHandlesSingleUppercaseCharacter()
    {
        string result = NamingConventions.ToKebabCase("B");
        Assert.Equal("b", result);
    }

    /// <summary>
    ///     ToKebabCase should return empty for empty input.
    /// </summary>
    [Fact]
    public void ToKebabCaseReturnsEmptyForEmptyInput()
    {
        string result = NamingConventions.ToKebabCase(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     ToKebabCase should return null for null input.
    /// </summary>
    [Fact]
    public void ToKebabCaseReturnsNullForNullInput()
    {
        string result = NamingConventions.ToKebabCase(null!);
        Assert.Null(result);
    }
}