using Mississippi.Brooks.Abstractions.Streaming;


namespace Mississippi.Brooks.Abstractions.L0Tests.Streaming;

/// <summary>
///     Tests for <see cref="BrookProviderOptions" />.
/// </summary>
public sealed class BrookProviderOptionsTests
{
    /// <summary>
    ///     OrleansStreamProviderName should be settable.
    /// </summary>
    [Fact]
    public void OrleansStreamProviderNameCanBeSet()
    {
        // Arrange
        BrookProviderOptions sut = new();
        const string customName = "CustomStreamProvider";

        // Act
        sut.OrleansStreamProviderName = customName;

        // Assert
        Assert.Equal(customName, sut.OrleansStreamProviderName);
    }

    /// <summary>
    ///     OrleansStreamProviderName should default to BrookStreamingDefaults.OrleansStreamProviderName.
    /// </summary>
    [Fact]
    public void OrleansStreamProviderNameDefaultsToBrookStreamingDefaults()
    {
        // Arrange & Act
        BrookProviderOptions sut = new();

        // Assert
        Assert.Equal(BrookStreamingDefaults.OrleansStreamProviderName, sut.OrleansStreamProviderName);
    }
}