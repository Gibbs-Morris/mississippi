using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="ConnectionNotice" />.
/// </summary>
public sealed class ConnectionNoticeTests : BunitContext
{
    /// <summary>Startup and reconnection are reported without a false connection-lost dialog.</summary>
    /// <param name="status">The live connection state.</param>
    [Theory]
    [InlineData("Connecting")]
    [InlineData("Reconnecting")]
    public void ConnectingIsAnInlineStatusWithoutAFalseLostDialog(
        string status
    )
    {
        using IRenderedComponent<ConnectionNotice> cut = Render<ConnectionNotice>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.ConnectionStatusText, status));
        Assert.Empty(cut.FindAll("dialog"));
        Assert.Equal("Connecting to live updates", cut.Find("[role='status'] strong").TextContent);
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    /// <summary>
    ///     Reconnect button invokes callback.
    /// </summary>
    [Fact]
    public void ReconnectButtonInvokesCallback()
    {
        bool reconnected = false;
        using IRenderedComponent<ConnectionNotice> cut = Render<ConnectionNotice>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.OnReconnect, EventCallback.Factory.Create(this, () => reconnected = true)));
        cut.Find("button").Click();
        Assert.True(reconnected);
    }
}