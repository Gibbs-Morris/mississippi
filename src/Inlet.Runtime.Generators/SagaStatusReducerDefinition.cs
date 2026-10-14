using System.Collections.Immutable;


namespace Mississippi.Inlet.Runtime.Generators;

/// <summary>
///     Shares the fixed saga reducer identities and bodies between emission and registration.
/// </summary>
internal sealed class SagaStatusReducerDefinition
{
    /// <summary>
    ///     Identifies the marker selecting saga status reducer generation.
    /// </summary>
    internal const string AttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.GenerateSagaStatusReducersAttribute";

    /// <summary>
    ///     Defines the namespace of the shared saga events.
    /// </summary>
    internal const string EventNamespace = "Mississippi.DomainModeling.Abstractions";

    private const string SagaFailedBody = "Phase = SagaPhase.Failed,\n" +
                                          "ErrorCode = eventData.ErrorCode,\n" +
                                          "ErrorMessage = eventData.ErrorMessage,\n" +
                                          "CompletedAt = eventData.FailedAt,";

    private const string SagaStartedBody = "Phase = SagaPhase.Running,\n" +
                                           "StartedAt = eventData.StartedAt,\n" +
                                           "LastCompletedStepIndex = -1,\n" +
                                           "ErrorCode = null,\n" +
                                           "ErrorMessage = null,\n" +
                                           "CompletedAt = null,";

    private SagaStatusReducerDefinition(
        string className,
        string eventTypeName,
        string reduceBody
    )
    {
        ClassName = className;
        EventTypeName = eventTypeName;
        ReduceBody = reduceBody;
    }

    /// <summary>
    ///     Gets the reducer definitions in their established emission order.
    /// </summary>
    internal static ImmutableArray<SagaStatusReducerDefinition> All { get; } =
    [
        new("SagaStartedStatusReducer", "SagaStartedEvent", SagaStartedBody),
        new("SagaStepCompletedStatusReducer", "SagaStepCompleted", "LastCompletedStepIndex = eventData.StepIndex,"),
        new(
            "SagaStepFailedStatusReducer",
            "SagaStepFailed",
            "ErrorCode = eventData.ErrorCode,\n" + "ErrorMessage = eventData.ErrorMessage,"),
        new("SagaFailedStatusReducer", "SagaFailed", SagaFailedBody),
        new(
            "SagaCompletedStatusReducer",
            "SagaCompleted",
            "Phase = SagaPhase.Completed,\n" + "CompletedAt = eventData.CompletedAt,"),
        new("SagaCompensatingStatusReducer", "SagaCompensating", "Phase = SagaPhase.Compensating,"),
        new(
            "SagaCompensatedStatusReducer",
            "SagaCompensated",
            "Phase = SagaPhase.Compensated,\n" + "CompletedAt = eventData.CompletedAt,"),
    ];

    /// <summary>
    ///     Gets the generated reducer class name.
    /// </summary>
    internal string ClassName { get; }

    /// <summary>
    ///     Gets the shared saga event type name.
    /// </summary>
    internal string EventTypeName { get; }

    /// <summary>
    ///     Gets the projection assignments emitted for the event.
    /// </summary>
    internal string ReduceBody { get; }
}