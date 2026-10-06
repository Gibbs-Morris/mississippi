using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="ConnectionDetails" />.
/// </summary>
public sealed class ConnectionDetailsTests : BunitContext
{
    /// <summary>
    ///     Close button invokes callback.
    /// </summary>
    [Fact]
    public void CloseButtonInvokesCallback()
    {
        bool closed = false;
        using IRenderedComponent<ConnectionDetails> cut = Render<ConnectionDetails>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.ConnectionStatusText, "Connected")
            .Add(c => c.OnClose, EventCallback.Factory.Create(this, () => closed = true)));
        cut.Find("button").Click();
        Assert.True(closed);
    }
}