using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using global::Spring.Client.Features.AuthProof.Dtos;
using global::Spring.Client.Features.AuthProofAggregate.Actions;
using global::Spring.Client.Features.AuthProofAggregate.State;
using global::Spring.Client.Features.AuthProofSaga.Actions;
using global::Spring.Client.Features.AuthProofSaga.State;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.Actions;
using Mississippi.Spring.Client.Features.AuthSimulation;


namespace Mississippi.Spring.Client.Pages;

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

    private string ActivePersonaDescription =>
        Select<AuthSimulationState, string>(AuthSimulationSelectors.GetDescription);

    private string ActivePersonaName => Select<AuthSimulationState, string>(AuthSimulationSelectors.GetName);

    private List<(string Name, string Value)> AggregateStateRows => BuildSnapshotRows(AggregateStateSnapshot);

    private AuthProofAggregateState AggregateStateSnapshot =>
        Select<AuthProofAggregateState, AuthProofAggregateState>(state => state);

    private string LastCorrelationIdDisplay => lastCorrelationId ?? "—";

    private string LastSagaIdDisplay => lastSagaId?.ToString() ?? "—";

    private string ProjectionEntityIdDisplay =>
        string.IsNullOrWhiteSpace(entityIdInput) ? DefaultEntityId : entityIdInput.Trim();

    private AuthProofProjectionDto? ProjectionSnapshot =>
        GetProjection<AuthProofProjectionDto>(ProjectionEntityIdDisplay);

    private List<(string Name, string Value)> ProjectionSnapshotRows => BuildSnapshotRows(ProjectionSnapshot);

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
        Array.Sort(
            properties,
            static (
                left,
                right
            ) => string.CompareOrdinal(left.Name, right.Name));
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
            return;
        }

        if (!string.IsNullOrWhiteSpace(subscribedEntityId))
        {
            UnsubscribeFromProjection<AuthProofProjectionDto>(subscribedEntityId);
        }

        SubscribeToProjection<AuthProofProjectionDto>(targetEntityId);
        subscribedEntityId = targetEntityId;
    }

    private void NavigateToIndex() => Dispatch(new NavigateAction("/"));

    private void RecordAuthenticatedAccess() =>
        Dispatch(new RecordAuthenticatedAccessAction(ProjectionEntityIdDisplay));

    private void RecordPolicyAccess() => Dispatch(new RecordPolicyAccessAction(ProjectionEntityIdDisplay));

    private void RecordRoleAccess() => Dispatch(new RecordRoleAccessAction(ProjectionEntityIdDisplay));

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