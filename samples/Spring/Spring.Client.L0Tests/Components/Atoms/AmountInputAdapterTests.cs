using System;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;

using MississippiSamples.Spring.Client.Components.Atoms.AmountInputAdapter;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="SpringAmountInput" />.
/// </summary>
public sealed class AmountInputAdapterTests : BunitContext
{
    /// <summary>
    ///     Blank, incomplete, malformed, and unrepresentable values report invalid state without dispatching.
    /// </summary>
    [Fact]
    public void InvalidDecimalDraftsDoNotInvokeValueCallback()
    {
        decimal? result = null;
        bool? isValid = null;
        using IRenderedComponent<SpringAmountInput> cut = Render<SpringAmountInput>(p => p
            .Add(c => c.InputId, "account-b-amount-input")
            .Add(c => c.Label, "Account B amount")
            .Add(c => c.Value, 0m)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<decimal>(this, value => result = value))
            .Add(c => c.IsValidChanged, EventCallback.Factory.Create<bool>(this, value => isValid = value)));
        foreach (string draft in new[]
                 {
                     string.Empty,
                     "12.",
                     "1.2.3",
                     "not-an-amount",
                     "1,234.50",
                     " 12.34",
                     "79228162514264337593543950336",
                     "0.12345678901234567890123456789",
                 })
        {
            cut.Find("#account-b-amount-input").Input(draft);
            Assert.Null(result);
            Assert.False(isValid);
            IElement input = cut.Find("#account-b-amount-input");
            Assert.Equal("true", input.GetAttribute("aria-invalid"));
            string describedBy = input.GetAttribute("aria-describedby") ?? string.Empty;
            Assert.Contains("account-b-amount-input-error", describedBy, StringComparison.Ordinal);
            Assert.Single(cut.FindAll("#account-b-amount-input-error"));
        }
    }

    /// <summary>
    ///     Valid decimal input is passed to the callback without changing its value.
    /// </summary>
    [Fact]
    public void ValidDecimalInputInvokesValueCallbackUnchanged()
    {
        decimal? result = null;
        bool? isValid = null;
        using IRenderedComponent<SpringAmountInput> cut = Render<SpringAmountInput>(p => p
            .Add(c => c.InputId, "account-a-amount-input")
            .Add(c => c.Label, "Account A amount")
            .Add(c => c.Value, 0m)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<decimal>(this, value => result = value))
            .Add(c => c.IsValidChanged, EventCallback.Factory.Create<bool>(this, value => isValid = value)));
        cut.Find("#account-a-amount-input").Input("12.345");
        Assert.Equal(12.345m, result);
        Assert.True(isValid);
        Assert.Equal("12.345", cut.Find("#account-a-amount-input").GetAttribute("value"));
        Assert.Null(cut.Find("#account-a-amount-input").GetAttribute("aria-invalid"));
    }
}