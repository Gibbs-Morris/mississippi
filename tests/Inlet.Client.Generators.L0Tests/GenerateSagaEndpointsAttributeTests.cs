using Mississippi.Inlet.Generators.Abstractions;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="GenerateSagaEndpointsAttribute" />.
/// </summary>
public sealed class GenerateSagaEndpointsAttributeTests
{
    /// <summary>
    ///     Verifies attribute properties can be assigned.
    /// </summary>
    [Fact]
    public void AttributeStoresConfiguredValues()
    {
        GenerateSagaEndpointsAttribute attribute = new()
        {
            FeatureKey = "feature",
            InputType = typeof(string),
            RoutePrefix = "route",
        };
        Assert.Equal("feature", attribute.FeatureKey);
        Assert.Equal(typeof(string), attribute.InputType);
        Assert.Equal("route", attribute.RoutePrefix);
    }
}