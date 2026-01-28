using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a saga for infrastructure code generation.
/// </summary>
/// <remarks>
///     <para>
///         When applied to a saga state record that also has
///         <c>SagaOptionsAttribute</c>, <c>BrookNameAttribute</c>, and <c>SnapshotStorageNameAttribute</c>,
///         the following generators will produce code:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Sdk.Silo.Generators</term>
///             <description>
///                 Generates <c>Add{Saga}Saga()</c> extension method that registers
///                 event types, saga steps, compensations, reducers, effects, and snapshot converters.
///             </description>
///         </item>
///     </list>
///     <para>
///         Saga steps are discovered from the <c>Steps</c> sub-namespace by looking for classes
///         that extend <c>SagaStepBase&lt;TSaga&gt;</c> and have <c>[SagaStep]</c> applied.
///     </para>
///     <para>
///         Compensations are discovered from the <c>Compensations</c> sub-namespace by looking for classes
///         that extend <c>SagaCompensationBase&lt;TSaga&gt;</c> and have <c>[SagaCompensation]</c> applied.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         [BrookName("CONTOSO", "BANKING", "TRANSFER")]
///         [SnapshotStorageName("CONTOSO", "BANKING", "TRANSFERSTATE")]
///         [SagaOptions(CompensationStrategy = CompensationStrategy.Immediate)]
///         [GenerateSagaEndpoints]
///         [GenerateSerializer]
///         public sealed record TransferFundsSagaState : ISagaDefinition
///         {
///             public static string SagaName =&gt; "TransferFunds";
///             public decimal Amount { get; init; }
///         }
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateSagaEndpointsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the saga input type for command handling.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, the generator will also register a <c>StartSagaCommandHandler</c>
    ///         for this saga with the specified input type.
    ///     </para>
    ///     <para>
    ///         If not set, only steps, compensations, effects, and reducers will be registered.
    ///     </para>
    /// </remarks>
    public Type? InputType { get; set; }
}