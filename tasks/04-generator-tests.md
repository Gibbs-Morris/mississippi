# Task 04: Generator Tests

## Objective

Create comprehensive L0 tests for all new saga generators to ensure they produce valid, compilable code.

## Rationale

Generator bugs are hard to debug at runtime. L0 tests that verify generated code syntax and content catch issues early.

## Deliverables

### 1. `SagaControllerGeneratorTests`

**Location:** `tests/Inlet.Server.Generators.L0Tests/SagaControllerGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesControllerForSagaWithInputType` - Basic controller generation
- [ ] `GeneratorIncludesStartEndpoint` - POST /{sagaId} endpoint present
- [ ] `GeneratorIncludesStatusEndpoint` - GET /{sagaId}/status endpoint present
- [ ] `GeneratorUsesKebabCaseRoute` - Route is `api/sagas/transfer-funds` not `api/sagas/TransferFunds`
- [ ] `GeneratorSkipsSagaWithoutInputType` - No controller if `InputType` not set
- [ ] `GeneratorIncludesMapperDependency` - Constructor has mapper parameter
- [ ] `GeneratorIncludesOrchestratorDependency` - Constructor has orchestrator parameter

### 2. `SagaServerDtoGeneratorTests`

**Location:** `tests/Inlet.Server.Generators.L0Tests/SagaServerDtoGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesDtoWithAllInputProperties` - All input properties present
- [ ] `GeneratorIncludesCorrelationIdProperty` - CorrelationId always added
- [ ] `GeneratorUsesRequiredModifier` - Non-nullable properties are required
- [ ] `GeneratorHandlesNullableProperties` - Nullable properties preserved
- [ ] `GeneratorHandlesDecimalProperties` - Decimal type handled correctly

### 3. `SagaClientActionsGeneratorTests`

**Location:** `tests/Inlet.Client.Generators.L0Tests/SagaClientActionsGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesPrimaryAction` - `Start{Saga}SagaAction` generated
- [ ] `GeneratorProducesExecutingAction` - `Start{Saga}SagaExecutingAction` generated
- [ ] `GeneratorProducesSucceededAction` - `Start{Saga}SagaSucceededAction` generated
- [ ] `GeneratorProducesFailedAction` - `Start{Saga}SagaFailedAction` generated
- [ ] `PrimaryActionImplementsISagaAction` - Inherits from ISagaAction
- [ ] `PrimaryActionHasSagaIdProperty` - SagaId is Guid type
- [ ] `PrimaryActionHasInputProperties` - All input properties mapped

### 4. `SagaClientActionEffectsGeneratorTests`

**Location:** `tests/Inlet.Client.Generators.L0Tests/SagaClientActionEffectsGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesActionEffect` - Effect class generated
- [ ] `EffectInheritsSagaActionEffectBase` - Correct base class
- [ ] `EffectHasCorrectSagaRoute` - Route property matches saga name
- [ ] `EffectHasRequiredDependencies` - HttpClient, Mapper, Store in constructor

### 5. `SagaClientStateGeneratorTests`

**Location:** `tests/Inlet.Client.Generators.L0Tests/SagaClientStateGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesStateRecord` - State record generated
- [ ] `StateHasIsExecutingProperty` - bool IsExecuting present
- [ ] `StateHasExecutingSagaIdProperty` - Guid? ExecutingSagaId present
- [ ] `StateHasErrorMessageProperty` - string? ErrorMessage present
- [ ] `StateHasLastStartedSagaIdProperty` - Guid? LastStartedSagaId present

### 6. `SagaClientReducersGeneratorTests`

**Location:** `tests/Inlet.Client.Generators.L0Tests/SagaClientReducersGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesExecutingReducer` - Reducer for executing action
- [ ] `GeneratorProducesSucceededReducer` - Reducer for succeeded action
- [ ] `GeneratorProducesFailedReducer` - Reducer for failed action
- [ ] `ExecutingReducerSetsIsExecutingTrue` - Correct state mutation
- [ ] `SucceededReducerClearsIsExecuting` - Correct state mutation
- [ ] `FailedReducerSetsErrorMessage` - Correct state mutation

### 7. `SagaClientRegistrationGeneratorTests`

**Location:** `tests/Inlet.Client.Generators.L0Tests/SagaClientRegistrationGeneratorTests.cs`

**Test Cases:**

- [ ] `GeneratorProducesRegistrationMethod` - `Add{Saga}SagaFeature()` exists
- [ ] `RegistrationIncludesStateRegistration` - State registered
- [ ] `RegistrationIncludesReducers` - All reducers registered
- [ ] `RegistrationIncludesActionEffect` - Effect registered
- [ ] `RegistrationIncludesMapper` - Mapper registered

## Test Infrastructure

Use same patterns as existing generator tests:

- `RunGenerator()` helper that compiles source with generator
- Assert on generated source text content
- Assert no diagnostic errors

**Example:**

```csharp
[Fact]
public void GeneratorProducesControllerForSagaWithInputType()
{
    // Arrange
    string source = """
        [GenerateSagaEndpoints(InputType = typeof(TransferFundsSagaInput))]
        [BrookName("SPRING", "BANKING", "TRANSFER")]
        public sealed record TransferFundsSagaState : ISagaDefinition
        {
            public static string SagaName => "TransferFunds";
        }
        
        public sealed record TransferFundsSagaInput
        {
            public required string SourceAccountId { get; init; }
            public required decimal Amount { get; init; }
        }
        """;
    
    // Act
    (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) = 
        RunGenerator(source);
    
    // Assert
    Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    Assert.Contains("TransferFundsSagaController", 
        result.GeneratedTrees.First().ToString());
}
```

## Acceptance Criteria

- [ ] All test files created in appropriate L0Tests projects
- [ ] All listed test cases implemented
- [ ] All tests pass
- [ ] Tests follow existing patterns in codebase
- [ ] Tests cover happy path and edge cases
- [ ] No flaky tests

## Dependencies

- [02-server-generators](02-server-generators.md) - Server generators to test
- [03-client-generators](03-client-generators.md) - Client generators to test

## Blocked By

- [02-server-generators](02-server-generators.md)
- [03-client-generators](03-client-generators.md)

## Blocks

- Nothing (tests can run in parallel with implementation)
