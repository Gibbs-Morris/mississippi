using System;
using System.Collections.Immutable;

using Bunit;

using Mississippi.Inlet.Client.Abstractions.Commands;

using MississippiSamples.Spring.Client.Components.Organisms;
using MississippiSamples.Spring.Client.Features.BankAccountAggregate.State;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Organisms;

/// <summary>
///     Protects the distinction between command responses and live projection outcomes.
/// </summary>
public sealed class CommandActivityTests : BunitContext
{
    /// <summary>Acceptance never claims that a projection has refreshed.</summary>
    [Fact]
    public void AcceptedResponseExplainsSeparateProjectionOutcome()
    {
        using IRenderedComponent<CommandActivity> cut = Render<CommandActivity>(parameters => parameters.Add(
                component => component.State,
                new BankAccountAggregateState
                {
                    LastCommandSucceeded = true,
                })
            .Add(
                component => component.Explanation,
                "Check both live balances before deciding the transfer completed."));
        Assert.Contains(
            "Latest response: accepted.",
            cut.Find("[role='status']").TextContent,
            StringComparison.Ordinal);
        Assert.Contains("Check both live balances", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Command executed successfully", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Transport failures cannot establish that the server rejected or did not commit a command.</summary>
    /// <param name="code">The real generated failure category.</param>
    /// <param name="message">The real generated failure detail.</param>
    [Theory]
    [InlineData("HttpError", "Network error: response lost.")]
    [InlineData("HttpError", "Request cancelled: response unavailable.")]
    [InlineData("HttpError", "Server error (500): response failed.")]
    [InlineData("NoResponse", "No response from server.")]
    public void FailedClientStatusDoesNotClaimServerRejection(
        string code,
        string message
    )
    {
        DateTimeOffset timestamp = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        CommandHistoryEntry failed = CommandHistoryEntry
            .CreateExecuting("lost-response", "DepositFundsAction", timestamp)
            .ToFailed(timestamp, code, message);
        using IRenderedComponent<CommandActivity> cut = Render<CommandActivity>(parameters => parameters.Add(
            component => component.State,
            new BankAccountAggregateState
            {
                LastCommandSucceeded = false,
                ErrorCode = code,
                ErrorMessage = message,
                CommandHistory = ImmutableList.Create(failed),
            }));
        Assert.Contains("Latest request: failed.", cut.Find("[role='status']").TextContent, StringComparison.Ordinal);
        Assert.Contains("1 failed", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Failed", cut.Find("tbody tr").TextContent, StringComparison.Ordinal);
        Assert.Contains("does not establish the server outcome", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(code, cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.Contains(message, cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("rejected", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Concurrent commands keep the awaiting response state visible.</summary>
    [Fact]
    public void InFlightResponsesTakePrecedenceOverEarlierAcceptance()
    {
        using IRenderedComponent<CommandActivity> cut = Render<CommandActivity>(parameters => parameters.Add(
            component => component.State,
            new BankAccountAggregateState
            {
                InFlightCommands = ImmutableHashSet.Create("pending-one", "pending-two"),
                LastCommandSucceeded = true,
            }));
        Assert.Contains(
            "2 request(s) awaiting a response.",
            cut.Find("[role='status']").TextContent,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Latest response: accepted.", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>A later accepted response does not erase earlier rejections from the visible history.</summary>
    [Fact]
    public void MixedHistoryRetainsRejectionsAndTheirRequestIds()
    {
        DateTimeOffset timestamp = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        CommandHistoryEntry rejected = CommandHistoryEntry
            .CreateExecuting("withdraw-rejected", "WithdrawFundsAction", timestamp)
            .ToFailed(timestamp, "insufficient-funds", "The account has insufficient funds.");
        CommandHistoryEntry accepted = CommandHistoryEntry
            .CreateExecuting("deposit-accepted", "DepositFundsAction", timestamp)
            .ToSucceeded(timestamp);
        using IRenderedComponent<CommandActivity> cut = Render<CommandActivity>(parameters => parameters.Add(
            component => component.State,
            new BankAccountAggregateState
            {
                CommandHistory = ImmutableList.Create(rejected, accepted),
                LastCommandSucceeded = true,
            }));
        Assert.Contains("1 accepted", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("1 failed", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("withdraw-rejected", cut.Find("tbody tr:last-child").TextContent, StringComparison.Ordinal);
        Assert.Contains("Failed", cut.Find("tbody tr:last-child").TextContent, StringComparison.Ordinal);
        Assert.Contains("insufficient-funds", cut.Find("tbody tr:last-child").TextContent, StringComparison.Ordinal);
        Assert.Contains("deposit-accepted", cut.Find("tbody tr:first-child").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Actual server rejections are exposed as actionable alerts.</summary>
    [Fact]
    public void RejectedResponseExposesServerReason()
    {
        using IRenderedComponent<CommandActivity> cut = Render<CommandActivity>(parameters => parameters.Add(
            component => component.State,
            new BankAccountAggregateState
            {
                LastCommandSucceeded = false,
                ErrorCode = "forbidden",
                ErrorMessage = "Server error (403): access denied.",
            }));
        Assert.Contains("Latest request: failed.", cut.Find("[role='status']").TextContent, StringComparison.Ordinal);
        Assert.Contains("403", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
        Assert.Contains("forbidden", cut.Find("[role='alert']").TextContent, StringComparison.Ordinal);
    }
}