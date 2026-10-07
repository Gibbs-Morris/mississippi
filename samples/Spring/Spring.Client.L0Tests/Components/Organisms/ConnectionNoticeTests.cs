using System;

using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Organisms;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="ConnectionNotice" />.
/// </summary>
public sealed class ConnectionNoticeTests : BunitContext
{
    /// <summary>Verify that closed notices expose neither their diagnostics nor the action.</summary>
    [Fact]
    public void ClosedNoticeHidesDiagnosticsAndReconnectAction()
    {
        using IRenderedComponent<ConnectionNotice> cut = Render<ConnectionNotice>(parameters => parameters
            .Add(component => component.IsOpen, false)
            .Add(component => component.ConnectionStatusText, "Disconnected")
            .Add(component => component.ReconnectAttemptCount, 3)
            .Add(component => component.LastError, "A retained failure."));
        Assert.Empty(cut.FindAll("section, [role='status'], [role='alert'], button"));
        Assert.DoesNotContain("A retained failure.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Verify that startup and reconnection are reported without a false connection-lost dialog.</summary>
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

    /// <summary>Verify that a disconnected notice exposes actual attempts and escaped failure text with an enabled reconnect action.</summary>
    [Fact]
    public void DisconnectedNoticeShowsAttemptsAndErrorAndAllowsReconnect()
    {
        const string error = "The hub request failed <unsafe>.";
        int reconnects = 0;
        using IRenderedComponent<ConnectionNotice> cut = Render<ConnectionNotice>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.ConnectionStatusText, "Disconnected")
            .Add(component => component.ReconnectAttemptCount, 3)
            .Add(component => component.LastError, error)
            .Add(component => component.OnReconnect, EventCallback.Factory.Create(this, () => reconnects++)));
        Assert.Equal("Live updates are disconnected", cut.Find("[role='status'] strong").TextContent);
        Assert.Contains("Status: Disconnected", cut.Find("[role='status']").TextContent, StringComparison.Ordinal);
        Assert.Contains("Reconnect attempt 3", cut.Find("[role='status']").TextContent, StringComparison.Ordinal);
        Assert.Equal(error, cut.Find("[role='alert']").TextContent);
        Assert.Empty(cut.FindAll("unsafe, dialog"));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
        cut.Find("button").Click();
        Assert.Equal(1, reconnects);
    }

    /// <summary>Verify that zero attempts and no error do not manufacture diagnostics.</summary>
    [Fact]
    public void DisconnectedNoticeWithoutErrorOrAttemptsShowsOnlyCurrentStatus()
    {
        using IRenderedComponent<ConnectionNotice> cut = Render<ConnectionNotice>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.ConnectionStatusText, "Disconnected"));
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.DoesNotContain("Reconnect attempt", cut.Markup, StringComparison.Ordinal);
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    /// <summary>Verify that reconnect button invokes callback.</summary>
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