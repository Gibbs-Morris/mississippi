using Mississippi.Reservoir.Blazor.BuiltIn.Navigation.Actions;

using Spring.Client.Features.FlaggedTransactions.Dtos;


namespace Spring.Client.Pages;

/// <summary>
///     Investigations page showing flagged high-value transactions.
/// </summary>
/// <remarks>
///     <para>
///         This page displays transactions that exceeded the AML threshold (£10,000)
///         and were automatically flagged for manual investigation.
///     </para>
///     <para>
///         The projection uses a singleton entity ID ("global") since the
///         <see cref="Domain.Aggregates.TransactionInvestigationQueue.TransactionInvestigationQueueAggregate" />
///         is a global aggregate that receives flags from all bank accounts.
///     </para>
/// </remarks>
public sealed partial class Investigations
{
    /// <summary>
    ///     The singleton entity ID for the investigation queue.
    /// </summary>
    private const string GlobalEntityId = "global";

    private string? subscribedEntityId;

    /// <summary>
    ///     Gets the flagged transactions projection data.
    /// </summary>
    private FlaggedTransactionsProjectionDto? FlaggedProjection =>
        GetProjection<FlaggedTransactionsProjectionDto>(GlobalEntityId);

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing && !string.IsNullOrEmpty(subscribedEntityId))
        {
            UnsubscribeFromProjection<FlaggedTransactionsProjectionDto>(subscribedEntityId);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(
        bool firstRender
    )
    {
        base.OnAfterRender(firstRender);
        ManageProjectionSubscription();
    }

    private void ManageProjectionSubscription()
    {
        if (subscribedEntityId != GlobalEntityId)
        {
            if (!string.IsNullOrEmpty(subscribedEntityId))
            {
                UnsubscribeFromProjection<FlaggedTransactionsProjectionDto>(subscribedEntityId);
            }

            SubscribeToProjection<FlaggedTransactionsProjectionDto>(GlobalEntityId);
            subscribedEntityId = GlobalEntityId;
        }
    }

    private void NavigateToIndex() => Dispatch(new NavigateAction("/"));
}