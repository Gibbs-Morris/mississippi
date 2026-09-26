# Event-sourced sample feature bindings

Use these local sources with the portable feature workflow. They bind the
procedure to Mississippi; they do not replace the applicable instruction Rules.

| Decision | Authoritative local sources |
| --- | --- |
| Sample structure, state boundaries, storage, effects and generation | [Framework policy](../instructions/mississippi-framework.instructions.md), [sample discipline](../instructions/coding-discipline.instructions.md) |
| Domain types, handler/reducer contracts, serialization and naming | [Domain policy](../instructions/domain-modeling.instructions.md), [serialization](../instructions/orleans-serialization.instructions.md), [storage names](../instructions/storage-type-naming.instructions.md) |
| Commands, aggregates and generated exposure | [Generator attributes](../../src/Inlet.Generators.Abstractions/), [handler base](../../src/DomainModeling.Abstractions/CommandHandlerBase.cs) |
| Immutable reduction and projections | [Reducer base](../../src/Tributary.Abstractions/EventReducerBase.cs), [Spring domain](../../samples/Spring/Spring.Domain/) |
| Client composition and store integration | [Client builder](../../src/Hosting.Client/ClientBuilder.cs), [Spring client](../../samples/Spring/Spring.Client/), [Blazor policy](../instructions/blazor-ux-guidelines.instructions.md) |
| Runtime composition and keyed providers | [Runtime builder](../../src/Hosting.Runtime/RuntimeBuilder.cs), [Spring runtime](../../samples/Spring/Spring.Runtime/Program.cs), [keyed services](../instructions/keyed-services.instructions.md) |
| Event compatibility and deployment baseline | [Compatibility policy](../instructions/backwards-compatibility.instructions.md) |
| Quality and browser evidence | [Testing policy](../instructions/testing.instructions.md), [UX policy](../instructions/ux-validation.instructions.md), [Spring test suites](../../samples/Spring/TESTING.md), [Spring validation](../../README.md#validate-spring-after-a-change) |

For a concrete command/reducer pair, inspect
[DepositFundsHandler](../../samples/Spring/Spring.Domain/Aggregates/BankAccount/Handlers/DepositFundsHandler.cs)
and [FundsDepositedReducer](../../samples/Spring/Spring.Domain/Aggregates/BankAccount/Reducers/FundsDepositedReducer.cs).
Use the current source for signatures and generated names. Sample visibility
does not automatically grant an exception to retained instruction rules;
reconcile a relevant source/policy conflict before implementing a feature.

The domain project owns aggregates, commands, events, handlers and projections.
Runtime and Gateway remain configuration/registration hosts. Client owns UX
and local features. Optional AppHost orchestration follows the existing sample.
Generated artifacts come from domain inputs, not hand-maintained copies.
The required attributes, naming, folder roles, client flow, server flow and
effect boundaries remain in the three instruction Rules sections; their tables
and prose are not independent authority here.
Replay and recovery claims also depend on event retention and serializer
compatibility; an event log alone does not prove arbitrary rollback support.

Choose validation from those policies and current scripts. The relevant fast
quality route for Spring domain changes uses
`pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject samples/Spring/Spring.Domain.L0Tests/Spring.Domain.L0Tests.csproj -SourceProject samples/Spring/Spring.Domain/Spring.Domain.csproj -SkipMutation`.
Bare test names resolve only below `tests/`; sample coverage needs the explicit
source path because automatic mapping prefers references below `src/`.
For other targets, inspect the actual test/source paths and command mapping.
Spring readiness
uses `pwsh ./test-spring.ps1 -Doctor`, followed by executed selected suites where
applicable. `READY` is not `PASS`. The full repository quality gates remain
required under their policy. This guidance migration changes no application
behavior and does not claim execution of a new sample feature.

## Consistency Model Separation

Write decisions use the aggregate's authoritative current state through its
command-handling and persistence pipeline. Orleans serial execution within an
aggregate activation is the local write boundary; it does not establish a
cross-aggregate transaction. Cross-aggregate coordination follows the existing
saga or eventual-consistency contract.

Brooks may feed multiple projections asynchronously. These optimized read views
are eventually consistent: command success does not prove the client has already
received the corresponding projection update. Inspect the actual pipeline and
subscription evidence before making stronger freshness or atomicity claims.

## Domain type suffix examples

These illustrative names retain the former naming table; they are not new APIs
or exceptions to general C# visibility and naming. The [naming Rules](../instructions/naming.instructions.md#rules-rfc-2119),
[domain Rules](../instructions/domain-modeling.instructions.md#rules-rfc-2119),
[registration Rules](../instructions/service-registration.instructions.md#rules-rfc-2119),
and [logging Rules](../instructions/logging-rules.instructions.md#rules-rfc-2119) remain authoritative.

| Type | Suffix | Example |
|------|--------|---------|
| Aggregate state | `Aggregate` | `ChannelAggregate` |
| Command handler | `Handler` | `CreateChannelHandler` |
| Aggregate reducer | `Reducer` | `ChannelCreatedReducer` |
| Projection state | `Projection` | `UserProfileProjection` |
| Projection reducer | `ProjectionReducer` | `UserRegisteredProjectionReducer` |
| Registration class | `Registrations` | `ContosoRegistrations` |
| LoggerExtensions | `LoggerExtensions` | `BrookWriterGrainLoggerExtensions` |

## Serialization and storage examples

These attribute shapes retain the former quick-start examples; they are not
runnable samples or a universal compatibility promise. Read the [serialization Rules](../instructions/orleans-serialization.instructions.md#rules-rfc-2119)
and [storage Rules](../instructions/storage-type-naming.instructions.md#rules-rfc-2119) for the applicable version and persistence boundary.

- Add `[GenerateSerializer]`, `[Alias("Namespace.TypeName")]` (fully qualified type name), `[Id(n)]` (starting at 0) to members.
- Decorate types with `[EventStorageName("ORDER","FULFILLMENT","SHIPPED", version: 1)]` (or appropriate attribute).
