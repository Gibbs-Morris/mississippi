namespace Mississippi.Ripples.Blazor.L0Tests;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Allure.Xunit.Attributes;
using Bunit;
using Xunit;

/// <summary>
/// Unit tests for <see cref="RippleComponent"/>.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Blazor")]
[AllureSubSuite("RippleComponent")]
[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created", Justification = "bUnit TestContext manages component disposal.")]
public sealed class RippleComponentTests : TestContext
{
    /// <summary>
    /// UseRipple should subscribe to Changed event and call StateHasChanged.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Lifecycle Management")]
    public async Task UseRippleSubscribesToChangedEvent()
    {
        // Arrange
        await using var ripple = new TestRipple();

        // Act
        var cut = RenderComponent<TestComponent>(parameters =>
            parameters.Add(p => p.Ripple, ripple));

        // Trigger a change on the ripple
        ripple.TriggerChanged();

        // Assert - component should have re-rendered
        Assert.True(cut.Instance.RenderCount > 1);
    }

    /// <summary>
    /// DisposeAsync should unsubscribe from Changed event.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Lifecycle Management")]
    public async Task DisposeAsyncUnsubscribesFromChangedEvent()
    {
        // Arrange
        await using var ripple = new TestRipple();
        var cut = RenderComponent<TestComponent>(parameters =>
            parameters.Add(p => p.Ripple, ripple));

        // Act
        await cut.Instance.DisposeAsync();

        // Assert - should have unsubscribed
        Assert.False(ripple.HasSubscribers);
    }

    /// <summary>
    /// Multiple UseRipple calls should track all subscriptions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Lifecycle Management")]
    public async Task MultipleUseRippleCallsAreTracked()
    {
        // Arrange
        await using var ripple1 = new TestRipple();
        await using var ripple2 = new TestRipple();
        var cut = RenderComponent<MultiRippleComponent>(parameters =>
            parameters
                .Add(p => p.Ripple1, ripple1)
                .Add(p => p.Ripple2, ripple2));

        // Act
        await cut.Instance.DisposeAsync();

        // Assert - both should be unsubscribed
        Assert.False(ripple1.HasSubscribers);
        Assert.False(ripple2.HasSubscribers);
    }

    /// <summary>
    /// UseRipple with null ripple should not throw.
    /// </summary>
    [Fact]
    [AllureFeature("Error Handling")]
    public void UseRippleWithNullRippleDoesNotThrow()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<TestComponent>(parameters =>
            parameters.Add(p => p.Ripple, null));

        // Assert
        Assert.NotNull(cut.Instance);
    }

    /// <summary>
    /// Component should render projection data when available.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    [AllureFeature("Rendering")]
    public async Task ComponentRendersProjectionData()
    {
        // Arrange
        await using var ripple = new TestRipple();
        ripple.SetData(new TestProjection("Test Data", 42));

        // Act
        var cut = RenderComponent<TestComponent>(parameters =>
            parameters.Add(p => p.Ripple, ripple));

        // Assert
        cut.MarkupMatches("<div>Test Data: 42</div>");
    }
}
