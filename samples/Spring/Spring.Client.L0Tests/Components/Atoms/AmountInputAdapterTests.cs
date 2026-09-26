using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    /// <summary>External valid values restore parent-owned amount validity.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExternalValidValueRestoresParentValidity()
    {
        const string inputId = "account-b-amount-input";
        bool? isValid = null;
        using IRenderedComponent<SpringAmountInput> cut = Render<SpringAmountInput>(p => p
            .Add(c => c.InputId, inputId)
            .Add(c => c.Label, "Account B amount")
            .Add(c => c.Value, 0m)
            .Add(c => c.IsValidChanged, EventCallback.Factory.Create<bool>(this, value => isValid = value)));
        await cut.Find($"#{inputId}").InputAsync("12.");
        Assert.False(isValid);
        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(
            ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(SpringAmountInput.Value)] = 42.5m,
                })));
        Assert.True(isValid);
    }

    /// <summary>External value changes reset invalid draft text and its error state.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExternalValueChangeResetsInvalidDraftAndError()
    {
        const string inputId = "account-a-amount-input";
        using IRenderedComponent<SpringAmountInput> cut = Render<SpringAmountInput>(p => p
            .Add(c => c.InputId, inputId)
            .Add(c => c.Label, "Account A amount")
            .Add(c => c.Value, 0m));
        await cut.Find($"#{inputId}").InputAsync("12.");
        IElement invalidInput = cut.Find($"#{inputId}");
        Assert.Equal("12.", invalidInput.GetAttribute("value"));
        Assert.Equal("true", invalidInput.GetAttribute("aria-invalid"));
        Assert.Contains($"{inputId}-error", invalidInput.GetAttribute("aria-describedby"), StringComparison.Ordinal);
        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(
            ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(SpringAmountInput.Value)] = 42.5m,
                })));
        IElement updatedInput = cut.Find($"#{inputId}");
        Assert.Equal("42.5", updatedInput.GetAttribute("value"));
        Assert.Null(updatedInput.GetAttribute("aria-invalid"));
        Assert.DoesNotContain(
            $"{inputId}-error",
            updatedInput.GetAttribute("aria-describedby") ?? string.Empty,
            StringComparison.Ordinal);
        Assert.Empty(cut.FindAll($"#{inputId}-error"));
    }

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
                     "10000000000000000000000000000.1",
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