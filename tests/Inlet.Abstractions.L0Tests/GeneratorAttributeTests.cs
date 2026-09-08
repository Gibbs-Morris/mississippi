using Mississippi.Inlet.Generators.Abstractions;


namespace Mississippi.Inlet.Abstractions.L0Tests;

/// <summary>
///     Tests the defaults and constructor values consumed by source generators.
/// </summary>
public sealed class GeneratorAttributeTests
{
    /// <summary>
    ///     Command endpoints default to HTTP POST.
    /// </summary>
    [Fact]
    public void CommandDefaultsToPost() => Assert.Equal("POST", new GenerateCommandAttribute().HttpMethod);

    /// <summary>
    ///     Command metadata defaults describe a potentially destructive operation.
    /// </summary>
    [Fact]
    public void CommandToolDefaultsDescribeMutation()
    {
        GenerateMcpToolMetadataAttribute attribute = new();
        Assert.True(attribute.Destructive);
        Assert.False(attribute.Idempotent);
        Assert.False(attribute.OpenWorld);
        Assert.False(attribute.ReadOnly);
    }

    /// <summary>
    ///     Generator descriptions and property names retain their constructor values.
    /// </summary>
    [Fact]
    public void ConstructorMetadataRetainsValues()
    {
        Assert.Equal(
            "Account identifier",
            new GenerateMcpParameterDescriptionAttribute("Account identifier").Description);
        Assert.Equal("accountId", new GeneratorPropertyNameAttribute("accountId").Name);
        Assert.Equal("Awaiting generator", new PendingSourceGeneratorAttribute("Awaiting generator").Reason);
        Assert.Null(new PendingSourceGeneratorAttribute().Reason);
    }

    /// <summary>
    ///     The generic saga marker exposes its actual input type.
    /// </summary>
    [Fact]
    public void GenericSagaExposesInputType() =>
        Assert.Equal(typeof(string), new GenerateSagaEndpointsAttribute<string>().InputType);

    /// <summary>
    ///     Projection endpoints generate subscriptions by default.
    /// </summary>
    [Fact]
    public void ProjectionEnablesClientSubscription() =>
        Assert.True(new GenerateProjectionEndpointsAttribute().GenerateClientSubscription);

    /// <summary>
    ///     Read tools default to safe, idempotent read operations.
    /// </summary>
    [Fact]
    public void ReadToolDefaultsDescribeReadOperation()
    {
        GenerateMcpReadToolAttribute attribute = new();
        Assert.False(attribute.Destructive);
        Assert.True(attribute.Idempotent);
        Assert.False(attribute.OpenWorld);
        Assert.True(attribute.ReadOnly);
    }

    /// <summary>
    ///     Required markers default to required.
    /// </summary>
    [Fact]
    public void RequiredMarkerDefaultsToRequired() => Assert.True(new GeneratorRequiredAttribute().IsRequired);

    /// <summary>
    ///     Explicit required markers retain both supported values.
    /// </summary>
    /// <param name="isRequired">Whether the generated property is required.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RequiredMarkerRetainsExplicitValue(
        bool isRequired
    ) =>
        Assert.Equal(isRequired, new GeneratorRequiredAttribute(isRequired).IsRequired);
}