---
applyTo: '**/*.cs'
---

# Feature-Centric C# Naming & Commenting Playbook

This document establishes comprehensive naming conventions and XML documentation standards for the Mississippi Framework and all related applications. All C# code must follow these guidelines to ensure consistency, maintainability, and alignment with .NET Framework design principles.

## 0. Core Objectives

* **Match .NET Framework design guidelines** so every identifier feels native to .NET.
* Treat **StyleCop SA13xx (naming) and SA16xx (documentation)** violations as build-breaking errors.
* Use a **feature-oriented folder/namespace layout**—never technical silos such as *Services* or *Models*.
* Produce **self-descriptive identifiers and XML comments** detailed enough that a human or LLM can understand intent without opening the body.
* Ensure all prose is **100% factual**—no invented behaviour or placeholders.

## 1. Namespace Construction

| Step | Instruction                                                                            | Example                           |
| ---- | -------------------------------------------------------------------------------------- | --------------------------------- |
| 1    | **Company/Org root** – PascalCase                                                      | `Contoso`                         |
| 2    | **Product or bounded context** – PascalCase                                            | `Accounting`                      |
| 3    | **Feature[.SubFeature]** – PascalCase business terms                                  | `Invoices.Billing`                |
| 4    | *(Optional)* `.Abstractions`, `.Infrastructure`, `.Api`, etc., **only** when essential | `Contoso.Accounting.Invoices.Api` |

**Rule N-1:** Maximum five segments; PascalCase alphanumerics; no underscores; abbreviations only when industry-standard (`IO`, `DB`, `Html`).

## 2. Type-Naming Rules

| ID  | Element                         | Rule                                                                                                                                                            |
| --- | ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T-1 | **Classes / records / structs** | PascalCase noun or noun phrase that expresses core responsibility (`InvoicePaymentProcessor`).                                                                  |
| T-2 | **Approved suffixes**           | Apply when the pattern demands: `…Exception`, `…EventArgs`, `…Attribute`, `…Options`, `…Provider`, `…Handler`, `…Middleware`, `…Controller`, `…Dto`, `…Record`. |
| T-3 | **Abstract base classes**       | Do **not** prefix with "Base" unless consumers must derive from them.                                                                                           |
| T-4 | **Interfaces**                  | Prefix **`I`** + capability adjective/noun (`ILogger`, `IDisposable`, `IHasher`).                                                                               |
| T-5 | **Enums**                       | Type name singular PascalCase; members PascalCase. Use `[Flags]` and powers of two when bitwise.                                                                |

## 3. Member-Naming Rules

| Member Kind                      | Rule                                                                      |
| -------------------------------- | ------------------------------------------------------------------------- |
| **Public / internal methods**    | PascalCase **verb phrase** (`CalculateHash`).                             |
| **Public / internal properties** | PascalCase **noun** (`TotalAmount`).                                      |
| **Boolean properties**           | Prefix with `Is`, `Has`, `Can`, or `Should` (`IsActive`).                 |
| **Dependency injection properties** | `private Type Name { get; }` pattern for all injected dependencies (`Logger`, `Repository`). |
| **Private fields & locals**      | camelCase with **no leading underscore** (`customerId`, `cancellationTokenSource`).  |
| **Constants**                    | PascalCase and meaningful (`MaxRetryCount`).                              |
| **Generic type parameters**      | `T` + descriptive (`TResponse`) or a conventional single letter (`TKey`). |
| **Events**                       | PascalCase past-tense verb (`PaymentCompleted`).                          |

## 4. XML Documentation & Commenting Rules

| ID   | Rule                                                                                                                                                                                                             |
| ---- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| C-1  | Every **publicly accessible** symbol **must** include an XML comment containing `<summary>`.                                                                                                                     |
| C-2  | Follow **class-first narrative flow**: read the type `<summary>` then each member `<summary>`; member docs build on but do not repeat the type doc.                                                              |
| C-3  | Comments must be **richly descriptive yet 100% factual**—no speculation or placeholders.                                                                                                                        |
| C-4  | Minimum tags per symbol: `<summary>` (1–3 sentences); `<param>` for every parameter; `<typeparam>` for every generic; `<returns>` for non-void methods. Add `<remarks>` (≤ 8 lines) or `<example>` when helpful. |
| C-5  | Voice: **imperative, active, present tense** ("Calculates the pro-rata refund."). Avoid "This method…".                                                                                                          |
| C-6  | Prefer **domain vocabulary** over technical jargon.                                                                                                                                                              |
| C-7  | **Validation pass**: parameter names match, no empty tags, no TODOs/TBDs, no contradictions with code.                                                                                                           |
| C-8  | Formatting: `/// ` (triple slash + single space) at the start of each line; block preceded by one blank line except at file top.                                                                                 |
| C-9  | **Internal/private** members require docs only when behaviour is non-trivial or exposed via `InternalsVisibleTo`; suppress StyleCop otherwise.                                                                   |
| C-10 | `<example>` snippets **must compile** in isolation when wrapped in minimal scaffolding.                                                                                                                          |

## 5. Generation Algorithm for the AI Agent

1. **Derive the namespace** per §1.
2. **Select the type kind** (class, record, interface, enum, etc.).
3. **Compose the identifier** using §§2–3 rules.
4. **Create the file path** mirroring the namespace.
5. **Generate members** with compliant names.
6. **Write XML comments** adhering to §4 (C-1 → C-6).
7. **Insert `<example>` snippets** where instructive, ensuring compilation.
8. **Run StyleCop analyzers** (SA1300–SA1314, SA1600–SA1619); fix all diagnostics.
9. **Validate comments** per C-7; regenerate documentation if needed.
10. **Compile implementation and examples**; fail generation if any rule is violated.

## 6. Code Examples

### Proper Namespace and Type Naming

```csharp
// Feature-oriented namespace structure
namespace Mississippi.EventSourcing.Streams
{
    /// <summary>
    /// Provides stream processing capabilities for event sourcing scenarios.
    /// Manages event ordering, batching, and delivery guarantees for distributed systems.
    /// </summary>
    /// <remarks>
    /// This processor ensures exactly-once delivery semantics and maintains
    /// causal ordering within each logical stream partition.
    /// </remarks>
    public sealed class EventStreamProcessor : IDisposable
    {
        // Dependency injection properties following the required pattern
        private ILogger<EventStreamProcessor> Logger { get; }
        private IEventStore EventStore { get; }
        private IOptions<StreamSettings> Settings { get; }

        public EventStreamProcessor(
            ILogger<EventStreamProcessor> logger,
            IEventStore eventStore,
            IOptions<StreamSettings> settings)
        {
            Logger = logger;
            EventStore = eventStore;
            Settings = settings;
        }

        /// <summary>
        /// Gets a value indicating whether the processor is currently active and accepting events.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Processes a batch of events from the specified stream.
        /// </summary>
        /// <param name="streamId">The unique identifier of the stream to process.</param>
        /// <param name="events">The collection of events to process in order.</param>
        /// <returns>A task representing the asynchronous operation, containing the processing result.</returns>
        /// <example>
        /// <code>
        /// var processor = new EventStreamProcessor();
        /// var events = new[] { new OrderCreated(), new OrderPaid() };
        /// var result = await processor.ProcessBatchAsync("order-123", events);
        /// </code>
        /// </example>
        public async Task<ProcessingResult> ProcessBatchAsync(string streamId, IReadOnlyList<DomainEvent> events)
        {
            // Implementation here
        }
    }
}
```

### Interface and Enum Naming

```csharp
namespace Mississippi.EventSourcing.Abstractions
{
    /// <summary>
    /// Defines the contract for persisting and retrieving event streams.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Appends events to the specified stream with optimistic concurrency control.
        /// </summary>
        /// <param name="streamId">The unique identifier of the stream.</param>
        /// <param name="expectedVersion">The expected current version of the stream.</param>
        /// <param name="events">The events to append to the stream.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AppendAsync(string streamId, long expectedVersion, IReadOnlyList<DomainEvent> events);
    }

    /// <summary>
    /// Specifies the consistency level for event store operations.
    /// </summary>
    public enum ConsistencyLevel
    {
        /// <summary>
        /// Eventual consistency with high performance.
        /// </summary>
        Eventual,

        /// <summary>
        /// Strong consistency with guaranteed ordering.
        /// </summary>
        Strong,

        /// <summary>
        /// Session-level consistency for the current session.
        /// </summary>
        Session
    }
}
```

### Boolean Properties and Dependency Injection

```csharp
public sealed class StreamSubscription
{
    // Dependency injection properties following the required pattern
    private ILogger<StreamSubscription> Logger { get; }
    private IEventStore EventStore { get; }
    private IOptions<StreamSettings> Settings { get; }
    
    // Private fields with camelCase, no underscores
    private readonly string streamId;
    private CancellationTokenSource cancellationTokenSource;

    public StreamSubscription(
        ILogger<StreamSubscription> logger,
        IEventStore eventStore,
        IOptions<StreamSettings> settings)
    {
        Logger = logger;
        EventStore = eventStore;
        Settings = settings;
    }

    /// <summary>
    /// Gets a value indicating whether the subscription is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the subscription can be restarted after failure.
    /// </summary>
    public bool CanRestart => !IsRunning && cancellationTokenSource is not null;

    /// <summary>
    /// Gets a value indicating whether the subscription has processed all available events.
    /// </summary>
    public bool HasCaughtUp { get; private set; }
}
```

## 7. Quick Cheat Sheet

```text
# Namespace
Company.Product.Feature[.SubFeature]

# Class / Record
PascalCaseNoun + OptionalSuffix

# Interface
I + PascalCaseCapability

# Enum Members
PascalCaseValue

# Public Members
PascalCase

# Dependency Injection Properties
private Type Name { get; }

# Private Fields & Locals
camelCase (no _)

# Boolean Property Prefixes
Is / Has / Can / Should

# Generic Type Parameters
TDescriptive
```

## 8. StyleCop Integration

All naming conventions are enforced through StyleCop analyzers. The following rules are particularly relevant:

### Naming Rules (SA13xx)
- **SA1300**: Element should begin with uppercase letter
- **SA1301**: Element should begin with lowercase letter
- **SA1302**: Interface names should begin with I
- **SA1303**: Const field names should begin with uppercase letter
- **SA1304**: Non-private readonly fields should begin with uppercase letter
- **SA1305**: Field names should not use Hungarian notation
- **SA1306**: Field names should begin with lowercase letter
- **SA1307**: Accessible fields should begin with uppercase letter
- **SA1308**: Variable names should not be prefixed
- **SA1309**: Field names should not begin with underscore
- **SA1310**: Field names should not contain underscore
- **SA1311**: Static readonly fields should begin with uppercase letter
- **SA1312**: Variable names should begin with lowercase letter
- **SA1313**: Parameter names should begin with lowercase letter
- **SA1314**: Type parameter names should begin with T

### Documentation Rules (SA16xx)
- **SA1600**: Elements should be documented
- **SA1601**: Partial elements should be documented
- **SA1602**: Enumeration items should be documented
- **SA1603**: Documentation should contain meaningful text
- **SA1604**: Element documentation should have summary
- **SA1605**: Partial element documentation should have summary
- **SA1606**: Element documentation should have summary text
- **SA1607**: Partial element documentation should have summary text
- **SA1608**: Element documentation should not have default summary
- **SA1609**: Property documentation should have value
- **SA1610**: Property documentation should have value text
- **SA1611**: Element parameters should be documented
- **SA1612**: Element parameter documentation should match element parameters
- **SA1613**: Element parameter documentation should declare parameter name
- **SA1614**: Element parameter documentation should have text
- **SA1615**: Element return value should be documented
- **SA1616**: Element return value documentation should have text
- **SA1617**: Void return value should not be documented
- **SA1618**: Generic type parameters should be documented
- **SA1619**: Generic type parameters should be documented partial class

## 9. Alignment with Other Instruction Files

This naming convention document aligns with and reinforces patterns established in other instruction files:

### C# General Best Practices Alignment
- **Dependency injection properties**: Follow the `private Type Name { get; }` pattern specified in csharp.instructions.md
- **Immutable objects**: Support the preference for records and init-only properties
- **SOLID principles**: Naming should reflect single responsibility and clear interfaces

### Logging Rules Alignment
- **Logger property naming**: Use `private ILogger<T> Logger { get; }` pattern consistently
- **High-performance logging**: Naming conventions support LoggerMessage pattern usage
- **Structured logging**: Property names should support structured logging requirements

### Orleans Best Practices Alignment
- **POCO grain pattern**: Naming conventions work with `IGrainBase` implementation
- **Extension method usage**: Support for Orleans extension methods like `this.GetPrimaryKey()`
- **Grain naming**: Grain classes should end with "Grain" suffix (e.g., `TodoGrain`)

### Build Rules Alignment
- **Zero warnings policy**: All naming violations must be fixed, not suppressed
- **StyleCop enforcement**: SA13xx and SA16xx rules are treated as build errors
- **Quality standards**: Naming supports comprehensive test coverage and mutation testing

## 10. Reference Summary

* *Microsoft Framework Design Guidelines*, 3rd ed., chapters on Naming & Documentation.
* .NET documentation: *C# Identifier and Namespace Conventions*.
* *StyleCop Analyzers* rule sets SA13xx (Naming) and SA16xx (Documentation).

## 11. Related Guidelines

This document should be read in conjunction with:

- **C# General Development Best Practices** (`.github/instructions/csharp.instructions.md`) - For SOLID principles, dependency injection patterns, and immutable object preferences
- **Logging Rules** (`.github/instructions/logging-rules.instructions.md`) - For Logger property naming and structured logging support
- **Orleans Best Practices** (`.github/instructions/orleans.instructions.md`) - For POCO grain patterns and Orleans-specific naming
- **Build Rules** (`.github/instructions/build-rules.instructions.md`) - For quality standards and zero warnings policy

*End of naming.instructions.md*
