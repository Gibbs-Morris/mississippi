using Bunit;

using Microsoft.AspNetCore.Components;

using Spring.Client.Components.Organisms;


namespace Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Tests for <see cref="ConnectionStatusModal" />.
/// </summary>
public sealed class ConnectionStatusModalTests : BunitContext
{
    /// <summary>
    ///     Close button invokes callback.
    /// </summary>
    [Fact]
    public void CloseButtonInvokesCallback()
    {
        bool closed = false;
        using IRenderedComponent<ConnectionStatusModal> cut = Render<ConnectionStatusModal>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.ConnectionStatusText, "Connected")
            .Add(c => c.OnClose, EventCallback.Factory.Create(this, () => closed = true)));
        cut.Find("button").Click();
        Assert.True(closed);
    }
}