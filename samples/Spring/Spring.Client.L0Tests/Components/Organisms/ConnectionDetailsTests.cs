using System;

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

    /// <summary>
    ///     Closing the disclosure removes its retained diagnostics and close control.
    /// </summary>
    [Fact]
    public void ClosedDetailsHideRetainedDiagnostics()
    {
        using IRenderedComponent<ConnectionDetails> cut = Render<ConnectionDetails>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.ConnectionIdText, "retained-connection")
            .Add(component => component.LastError, "A retained failure."));
        Assert.Contains("retained-connection", cut.Markup, StringComparison.Ordinal);
        cut.Render(parameters => parameters.Add(component => component.IsOpen, false));
        Assert.Empty(cut.FindAll("section, dl, p, button"));
        Assert.DoesNotContain("retained-connection", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("A retained failure.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Expanded details retain each supplied diagnostic and encode the raw error.
    /// </summary>
    [Fact]
    public void OpenDetailsRenderActualDiagnosticsAndEscapedError()
    {
        const string error = "The transport failed <unsafe>.";
        using IRenderedComponent<ConnectionDetails> cut = Render<ConnectionDetails>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.ConnectionStatusText, "Disconnected")
            .Add(component => component.ConnectionIdText, "connection/<unsafe>")
            .Add(component => component.ReconnectAttemptCount, 4)
            .Add(component => component.LastConnectedAtText, "10:15:00")
            .Add(component => component.LastDisconnectedAtText, "10:16:00")
            .Add(component => component.LastMessageReceivedAtText, "10:15:30")
            .Add(component => component.LastError, error));
        Assert.Equal("Live connection details", cut.Find("section").GetAttribute("aria-label"));
        Assert.Collection(
            cut.FindAll("dl dd"),
            value => Assert.Equal("Disconnected", value.TextContent),
            value => Assert.Equal("connection/<unsafe>", value.TextContent.Trim()),
            value => Assert.Equal("4", value.TextContent),
            value => Assert.Equal("10:15:00", value.TextContent),
            value => Assert.Equal("10:16:00", value.TextContent),
            value => Assert.Equal("10:15:30", value.TextContent));
        Assert.Contains(error, cut.Find("section > p").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("unsafe, dialog"));
    }

    /// <summary>
    ///     Missing diagnostic values use existing placeholders and do not display an invented error.
    /// </summary>
    [Fact]
    public void OpenDetailsWithoutErrorRetainDefaultPlaceholders()
    {
        using IRenderedComponent<ConnectionDetails> cut = Render<ConnectionDetails>(parameters => parameters
            .Add(component => component.IsOpen, true)
            .Add(component => component.ConnectionStatusText, "Connected"));
        Assert.Empty(cut.FindAll("section > p"));
        Assert.Collection(
            cut.FindAll("dl dd"),
            value => Assert.Equal("Connected", value.TextContent),
            value => Assert.Equal("—", value.TextContent.Trim()),
            value => Assert.Equal("0", value.TextContent),
            value => Assert.Equal("—", value.TextContent),
            value => Assert.Equal("—", value.TextContent),
            value => Assert.Equal("—", value.TextContent));
    }
}