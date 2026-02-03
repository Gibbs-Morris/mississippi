using Bunit;

using Microsoft.AspNetCore.Components;

using Spring.Client.Components.Organisms;


namespace Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="ConnectionLostModal" />.
/// </summary>
public sealed class ConnectionLostModalTests : BunitContext
{
    /// <summary>
    ///     Reconnect button invokes callback.
    /// </summary>
    [Fact]
    public void ReconnectButtonInvokesCallback()
    {
        bool reconnected = false;
        using IRenderedComponent<ConnectionLostModal> cut = Render<ConnectionLostModal>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.OnReconnect, EventCallback.Factory.Create(this, () => reconnected = true)));
        cut.Find("button").Click();
        Assert.True(reconnected);
    }
}