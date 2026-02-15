---
applyTo: '**/*.razor*'
---

# Blazor UX Guidelines

Governing thought: Build atomic, testable Blazor components with split markup/logic, Redux-style state, accessibility by default, and WebAssembly-friendly dependencies.

> Drift check: Check DI/logging/test settings and design system assets before editing components; repository scripts/configs stay authoritative.

## Rules (RFC 2119)

- Agents **MUST** follow this guide when authoring/reviewing Razor components. Why: Keeps UX consistent.
- Components **MUST** mirror atomic layers (Atoms/Molecules/Organisms/Templates/Pages) with one component per folder containing `.razor`, `.razor.cs`, styles, and tests. Why: Predictable structure.
- Markup and logic **MUST** be split (`.razor` + `partial` `.razor.cs`, `sealed` unless extensibility is required). Why: Focused diffs and testability.
- View-only components **MUST** stay presentational, exposing `[Parameter]` + `EventCallback`; child components **MUST NOT** call APIs or manage side effects; domain logic **MUST** live outside the UI. Why: Separation of concerns.
- Redux-style state (actions/reducers/selectors/effects) **SHOULD** be used; selectors **MUST** feed components instead of raw store state; effects **MUST** call interfaces for IO. Why: Predictable updates and testability.
- Templates **MUST NOT** fetch data; injection inside Razor markup **MUST NOT** be used (inject in partial class); server-only dependencies **MUST NOT** appear in shared components to keep WASM compatibility. Why: Portability and clarity.
- Controlled `<input>`/`<textarea>`/`<select>` state **MUST** use `@bind:get` + `@bind:set` (and `@bind:event` when needed) instead of `value`/`checked` with manual `@oninput`/`@onchange` syncing. Why: Prevents UI/model desynchronization.
- `[Parameter]` members **MUST** be PascalCase; atoms **MUST NOT** rely on global styles; organisms **MUST NOT** access data stores directly. Why: Consistent APIs and layering.
- Interactive atoms **MUST** be keyboard accessible with required ARIA metadata; components **MUST** include L0 tests for state transitions/callbacks. Why: Accessibility and regression safety.
- Atoms **SHOULD** forward `AdditionalAttributes`; duplicated markup **SHOULD** be refactored into slots/parameters; pages **SHOULD** implement `IAsyncDisposable` when holding resources. Why: Reuse and cleanup.
- Global overrides **SHOULD NOT** be required for theming; missing accessibility audits **SHOULD** be tracked. Why: Portable styling and visibility of gaps.
- Contributors **MUST** prefer C#, Razor, and CSS approaches over JavaScript; JS interop is a fallback, not a first choice (house rule). When no practical C#/CSS alternative exists, DOM-manipulation behaviours **MAY** use `IJSRuntime`/`ElementReference`[^5][^6] and **MUST** follow the JS Interop Isolation rules below. Why: C#-first keeps logic testable, SSR-compatible, and avoids WASM/Server divergence.
- Components **MUST** validate parameter combinations in `OnParametersSet`; in Debug builds, invalid combinations (e.g. `Href` on a submit button) **SHOULD** throw `InvalidOperationException` with a descriptive message; in Release builds, the component **SHOULD** normalise or ignore the invalid value and log a warning via LoggerExtensions. Why: Fail-fast in development catches misuse early; graceful handling in production avoids user-facing crashes.

### Single Component vs Split Components (Variant Rules)

- Contributors **MUST** default to ONE component with a `Variant` enum when variants differ primarily by visual styling and share the same semantic HTML element and behavioural contract (e.g. filled/outlined/text buttons all render `<button>`). Why: Aligns with industry practice—MudBlazor (`Variant.Filled`/`Outlined`/`Text`)[^1], Fluent UI Blazor (`Appearance.Accent`/`Neutral`/`Stealth`)[^2], and Ant Design Blazor (`ButtonType.Primary`/`Default`/`Dashed`)[^3] each expose one button component with a variant enum.
- Contributors **MUST** introduce a separate component or thin wrapper when any *hard split trigger* applies: (a) the variant requires a different semantic element or fundamentally different behaviour—action buttons (`<button type="submit|reset|button">`), navigation links (`<a href>` with `target`/`rel`), and router navigation (`<NavLink>` with active-state CSS) are three distinct contracts that **MUST NOT** share a single component; (b) the variant has a distinct interaction state machine (toggle, split-button, menu-button, file-upload); (c) variant-specific accessibility requirements cannot be enforced by the shared component alone (e.g. an icon-only variant that **MUST** require `AriaLabel`); (d) three or more parameters apply only to a single variant, or documentation contains repeated "only valid when `Variant == X`" caveats—this signals parameter soup; (e) performance-at-scale concerns arise because `CaptureUnmatchedValues` splatting is expensive in grids or large lists. Why: Blazor has no user-defined directive attributes[^4] so behavioural differences cannot be enforced at compile time; forcing divergent behaviour into one component creates invalid-state surfaces.
- Thin wrappers that lock the `Variant` value and expose only variant-relevant parameters **SHOULD** be preferred over fully duplicating a component when a split is triggered. Why: Wrappers prevent invalid combinations while reusing the shared implementation (e.g. `MisIconButton` locks icon-only mode and requires `AriaLabel`; `MisToggleButton` adds `IsToggled`/`OnToggled`). Ant Design Blazor follows an analogous pattern with `DownloadButton` wrapping `Button`[^3].
- Options objects (e.g. view-model records) **MAY** group variant-specific parameters and **SHOULD** be used when a variant adds ≥2 logically related parameters that do not apply to other variants. Why: Keeps the parameter list flat for the common case while offering opt-in richness.
- `[Flags]` enum variants **MAY** be used only when visual traits are genuinely combinable (e.g. `Danger | Outlined`); contributors **MUST NOT** use flags when combinations are semantically invalid or untested. Why: Flags that produce untested combinations are worse than separate enums.
- `CaptureUnmatchedValues` splatting **SHOULD** be used on leaf atoms but **MUST NOT** be added to components rendered at scale (list items, grid cells, table rows) unless profiling confirms acceptable overhead. Why: The Blazor renderer must match all supplied parameters against known parameters and track attribute overwrites, which Microsoft's performance guidance explicitly calls "expensive"[^7].

### JS Interop Isolation

- JS interop code **MUST** be isolated behind an `internal` interface or service; components **MUST NOT** call `IJSRuntime` directly from Razor markup or code-behind. Why: Enables unit testing with mocks and keeps the JS surface auditable.
- Every JS interop call site **MUST** document *why* a C#/CSS alternative is insufficient (inline comment or XML doc). Why: Prevents JS creep and makes the rationale reviewable.
- JS modules **MUST** be co-located with their component (same folder) and **MUST** handle unavailability gracefully (e.g. `try`/`catch` with fallback or no-op when JS is not yet initialised during prerendering). Why: SSR/prerender scenarios and Blazor Server reconnect can leave JS unavailable.
- Components using JS interop **MUST** include L0 tests that verify behaviour both when JS succeeds and when JS is unavailable. Why: WASM and Server hosting models behave differently during initialisation; tests must cover both paths.

## Scope and Audience

Developers authoring or reviewing Blazor components/pages.

## At-a-Glance Quick-Start

- Place components under a single root with atomic folders; one component per folder.
- Keep logic in `.razor.cs` partial class; inject services there, not in markup.
- Use Redux-style state + selectors; send intent via callbacks, not direct API calls.
- Ensure accessibility (keyboard/ARIA), isolated styles, and L0 tests.
- Prefer C#/Razor/CSS over JS; use JS interop only when no C#/CSS alternative exists, behind an interface.
- Default to ONE component + `Variant` enum for visual-only differences; split or wrap when the semantic element, behaviour, accessibility contract, or parameter shape diverges.
- Validate parameter combinations in `OnParametersSet`; throw in Debug, normalise and log in Release.

## Core Principles

- Atomic design supports reuse and consistent composition.
- Separation of markup/logic/state keeps components portable and testable.
- Accessibility and WASM readiness are defaults, not afterthoughts.
- C#-first: prefer C#/Razor/CSS over JavaScript; JS is a documented fallback.
- One-component-with-variants is the default; split only at clear semantic/behavioural boundaries.

## Variant Decision Checklist (PR Reviewers)

Before approving a new or modified component, verify each item:

| # | Check | Pass Criteria |
|---|-------|---------------|
| 1 | **Semantics vs styling** | Variants that change only CSS class/token use ONE component; variants needing a different HTML element or behaviour have their own component or wrapper. |
| 2 | **Variant-only parameters** | No parameter is documented as "only valid when `Variant == X`". If such parameters exist, extract to a wrapper or options object. |
| 3 | **Icon-only accessibility** | Any icon-only variant uses a wrapper (e.g. `MisIconButton`) with `[EditorRequired] AriaLabel`; the base component alone does not render icon-only without an accessible name. |
| 4 | **JS interop rationale** | Every `IJSRuntime` call has a documented reason why C#/CSS is insufficient; JS is behind an interface; tests cover JS-available and JS-unavailable paths. |
| 5 | **Render-at-scale** | Components used in lists/grids/virtualised containers do NOT use `CaptureUnmatchedValues`; explicit parameters are preferred. |
| 6 | **Split-trigger review** | Action (`<button>`), navigation (`<a>`), and router navigation (`<NavLink>`) are never combined into one component; each has its own component or wrapper. |

### Examples

**Good — single component with variant:**

```razor
@* Visual-only difference: filled vs outlined vs text *@
<MisButton Model="@(new MisButtonViewModel { Variant = MisButtonVariant.Filled })">
    Save
</MisButton>

<MisButton Model="@(new MisButtonViewModel { Variant = MisButtonVariant.Outlined })">
    Cancel
</MisButton>
```

**Good — icon-only wrapper enforcing accessibility:**

```razor
@* Icon-only buttons MUST have an AriaLabel; the wrapper enforces this at compile time. *@
<MisIconButton AriaLabel="Close dialogue"
               Icon="MisIcons.Close"
               OnAction="HandleClose" />

@* Wrapper implementation (simplified): *@
@inherits MisButton
@code {
    [Parameter, EditorRequired] public string AriaLabel { get; set; } = default!;
    [Parameter, EditorRequired] public string Icon { get; set; } = default!;
}
```

**Good — separate component for navigation (different semantic element):**

```razor
@* Navigation renders <a>, not <button>; supports Href, Target, and rel. *@
<MisLinkButton Href="/settings" Target="_self">Settings</MisLinkButton>

@* Router-aware variant highlights active route via NavLink: *@
<MisNavLinkButton Href="/dashboard" ActiveCssClass="mis-active">Dashboard</MisNavLinkButton>
```

**Anti-example — parameter soup in a single component:**

```razor
@* BAD: Href only applies when Variant == Link; ToggleState only when Variant == Toggle;
   ActiveCssClass only when Variant == NavLink. This is parameter soup. *@
<MisButton Variant="MisButtonVariant.Link"
           Href="/settings"
           ToggleState="false"
           ActiveCssClass="mis-active">
    Settings
</MisButton>
```

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Testing: `.github/instructions/testing.instructions.md`
- Logging: `.github/instructions/logging-rules.instructions.md`

[^1]: MudBlazor `<MudButton>` — single component with `Variant` enum (`Filled`/`Outlined`/`Text`): <https://mudblazor.com/components/button>
[^2]: Fluent UI Blazor `<FluentButton>` — single component with `Appearance` enum (`Accent`/`Neutral`/`Lightweight`/`Outline`/`Stealth`): <https://www.fluentui-blazor.net/Button>
[^3]: Ant Design Blazor `<Button>` — single component with `ButtonType` enum (`Primary`/`Default`/`Dashed`/`Link`/`Text`), separate `DownloadButton` wrapper: <https://antblazor.com/en-US/components/button>
[^4]: Blazor directive attributes (`@bind`, `@onclick`, `@ref`, `@key`, `@rendermode`, `@attributes`) are built-in; no user-defined directive attribute mechanism exists: <https://learn.microsoft.com/aspnet/core/blazor/components/?view=aspnetcore-10.0#component-classes> and <https://learn.microsoft.com/dotnet/architecture/blazor-for-web-forms-developers/components#an-introduction-to-razor>
[^5]: JS interop via `IJSRuntime` is the supported path for DOM manipulation: <https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet?view=aspnetcore-10.0>
[^6]: `ElementReference` passed to JS yields an `HTMLElement`; direct DOM access from C# is not supported: <https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet?view=aspnetcore-10.0#capture-references-to-elements>
[^7]: `CaptureUnmatchedValues` is "expensive" per Microsoft's rendering performance guidance: <https://learn.microsoft.com/aspnet/core/blazor/performance/rendering?view=aspnetcore-10.0#avoid-attribute-splatting-with-%60captureunmatchedvalues%60>
