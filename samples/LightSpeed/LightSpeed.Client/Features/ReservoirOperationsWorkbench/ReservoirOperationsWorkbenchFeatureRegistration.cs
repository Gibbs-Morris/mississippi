using Mississippi.Reservoir.Abstractions;


namespace MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;

/// <summary>
///     Extension methods for registering the LightSpeed Reservoir operations-workbench feature.
/// </summary>
internal static class ReservoirOperationsWorkbenchFeatureRegistration
{
    /// <summary>
    ///     Adds the LightSpeed Reservoir operations-workbench feature to the Reservoir builder.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddReservoirOperationsWorkbenchFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<ReservoirOperationsWorkbenchState>(feature => feature
            .AddReducer<
                ReservoirOperationsWorkbenchActions.ApplyActionRequested>(
                ReservoirOperationsWorkbenchReducers.ApplyAction)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.DraftAssignedAnalystChanged>(
                ReservoirOperationsWorkbenchReducers.SetDraftAssignedAnalyst)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.DraftDispositionChanged>(
                ReservoirOperationsWorkbenchReducers.SetDraftDisposition)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.DraftResponseSummaryChanged>(
                ReservoirOperationsWorkbenchReducers.SetDraftResponseSummary)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.DraftReviewNotesChanged>(
                ReservoirOperationsWorkbenchReducers.SetDraftReviewNotes)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.ReviewCanceled>(ReservoirOperationsWorkbenchReducers.CancelReview)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.ReviewOpened>(ReservoirOperationsWorkbenchReducers.OpenReview)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.ReviewSaved>(ReservoirOperationsWorkbenchReducers.SaveReview)
            .AddReducer<
                ReservoirOperationsWorkbenchActions.SearchChanged>(ReservoirOperationsWorkbenchReducers.SetSearchText)
            .AddReducer<ReservoirOperationsWorkbenchActions.SelectionChanged>(
                ReservoirOperationsWorkbenchReducers.SetSelectedWorkItem)
            .AddReducer<ReservoirOperationsWorkbenchActions.StageFilterChanged>(
                ReservoirOperationsWorkbenchReducers.SetStageFilter));
        return builder;
    }
}