using System;

using Mississippi.Inlet.Client.ActionEffects;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.BasePaths;

/// <summary>
///     Verifies the hub option's navigation URI contract.
/// </summary>
public sealed class InletHubBasePathTests
{
    /// <summary>
    ///     The default hub route resolves beneath the application navigation base.
    /// </summary>
    /// <param name="baseAddress">The application's navigation base.</param>
    /// <param name="expectedAddress">The expected hub URI.</param>
    [Theory]
    [InlineData("https://example.test/", "https://example.test/hubs/inlet")]
    [InlineData("https://example.test/bank/", "https://example.test/bank/hubs/inlet")]
    public void DefaultHubRoutePreservesNavigationBasePath(
        string baseAddress,
        string expectedAddress
    )
    {
        BasePathNavigationManager navigation = new(baseAddress);
        InletSignalRActionEffectOptions options = new();
        Uri hubUri = navigation.ToAbsoluteUri(options.HubPath);
        Assert.Equal(new(expectedAddress), hubUri);
    }

    /// <summary>
    ///     Explicit hub paths retain relative, root-relative, and absolute resolution.
    /// </summary>
    /// <param name="hubPath">The configured hub path.</param>
    /// <param name="expectedAddress">The expected hub URI.</param>
    [Theory]
    [InlineData("hubs/custom", "https://example.test/bank/hubs/custom")]
    [InlineData("/hubs/inlet", "https://example.test/hubs/inlet")]
    [InlineData("https://gateway.test/hubs/inlet", "https://gateway.test/hubs/inlet")]
    public void ExplicitHubRoutesRetainUriSemantics(
        string hubPath,
        string expectedAddress
    )
    {
        BasePathNavigationManager navigation = new("https://example.test/bank/");
        InletSignalRActionEffectOptions options = new()
        {
            HubPath = hubPath,
        };
        Uri hubUri = navigation.ToAbsoluteUri(options.HubPath);
        Assert.Equal(new(expectedAddress), hubUri);
    }
}