using Mississippi.Core.Abstractions.Brooks;


namespace Mississippi.Core.Abstractions.Tests.Streams;

public class BrookPositionTests
{
    [Fact]
    public void Constructor_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrookPosition(-1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void Constructor_WithNonNegativeValue_SetsValue(
        long input
    )
    {
        BrookPosition position = new(input);
        Assert.Equal(input, position.Value);
    }

    [Fact]
    public void IsNewerThan_ReturnsTrue_WhenThisIsGreaterThanOther()
    {
        BrookPosition earlier = new(5);
        BrookPosition later = new(10);
        Assert.True(later.IsNewerThan(earlier));
    }

    [Fact]
    public void IsNewerThan_ReturnsFalse_WhenThisIsNotGreaterThanOther()
    {
        BrookPosition a = new(5);
        BrookPosition b = new(5);
        Assert.False(a.IsNewerThan(b));
    }
}