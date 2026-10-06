using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using Mississippi.Inlet.Client.Abstractions.State;

using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;
using MississippiSamples.Spring.Client.Features.AuthProofAggregate.Actions;
using MississippiSamples.Spring.Client.Features.AuthProofAggregate.State;
using MississippiSamples.Spring.Client.Features.AuthProofRead;
using MississippiSamples.Spring.Client.Features.AuthProofSaga.Actions;
using MississippiSamples.Spring.Client.Features.AuthProofSaga.State;
using MississippiSamples.Spring.Client.Features.AuthSimulation;


namespace MississippiSamples.Spring.Client.Pages;

/// <summary>
///     Auth proof demonstration page.
/// </summary>
public sealed partial class AuthProofPage
{
    private const string DefaultEntityId = "auth-proof";

    private string entityIdInput = DefaultEntityId;

    private string? lastCorrelationId;

    private Guid? lastSagaId;

    private string? subscribedEntityId;

    private string? subscribedPersonaName;

    private AuthSimulationState ActivePersona => Select<AuthSimulationState, AuthSimulationState>(state => state);

    private string ActivePersonaDescription =>
        Select<AuthSimulationState, string>(AuthSimulationSelectors.GetDescription);

    private string ActivePersonaName => Select<AuthSimulationState, string>(AuthSimulationSelectors.GetName);

    private List<(string Name, string Value)> AggregateStateRows => BuildSnapshotRows(AggregateStateSnapshot);

    private AuthProofAggregateState AggregateStateSnapshot =>
        Select<AuthProofAggregateState, AuthProofAggregateState>(state => state);

    private bool IsAuthProjectionLoading => ProtectedReadState.IsLoading;

    private bool IsPersonaReadCurrent =>
        string.Equals(ProtectedReadState.PersonaName, ActivePersonaName, StringComparison.Ordinal) &&
        string.Equals(ProtectedReadState.EntityId, ProjectionEntityIdDisplay, StringComparison.Ordinal);

    private string LastCorrelationIdDisplay => lastCorrelationId ?? "—";

    private string LastSagaIdDisplay => lastSagaId?.ToString() ?? "—";

    private string ProjectionEntityIdDisplay =>
        string.IsNullOrWhiteSpace(entityIdInput) ? DefaultEntityId : entityIdInput.Trim();

    private string? ProjectionReadError => ProtectedReadState.ErrorMessage;

    private AuthProofProjectionDto? ProjectionSnapshot => ProtectedReadState.Data;

    private List<(string Name, string Value)> ProjectionSnapshotRows =>
        BuildSnapshotRows(ProjectionStateSnapshot.GetProjection<AuthProofProjectionDto>(ProjectionEntityIdDisplay));

    private ProjectionsFeatureState ProjectionStateSnapshot =>
        Select<ProjectionsFeatureState, ProjectionsFeatureState>(state => state);

    private long ProjectionVersion => ProtectedReadState.Version;

    private AuthProofReadState ProtectedReadState => Select<AuthProofReadState, AuthProofReadState>(state => state);

    private List<(string Name, string Value)> SagaStateRows => BuildSnapshotRows(SagaStateSnapshot);

    private AuthProofSagaState SagaStateSnapshot => Select<AuthProofSagaState, AuthProofSagaState>(state => state);

    private static List<(string Name, string Value)> BuildSnapshotRows(
        object? snapshot
    )
    {
        if (snapshot is null)
        {
            return [];
        }

        PropertyInfo[] properties = snapshot.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Array.Sort(properties, static (left, right) => string.CompareOrdinal(left.Name, right.Name));
        List<(string Name, string Value)> rows = [];
        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(snapshot);
            rows.Add((property.Name, FormatSnapshotValue(value)));
        }

        return rows;
    }

    private static string FormatSnapshotValue(
        object? value
    )
    {
        if (value is null)
        {
            return "—";
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToString("u", CultureInfo.InvariantCulture);
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            int count = 0;
            foreach (object? ignoredItem in enumerable)
            {
                count++;
            }

            return $"{count} item(s)";
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "—";
    }

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing && !string.IsNullOrWhiteSpace(subscribedEntityId))
        {
            UnsubscribeFromProjection<AuthProofProjectionDto>(subscribedEntityId);
            subscribedEntityId = null;
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
        string targetEntityId = ProjectionEntityIdDisplay;
        if (string.Equals(subscribedEntityId, targetEntityId, StringComparison.Ordinal))
        {
            if (!string.Equals(subscribedPersonaName, ActivePersonaName, StringComparison.Ordinal) ||
                ProtectedReadState.RequestId is null)
            {
                subscribedPersonaName = ActivePersonaName;
                RefreshProtectedRead();
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(subscribedEntityId))
        {
            UnsubscribeFromProjection<AuthProofProjectionDto>(subscribedEntityId);
        }

        SubscribeToProjection<AuthProofProjectionDto>(targetEntityId);
        subscribedEntityId = targetEntityId;
        subscribedPersonaName = ActivePersonaName;
        RefreshProtectedRead();
    }

    private void RecordAuthenticatedAccess() =>
        Dispatch(new RecordAuthenticatedAccessAction(ProjectionEntityIdDisplay));

    private void RecordPolicyAccess() => Dispatch(new RecordPolicyAccessAction(ProjectionEntityIdDisplay));

    private void RecordRoleAccess() => Dispatch(new RecordRoleAccessAction(ProjectionEntityIdDisplay));

    private void RefreshProtectedRead() =>
        Dispatch(new ReadAuthProofProjectionAction(Guid.NewGuid(), ProjectionEntityIdDisplay, ActivePersona));

    private void StartAuthProofSaga()
    {
        Guid sagaId = Guid.NewGuid();
        string correlationId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        lastSagaId = sagaId;
        lastCorrelationId = correlationId;
        Dispatch(new StartAuthProofSagaAction(sagaId, ProjectionEntityIdDisplay, correlationId));
    }

    private void UseAuthProofClaimPersona() => Dispatch(AuthSimulationProfiles.AuthProofClaim);

    private void UseAuthProofRolePersona() => Dispatch(AuthSimulationProfiles.AuthProofRole);

    private void UseFullAccessPersona() => Dispatch(AuthSimulationProfiles.FullAccess);

    private void UseOperatorOnlyPersona() => Dispatch(AuthSimulationProfiles.OperatorRoles);

    private void UseUnauthenticatedPersona() => Dispatch(AuthSimulationProfiles.Unauthenticated);
}