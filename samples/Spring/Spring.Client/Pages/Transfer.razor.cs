using System;

using Spring.Client.Features.TransferFundsSaga.Actions;
using Spring.Client.Features.TransferFundsSaga.Dtos;
using Spring.Client.Features.TransferFundsSaga.State;


namespace Spring.Client.Pages;

/// <summary>
///     Transfer Funds page for initiating saga-based money transfers.
/// </summary>
/// <remarks>
///     <para>
///         This page demonstrates the saga pattern with real-time status updates.
///         Users can initiate a transfer and watch the saga progress through steps.
///     </para>
/// </remarks>
public sealed partial class Transfer
{
    private decimal amount = 100m;

    private Guid? currentSagaId;

    private string destinationAccountId = string.Empty;

    private string sourceAccountId = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether a transfer can be started.
    /// </summary>
    private bool CanStartTransfer =>
        !IsTransferInProgress &&
        !string.IsNullOrWhiteSpace(sourceAccountId) &&
        !string.IsNullOrWhiteSpace(destinationAccountId) &&
        !string.Equals(sourceAccountId, destinationAccountId, StringComparison.OrdinalIgnoreCase) &&
        amount > 0;

    /// <summary>
    ///     Gets the current saga ID if a transfer is in progress or completed.
    /// </summary>
    private Guid? CurrentSagaId => currentSagaId;

    /// <summary>
    ///     Gets a value indicating whether a transfer is currently in progress.
    /// </summary>
    private bool IsTransferInProgress =>
        GetState<TransferFundsSagaFeatureState>().IsExecuting;

    /// <summary>
    ///     Gets the current saga status projection.
    /// </summary>
    private SagaStatusDto? SagaStatus =>
        currentSagaId.HasValue
            ? GetProjection<SagaStatusDto>(currentSagaId.Value.ToString())
            : null;

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing && currentSagaId.HasValue)
        {
            UnsubscribeFromProjection<SagaStatusDto>(currentSagaId.Value.ToString());
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///     Gets the CSS class for a step based on its order.
    /// </summary>
    /// <param name="stepOrder">The step order (1-based).</param>
    /// <returns>The CSS class to apply.</returns>
    private string GetStepClass(
        int stepOrder
    )
    {
        if (SagaStatus is null)
        {
            return "pending";
        }

        int? currentOrder = SagaStatus.CurrentStep?.StepOrder;

        if (string.Equals(SagaStatus.Phase, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return "completed";
        }

        if (string.Equals(SagaStatus.Phase, "Failed", StringComparison.OrdinalIgnoreCase) && currentOrder == stepOrder)
        {
            return "failed";
        }

        if (string.Equals(SagaStatus.Phase, "Compensating", StringComparison.OrdinalIgnoreCase))
        {
            return currentOrder >= stepOrder ? "compensating" : "completed";
        }

        if (currentOrder == stepOrder)
        {
            return "running";
        }

        if (currentOrder > stepOrder)
        {
            return "completed";
        }

        // Check if step is in completed steps
        foreach (SagaStepDto step in SagaStatus.CompletedSteps)
        {
            if (step.StepOrder == stepOrder)
            {
                return "completed";
            }
        }

        return "pending";
    }

    /// <summary>
    ///     Gets the icon for a step based on its order.
    /// </summary>
    /// <param name="stepOrder">The step order (1-based).</param>
    /// <returns>The icon character to display.</returns>
    private string GetStepIcon(
        int stepOrder
    ) =>
        GetStepClass(stepOrder) switch
        {
            "completed" => "✓",
            "running" => "●",
            "failed" => "✗",
            "compensating" => "↺",
            _ => "○",
        };

    /// <summary>
    ///     Gets the status text for a step based on its order.
    /// </summary>
    /// <param name="stepOrder">The step order (1-based).</param>
    /// <returns>The status text to display.</returns>
    private string GetStepStatus(
        int stepOrder
    ) =>
        GetStepClass(stepOrder) switch
        {
            "completed" => "Done",
            "running" => "Running...",
            "failed" => "Failed",
            "compensating" => "Rolling back",
            _ => "Pending",
        };

    /// <summary>
    ///     Starts a new transfer saga.
    /// </summary>
    private void StartTransfer()
    {
        // Unsubscribe from previous saga if any
        if (currentSagaId.HasValue)
        {
            UnsubscribeFromProjection<SagaStatusDto>(currentSagaId.Value.ToString());
        }

        Guid sagaId = Guid.NewGuid();
        currentSagaId = sagaId;

        // Subscribe to saga status updates
        SubscribeToProjection<SagaStatusDto>(sagaId.ToString());

        // Dispatch the generated saga action
        Dispatch(new StartTransferFundsSagaAction(
            sagaId,
            sourceAccountId.Trim(),
            destinationAccountId.Trim(),
            amount));
    }
}
