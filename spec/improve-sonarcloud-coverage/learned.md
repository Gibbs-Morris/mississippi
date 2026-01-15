# Learned - Coverage Gap Analysis

## Verified Repository Facts

### Test Framework
- xUnit with Allure.Xunit for reporting
- NSubstitute and Moq for mocking
- Test naming: `<Product>.<Feature>.L0Tests` for pure unit tests
- Allure attributes: `[AllureParentSuite]`, `[AllureSuite]`, `[AllureSubSuite]`, `[AllureFeature]`

### Files with Coverage Gaps (< 95% on new lines)

#### SKIP - Source Generators (cannot test in L0)
- `AggregateServiceGenerator.cs` - 0% (86 lines)
- `ProjectionApiGenerator.cs` - 0% (168 lines)
- Metadata models (NestedTypeMetadata, PropertyMetadata)

#### SKIP - SignalR/WebAssembly (requires real infrastructure)
- `AqueductHubLifetimeManager.cs` - 0% (160 lines)
- `InletHub.cs` - 0% (29 lines)
- `InletSignalREffect.cs` - 13.7% (145 lines)
- `SignalRClientGrain.cs` - 83% (8 lines)

#### SKIP - Test Infrastructure
- `Program.cs` in L2Tests.AppHost

#### EASY L0 Targets (testable with mocks)
| File | Coverage | Uncovered | Testability |
|------|----------|-----------|-------------|
| `AqueductSiloOptions.cs` | 64.3% | 5 lines | 8/10 - UseMemoryStreams() parameterless overload not tested |
| `UxProjectionMetrics.cs` | 76.5% | 8 lines | 7/10 - Static methods with TagList |
| `AqueductMetrics.cs` | 87.2% | 6 lines | 7/10 - Similar static pattern |
| `SnapshotMetrics.cs` | 90.4% | 5 lines | 7/10 - Similar static pattern |
| `SnapshotStorageMetrics.cs` | 88.2% | 4 lines | 7/10 - Similar static pattern |

#### MEDIUM L0 Targets (may need specific edge case setup)
| File | Coverage | Uncovered | Issue |
|------|----------|-----------|-------|
| `InletStore.cs` | 94% | 8 lines | Reflection-based handlers, null entityId paths |
| `AqueductNotifier.cs` | 73.5% | 9 lines | SendToAllAsync stream path not tested |
| `UxProjectionEndpointExtensions.cs` | 38.4% | 69 lines | Reflection-based endpoint handlers |

### Existing Test Patterns

#### AqueductSiloOptionsTests.cs
- Tests constructor validation
- Tests default property values
- Tests property setters
- Tests `UseMemoryStreams(string, string)` parameter validation
- **MISSING**: `UseMemoryStreams()` parameterless overload

#### AqueductNotifierTests.cs (444 lines)
- Comprehensive argument validation
- `SendToConnectionAsync` and `SendToGroupAsync` with grain mocks
- **MISSING**: `SendToAllAsync` happy path (needs Orleans stream mock)

#### ProjectionActionsTests.cs
- All action types tested for constructor, properties, validation
- Already comprehensive

### Evidence Paths
- `tests/Aqueduct.Grains.L0Tests/AqueductSiloOptionsTests.cs`
- `tests/Aqueduct.L0Tests/AqueductNotifierTests.cs`
- `tests/Inlet.Abstractions.L0Tests/Actions/ProjectionActionsTests.cs`
- `src/Aqueduct.Grains/AqueductSiloOptions.cs`
- `src/Aqueduct/AqueductNotifier.cs`
