---
applyTo: '**/*.razor*'
---

# Blazor UX Design System Guidelines

These guidelines define how we design, compose, and test Blazor UI components by combining atomic design,
BEM naming, state management discipline, accessibility, and WebAssembly readiness. Agents MUST use this document
whenever they author or review Razor components, regardless of whether the design system is fully matured
or still emerging.

## Intent

- Build a scalable component library that grows from atoms to pages without duplicating behavior or styles.
- Keep authoring consistent with our existing C# and logging instructions while embracing Blazor idioms.
- Enable teams to ship usable interfaces early, even before a full design framework exists, by layering
  components predictably.

## Atomic Layering Cheat Sheet

- **Atom**
  - Purpose: smallest functional unit (button, icon, input).
  - Typical location: `Components/Atoms/<Name>/<Name>.razor`.
  - Lifecycle: prefer stateless implementations and expose raw
    parameters only.
- **Molecule**
  - Purpose: cohesive cluster of atoms such as label, input, and
    validation summary.
  - Typical location: `Components/Molecules/<Name>/<Name>.razor`.
  - Lifecycle: owns light layout logic and delegates behavior back to
    atoms via parameters.
- **Organism**
  - Purpose: feature-level composition like a search bar, header, or
    data row.
  - Typical location: `Components/Organisms/<Name>/<Name>.razor`.
  - Lifecycle: encapsulates business rules and emits domain events via
    strongly typed callbacks.
- **Template**
  - Purpose: page skeleton that defines layout regions.
  - Typical location: `Pages/Templates/<Name>/<Name>.razor`.
  - Lifecycle: contains slots or `RenderFragment` parameters and stays
    data-agnostic.
- **Page**
  - Purpose: routed experience that binds templates to data sources.
  - Typical location: `Pages/<Feature>/<Name>.razor`.
  - Lifecycle: handles orchestration, navigation, and service calls.

> When unsure which layer to use, ask: *"Can this component stand alone and be reused without knowledge of
> larger flows?"* If yes, it belongs lower in the hierarchy.

## Source Layout and Naming Rules

1. **Directory structure MUST mirror atomic layers.** Keep components under a single root (for example,
  `src/Feature/Client/Components`). Introduce folders for `Atoms`, `Molecules`, `Organisms`, and so on.
2. **Exactly one component per directory.** Every component MUST live in a folder with its Razor file, backing partial
  class, styles, and tests:
   - `Button/Button.razor`
   - `Button/Button.razor.cs`
   - `Button/Button.razor.css`
   - `Button/Button.tests.md` (optional spec or visual notes)
3. **Name atoms with nouns, molecules with noun phrases, organisms with domain terms.** For example,
  `PrimaryButton`, `LabeledInput`, `OrderSummaryRow`. This naming guidance MUST be followed for consistency.
4. **Partial class backing files MUST follow the same namespace model as C# instructions.** Keep them `partial`
  and `sealed` unless composition requires extension.

## Component Responsibility Rules

- **View-only components MUST stay presentational.** Razor components expose `[Parameter]` inputs and
  `EventCallback` outputs, own only ephemeral UI state, and NEVER host business rules.
- **Always split markup and logic.** Use `.razor` for markup, `.razor.cs` partial classes for logic.
  This pattern MUST be followed to keep diffs focused, support unit tests, and align with repository C# standards.
- **Pages act as containers.** Routed pages orchestrate application services and store interactions.
  Child components MUST NOT call APIs or manage side effects directly.
- **Domain logic MUST live outside the UI.** Business rules belong in domain or application services to keep
  a single source of truth across channels.

## State Management and Effects

- **Adopt Redux-style state.** Use a single store with actions, reducers, selectors, and effects (Fluxor or
  equivalent) for predictable updates and simplified unit testing. A fragmented state approach SHOULD NOT be used.
- **Keep effects thin and testable.** Effects MUST call interfaces that encapsulate IO and business requests.
  Inject these services through DI so tests can mock behavior without touching UI components.
- **Selectors MUST feed components.** Components bind to memoized selectors instead of raw store state to avoid
  unnecessary re-renders and simplify reasoning.

## API Contracts and Cross-Channel Consistency

- **Design API-first contracts.** Version DTOs and error shapes explicitly so any frontend can integrate
  without UI-specific behavior. Agents SHOULD document versioning decisions.
- **Treat UI as a pure view over state.** Assume alternate frontends will reuse the same contracts; components
  MUST render state and raise intent without mutating domain rules.
- **Remain swap-friendly.** Keep contracts and state transitions channel-agnostic so replacing Blazor with
  another frontend preserves behavior. Tight coupling SHOULD NOT be introduced.

## WebAssembly Readiness

- **Keep components WASM-friendly.** Use async APIs, `HttpClient`, and `IJSRuntime` abstractions that run in
  both WebAssembly and server-hosted scenarios. Server-only dependencies MUST NOT be introduced into shared components.
- **Design for trimming and AOT.** Minimize reflection-heavy patterns and ensure DI registrations support
  trimming to keep payloads small.

## Authoring Rules by Layer

### Atoms

- Expose only the parameters consumers truly need. All `[Parameter]` members MUST be PascalCase and,
  when required, annotated with `[EditorRequired]`.
- Atoms SHOULD forward any unrecognized attributes via `AdditionalAttributes`. This keeps atoms flexible when
  design tokens evolve.
- Handle events through `EventCallback` to support async pipelines.
- Keep the component visually neutral; theme variants belong in CSS classes or parameters.

```razor
<button class="btn btn-primary" @onclick="HandleClickAsync" @attributes="AdditionalAttributes">
    @Text
</button>

@code {
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private Task HandleClickAsync(MouseEventArgs args) => OnClick.InvokeAsync(args);
}
```

### Molecules

- Compose atoms using child content or `RenderFragment` slots instead of hard-coding atoms
  internally. Hard-coded atoms SHOULD be refactored.
- Push validation logic into dedicated services or `EditContext` helpers; molecules SHOULD only
  orchestrate the visual relationship.
- Use cascading parameters sparingly; prefer explicit parameters to keep molecules portable.

```razor
<div class="form-field">
    <label class="form-label" for="@Id">@Label</label>
    <InputText id="@Id" class="form-input" @bind-Value="Value" />
    <ValidationMessage For="ValueExpression" />
</div>

@code {
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string? Value { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<string?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }
}
```

### Organisms

- Encapsulate feature logic; organisms know about domain models but not data access. Direct data access MUST NOT occur.
- Emit meaningful events via strongly typed callbacks (`EventCallback<OrderFilter>`). Avoid
  exposing raw UI state when a value object conveys intent better; leaking raw state SHOULD be treated as a refactoring task.
- Inject services via `[Inject]` **only** in the partial class; keep the Razor markup focused on
  structure. Injection inside markup MUST NOT be done.
- Prefer `RenderFragment` slots for optional regions (for example, action bars) to avoid branching
  on null parameters.

```razor
<div class="order-filter">
    <LabeledInput Label="Status" Id="status" @bind-Value="Criteria.Status" />
    <PrimaryButton Text="Apply" OnClick="HandleApplyAsync" />
</div>
```

```csharp
namespace Company.Orders.Client.Components.Organisms;

public sealed partial class OrderFilter : ComponentBase
{
    [Parameter, EditorRequired]
    public OrderFilterCriteria Criteria { get; set; } = new();

    [Parameter]
    public EventCallback<OrderFilterCriteria> OnApply { get; set; }

    private Task HandleApplyAsync(MouseEventArgs args) => OnApply.InvokeAsync(Criteria);
}
```

### Templates

- Treat templates as layout scaffolds. Accept `RenderFragment` slots for header, body, footer, and
  sidebars. Templates MUST NOT fetch data.
- Templates are agnostic to view models; they only coordinate placement.
- Ensure templates can host mocked fragments so designers can preview compositions without real
  data.

### Pages

- Pages translate routing and data access into component state. They may call services, dispatch
  actions, or orchestrate application-level concerns.
- Pages SHOULD use `@implements IAsyncDisposable` when they create long-lived resources.
- Keep page markup thin by delegating to organisms. Pages SHOULD rarely introduce new layout
  elements that templates could host.

## Handling Design Debt and Emerging Systems

- **Start with atoms.** When a pattern repeats twice, extract an atom first before creating molecules.
- **Version components through parameters, not duplication.** Introduce nullable parameters or
  new `RenderFragment` slots; keep the default behavior backward compatible. Component duplication SHOULD be avoided.
- **Document open questions** in component XML comments and optional `README.md` files inside the
  component folder. This supports iterative design where the system is still forming.
- **Create TODO-focused scratchpad tasks** when a component needs refactoring into lower layers;
  keep the live component functioning while the design system matures.

## Styling, Naming, and Theming

- Co-locate styles in `Component.razor.css`. Use CSS variables for tokens (`var(--color-primary)`),
  enabling consumers to swap themes without editing components. Global overrides SHOULD NOT be required.
- Follow BEM selectors (`Block__Element--Modifier`) even with CSS isolation to keep recomposition safe and
  predictable. Deviations MUST be justified in component docs.
- Atoms MUST NOT rely on global styles. If a shared utility class is necessary, document the
  dependency in the component README.
- Scope animations and transitions carefully to avoid impacting parent layouts. Prefer
  `prefers-reduced-motion` media queries for accessibility.

## Accessibility and Localization

- Meet WCAG 2.2 AA standards by default. Bake semantics, focus order, and keyboard paths into base
  components to avoid regressions down the stack.
- All interactive atoms MUST be keyboard accessible and expose ARIA attributes when semantics
  require it.
- Surface consistent error messaging patterns in molecules and organisms to ensure screen-reader parity.
- Use `@typeparam` to keep components strongly typed for resource keys when localizing content.
- Provide sensible defaults for labels and placeholders but allow callers to override them via
  parameters or `RenderFragment` content.
- Add automated a11y linting or audits in CI for high-traffic components when available. Missing audits SHOULD be tracked.

## Testing Expectations

- Add L0 tests for component logic in the corresponding test project (for example,
  `MyFeature.Components.Tests`). Focus MUST include state transitions and event callbacks.
- Use snapshot or rendering tests (via bUnit) at the molecule and organism levels to prevent
  regression.
- Write unit tests for reducers, selectors, and effects to keep Redux-style state predictable.
- Create contract tests that validate DTO and error shapes stay versioned and backwards compatible.
- Ensure mutation survivors tied to component logic are cleared by rerunning
  `summarize-mutation-survivors.ps1`, which regenerates the scratchpad tasks automatically;
  close the regenerated tasks before shipping.

## Review Checklist

- [ ] Component lives in the correct atomic folder and follows naming conventions.
- [ ] Markup and logic are split (`.razor` + `.razor.cs`); components stay view-only while pages handle
  orchestration.
- [ ] Parameters are explicit, documented, and use value objects when meaningful.
- [ ] Rendering logic delegates to lower layers instead of duplicating markup.
- [ ] Styles use CSS isolation with BEM selectors; tokens and theming are locally defined.
- [ ] Accessibility hooks (roles, labels, keyboard support, focus order) meet WCAG 2.2 expectations.
- [ ] State updates flow through the shared store; effects depend on DI abstractions.
- [ ] Tests cover interactions, reducers/effects, and contract compatibility; mutation score is stable or
  improving.
- [ ] Outstanding design debts or follow-ups are captured as scratchpad tasks when applicable.

---
Last verified: 2025-11-09
Default branch: main
