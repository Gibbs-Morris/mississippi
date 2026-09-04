using System;
using System.Reflection;

using Mississippi.DomainModeling.TestHarness.Architecture;

using Moq;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Architecture;

/// <summary>
///     Verifies alias diagnostics classify persisted contracts and generated artifacts correctly.
/// </summary>
public sealed class AliasValidationClassificationTests
{
    /// <summary>
    ///     Compiler attributes exclude generated artifacts even when their names appear ordinary.
    /// </summary>
    [Fact]
    public void CompilerGeneratedAttributesShouldExcludeOrdinaryNames()
    {
        Type fixture = AliasFixtureFactory.CreateType("Fixtures.GeneratedRecord", isGenerated: true);
        Assert.True(AliasValidation.IsGeneratedType(fixture));
        Assert.Empty(AliasValidation.AnalyzeAssemblies([fixture.Assembly], []).Mismatches);
    }

    /// <summary>
    ///     Generated naming patterns must match their documented positions within a type identity.
    /// </summary>
    /// <param name="typeFullName">The complete CLR identity.</param>
    /// <param name="isGenerated">Whether the identity denotes a generated artifact.</param>
    [Theory]
    [InlineData("Fixtures.Codec_Record", true)]
    [InlineData("Fixtures.Copier_Record", true)]
    [InlineData("Fixtures.Activator_Record", true)]
    [InlineData("Fixtures.Proxy_Record", true)]
    [InlineData("Fixtures.Invokable_Record", true)]
    [InlineData("Fixtures.SomeAnonymousTypeRecord", true)]
    [InlineData("OrleansCodeGen.Generated.Record", true)]
    [InlineData("Fixtures.RecordCodec_", false)]
    [InlineData("Fixtures.RecordCopier_", false)]
    [InlineData("Fixtures.RecordActivator_", false)]
    [InlineData("Fixtures.RecordProxy_", false)]
    [InlineData("Fixtures.RecordInvokable_", false)]
    [InlineData("Fixtures.OrleansCodeGen.Record", false)]
    public void GeneratedNamesShouldBeRecognizedPrecisely(
        string typeFullName,
        bool isGenerated
    )
    {
        Type fixture = AliasFixtureFactory.CreateType(typeFullName);
        Assert.Equal(isGenerated, AliasValidation.IsGeneratedType(fixture));
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies([fixture.Assembly], []);
        Assert.Equal(isGenerated ? 0 : 1, summary.Mismatches.Length);
    }

    /// <summary>
    ///     Generic parameters without a full name still have a usable expected identity.
    /// </summary>
    [Fact]
    public void GenericParametersShouldUseTheirSimpleNames()
    {
        Type parameter = typeof(GenericAliasFixture<>).GetGenericArguments()[0];
        Assert.Equal("T", AliasValidation.GetExpectedAlias(parameter));
        Assert.False(AliasValidation.IsGeneratedType(parameter));
    }

    /// <summary>
    ///     A current type identity must not be reported as an alias mismatch.
    /// </summary>
    [Fact]
    public void MatchingAliasesShouldNotProduceMismatches()
    {
        const string typeFullName = "Fixtures.CurrentContract";
        Type fixture = AliasFixtureFactory.CreateType(typeFullName, alias: typeFullName);
        Assert.Empty(AliasValidation.AnalyzeAssemblies([fixture.Assembly], []).Mismatches);
    }

    /// <summary>
    ///     Namespace conventions and suffix conventions independently identify contract categories.
    /// </summary>
    /// <param name="typeFullName">The complete CLR identity.</param>
    /// <param name="isInterface">Whether the identity represents an interface.</param>
    /// <param name="expectedCategory">The category presented to the test author.</param>
    [Theory]
    [InlineData("Fixtures.Commands.Request", false, AliasTypeCategory.Command)]
    [InlineData("Fixtures.SubmitCommand", false, AliasTypeCategory.Command)]
    [InlineData("Fixtures.Commands.CreatedEvent", false, AliasTypeCategory.Command)]
    [InlineData("Fixtures.Events.Payload", false, AliasTypeCategory.Event)]
    [InlineData("Fixtures.CreatedEvent", false, AliasTypeCategory.Event)]
    [InlineData("Fixtures.Projections.ReadModel", false, AliasTypeCategory.Projection)]
    [InlineData("Fixtures.AccountProjection", false, AliasTypeCategory.Projection)]
    [InlineData("Fixtures.Aggregates.State", false, AliasTypeCategory.Aggregate)]
    [InlineData("Fixtures.AccountAggregate", false, AliasTypeCategory.Aggregate)]
    [InlineData("Fixtures.TransferSagaState", false, AliasTypeCategory.Aggregate)]
    [InlineData("Fixtures.IAccountGrain", true, AliasTypeCategory.GrainInterface)]
    [InlineData("Fixtures.AccountGrain", false, AliasTypeCategory.GrainImplementation)]
    [InlineData("Fixtures.CommandRequest", false, AliasTypeCategory.Contract)]
    [InlineData("Fixtures.EventPayload", false, AliasTypeCategory.Contract)]
    [InlineData("Fixtures.ProjectionState", false, AliasTypeCategory.Contract)]
    [InlineData("Fixtures.AggregateState", false, AliasTypeCategory.Contract)]
    [InlineData("Fixtures.SagaStateFactory", false, AliasTypeCategory.Contract)]
    [InlineData("Fixtures.GrainFactory", false, AliasTypeCategory.Contract)]
    [InlineData("Fixtures.IContract", true, AliasTypeCategory.Contract)]
    [InlineData("GlobalContract", false, AliasTypeCategory.Contract)]
    public void MismatchesShouldIdentifyContractCategories(
        string typeFullName,
        bool isInterface,
        AliasTypeCategory expectedCategory
    )
    {
        Type fixture = AliasFixtureFactory.CreateType(typeFullName, isInterface);
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies([fixture.Assembly], []);
        AliasValidationResult mismatch = Assert.Single(summary.Mismatches);
        Assert.Equal("AliasValidationFixtures", mismatch.AssemblyName);
        Assert.Equal(typeFullName, mismatch.TypeFullName);
        Assert.Equal($"Legacy.{typeFullName}", mismatch.ActualAlias);
        Assert.Equal(typeFullName, mismatch.ExpectedAlias);
        Assert.Equal(expectedCategory, mismatch.TypeCategory);
        Assert.Equal(AliasMismatchCategory.AliasDoesNotMatchCurrentTypeName, mismatch.MismatchCategory);
        Assert.Empty(summary.ConfigurationErrors);
        Assert.Empty(summary.ActiveExceptions);
    }

    /// <summary>
    ///     One failed type load must not discard other loadable types in the same assembly.
    /// </summary>
    [Fact]
    public void PartialTypeLoadFailuresShouldRetainLoadableContracts()
    {
        Mock<Assembly> assembly = new(MockBehavior.Strict);
        assembly.SetupGet(value => value.FullName).Returns("PartiallyLoadableFixtures");
        assembly.Setup(value => value.GetName()).Returns(new AssemblyName("PartiallyLoadableFixtures"));
        assembly.Setup(value => value.GetTypes())
            .Throws(
                new ReflectionTypeLoadException([typeof(NamespaceMismatchFixture), null!], [new TypeLoadException()]));
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies([assembly.Object], []);
        AliasValidationResult mismatch = Assert.Single(summary.Mismatches);
        Assert.Equal(typeof(NamespaceMismatchFixture).FullName, mismatch.TypeFullName);
    }
}