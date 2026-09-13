---
applyTo: '**/*.css,**/*.razor*,src/Refraction.Client/**,src/Refraction.Abstractions/**/Theme/**,**/Themes/**/*.json,**/*.tokens.json'
---

# CSS and Design Token Authoring

Governing thought: Refraction styling uses explicit ownership, isolated components, semantic tokens, and native CSS so visual changes remain composable and reviewable.

> Drift check: Keep this standard aligned with Refraction source, generated output, the compiled Blazor bundle, and the repository's canonical validation scripts; those artifacts are authoritative.

## Rules (RFC 2119)

- Agents editing CSS, Razor class names, visual state, variants, token sources, themes, or new Refraction components **MUST** follow this standard. Why: One policy prevents styling rules from diverging across code and agent guidance.
- Atomic Design levels **MUST** remain filesystem and composition concerns. Why: Moving a component between Atoms, Molecules, Organisms, Templates, or Pages should not rename its styling API.
- Atomic Design level names **MUST NOT** appear in CSS class names. Why: Class names describe ownership and behavior rather than a component's current composition level.
- Refraction-owned classes and custom properties **MUST** use the `rf` namespace. Why: Namespace ownership prevents application CSS from colliding with Refraction APIs.
- Application and sample CSS **MUST NOT** invent `rf-*` classes or `--rf-*` properties. Why: Consumers need a clear boundary between their styles and the design system.
- Docusaurus CSS Modules and unrelated third-party styles **MUST** remain under their own conventions and be excluded from Refraction-specific naming and token rules. Why: External styling systems have separate ownership and build constraints.
- Refraction-owned component blocks **MUST** use lowercase kebab-case `rf-c-{block}` names. Why: The component namespace gives each public root a stable, predictable owner.
- Refraction-owned component elements **MUST** use flat `rf-c-{block}__{element}` names. Why: Flat elements keep private structure refactorable and avoid nested selector APIs.
- Authors **SHOULD** add an element class only when styling ownership requires it. Why: Naming every DOM node exposes unnecessary private structure.
- Refraction-owned reusable layout classes **MUST** use lowercase kebab-case `rf-l-{name}` names. Why: Layout primitives are intentionally separate from component ownership and remain mechanically recognizable.
- Refraction-owned deliberate single-purpose utilities **MUST** use lowercase kebab-case `rf-u-{name}` names. Why: Narrow utilities remain useful without turning Refraction into a utility-first framework and remain mechanically recognizable.
- Refraction layout classes **MUST NOT** use the `o-` prefix. Why: `Organism` already identifies a filesystem composition level in this repository.
- Generic state classes such as `.is-active` and `.has-error` **MUST NOT** be introduced. Why: Anonymous state classes make semantic ownership and accessibility harder to inspect.
- State styling **SHOULD** prefer native pseudo-classes, truthful ARIA attributes, explicit `data-*` attributes, and BEM modifiers in that order. Why: The selector should expose the most meaningful available state.
- Refraction component modifier syntax **MUST** use only `rf-c-{block}--{modifier}` or `rf-c-{block}__{element}--{modifier}` with lowercase kebab-case segments. Why: A small grammar keeps component state explicit without expanding utility APIs.
- Every modifier **MUST** accompany its corresponding base block or element class. Why: A modifier without its owner is ambiguous and difficult to compose.
- Layout and utility classes **MUST NOT** define BEM modifiers. Why: Layout and utility contracts stay narrow and predictable.
- BEM modifiers **SHOULD** be used only when native, ARIA, and `data-*` state mechanisms are unsuitable, with the reason documented. Why: Modifiers remain a deliberate fallback rather than a generic state channel.
- BEM modifier visibility **MUST** default to private. Why: Private selectors remain implementation details rather than theming or customization APIs.
- Deliberate public block modifier contracts **MUST** be explicitly documented. Why: Private element modifiers remain internal under the existing BEM API boundary.
- Typed `State`, `Variant`, `Size`, and `Tone` values **SHOULD** bind to explicit `data-*` attributes when CSS needs them. Why: Stable data bindings avoid generated class permutations and preserve typed component state.
- CSS state **MUST** agree with the component's semantic and accessibility state. Why: Visual feedback must not contradict what assistive technology receives.
- Reusable components **MUST** keep their `.razor`, `.razor.cs`, and `.razor.css` files colocated in one component folder, with tests in the matching component area. Why: Vertical ownership keeps markup, behavior, and presentation reviewable together.
- Component-specific rules **MUST** live in the colocated `.razor.css` file. Why: Blazor isolation keeps private presentation with its component.
- Global CSS **MUST** be limited to generated tokens, themes, reset or normalization, required base typography, layout primitives, utilities, and global accessibility helpers. Why: A small global surface reduces accidental coupling.
- `::deep` **SHOULD** be avoided. Why: Deep selectors bypass ordinary component ownership and make composition fragile.
- Every unavoidable `::deep` selector **MUST** document its reason, target boundary, and removal or review condition. Why: Exceptions need an auditable ownership decision.
- A parent **MUST NOT** target another Refraction component's private element selectors. Why: Parent-child composition should use public roots, parameters, variants, or tokens.
- A component **MUST** own its internal layout while its parent owns external placement. Why: Components remain composable without hidden margins or page-specific positioning contracts.
- Component rules **MUST NOT** prescribe external placement through incidental margins or page-specific offsets. Why: Placement belongs to the parent layout context.
- The supported token API for theming and customization **MUST** consist of semantic system tokens and deliberately exposed component tokens. Why: Consumers can theme stable concepts without coupling to DOM details.
- Consumers **MUST NOT** depend on private BEM element selectors. Why: Internal elements may evolve without breaking supported customization.
- Canonical token sources **MUST** be valid DTCG 2025.10 representations. Why: A standard source format enables validation and tooling without losing token meaning.
- The supported DTCG 2025.10 subset **MUST** be documented in the [initial DTCG input profile](#initial-dtcg-input-profile). Why: Contributors need to know which standard constructs the repository intentionally accepts.
- Token validation **MUST** reject constructs listed as unsupported in the [initial DTCG input profile](#initial-dtcg-input-profile) instead of silently reinterpreting them. Why: Silent reinterpretation can change token meaning during generation.
- Canonical token JSON **SHOULD** live under `src/Refraction.Client/Themes/Tokens/` in reference, system, and optional component catalogs. Why: A predictable source location keeps token ownership discoverable.
- DTCG source paths **MUST** begin with `ref.*`, `sys.*`, or optional `comp.*`. Why: The canonical JSON uses semantic source namespaces rather than CSS output names.
- Generated token custom properties **MUST** replace source dots with hyphens and prefix the source layer with `--rf-`, producing `--rf-ref-*`, `--rf-sys-*`, or deliberately exposed `--rf-comp-*`. Why: The output namespace preserves source ownership while remaining valid CSS.
- Hand-authored generated CSS **MUST NOT** be the token source of truth. Why: Source edits need to survive regeneration without being overwritten or silently diverging.
- Generated token output **MUST** identify itself as generated. Why: Reviewers and tools must distinguish source from derived artifacts.
- Token generation **MUST** be deterministic for identical inputs. Why: Reproducible output makes changes reviewable and stale output detectable.
- Repository validation **MUST** fail when generated token output is stale. Why: A source change without regenerated output would otherwise ship inconsistent themes.
- Reference tokens **MUST** describe values rather than UI purpose. Why: Semantic meaning belongs in the system layer.
- Component CSS **MUST NOT** consume `--rf-ref-*` properties directly. Why: The system layer is the boundary between values and component meaning.
- Reference scales **MUST** use an ordered direction such as `100`, `200`, and `300`, with that direction documented. Why: Names such as `n1` and `n2` do not communicate whether values become lighter, darker, larger, or smaller.
- System tokens **MUST** describe semantic roles such as surface, text, action, status, border, focus, space, type, radius, motion, and layering. Why: Components should ask for meaning rather than palette implementation details.
- System tokens **SHOULD** reference reference tokens. Why: Theme semantics remain separated from raw values and can be rebound coherently.
- Component CSS **SHOULD** consume system tokens by default. Why: Semantic defaults keep components theme-agnostic and consistent.
- Component tokens **MUST** be added only for a stable public concept, intentional divergence, or a real per-component theming need. Why: Mechanical one-token-per-declaration APIs create noise and freeze implementation details.
- Component tokens **SHOULD** default to system tokens. Why: Consumers receive coherent defaults while retaining an intentional customization point.
- Reusable design decisions **SHOULD** use tokens for color, spacing, typography, radius, borders, shadows, opacity, motion, easing, focus, reusable sizes, and layering. Why: Repeated meaningful decisions need one semantic place to change.
- Authors **MUST** treat repeated meaningful design literals as evidence of a missing token. Why: Duplication causes visual drift even when every individual declaration looks reasonable.
- Structural values such as `0`, `100%`, `50%`, `auto`, `none`, `1fr`, and `currentColor` **MAY** remain literal. Why: Not every CSS value represents a reusable design decision.
- New work **MUST NOT** introduce additional `--rf-raw-*` variables. Why: The reference/system/component hierarchy replaces ambiguous raw aliases in this pre-release repository.
- Existing `--rf-raw-*` variables **MUST** remain available until all of their consumers are migrated. Why: Removing a variable before its consumers move would make an intermediate stack layer invalid.
- The layer that removes existing `--rf-raw-*` variables **MUST** be recorded as the removal layer in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405). Why: Explicit removal ownership prevents a legacy file from being deleted prematurely or forgotten.
- Final migration completion **MUST** remove every `--rf-raw-*` token and obsolete styling API. Why: The completed architecture cannot retain legacy selectors or token contracts.
- Themes **SHOULD** rebind system tokens at a theme root. Why: Light, dark, high-contrast, and enterprise themes can change meaning without duplicating component selectors.
- Component CSS **SHOULD** remain theme-agnostic. Why: Theme behavior belongs in token values and semantic mappings.
- Theme definitions **MUST NOT** duplicate component selector rules solely to change theme values. Why: Semantic rebinding avoids a parallel theme-specific selector API.
- Interactive controls **MUST** retain a meaningful `:focus-visible` indication using shared focus tokens. Why: Keyboard users need a visible, consistent focus target.
- Non-essential motion **MUST** honor `@media (prefers-reduced-motion: reduce)`. Why: Motion preferences are an accessibility requirement.
- Refraction styles **MUST** account for forced-colors or high-contrast environments where their visual decisions affect meaning or control affordance. Why: System color modes can replace authored colors and expose contrast or state failures.
- Responsive component layout **SHOULD** prefer intrinsic flexbox, grid, `gap`, `min()`, `max()`, `clamp()`, and `minmax()` behavior. Why: Components adapt to their available space instead of accumulating breakpoint exceptions.
- Container-aware behavior **SHOULD** be used when available space is the meaningful constraint. Why: Component responsiveness often depends on its container rather than the viewport.
- Viewport queries **SHOULD** be reserved primarily for page or template composition. Why: Page composition owns viewport decisions while components own intrinsic layout.
- Logical properties such as `margin-inline`, `padding-block`, and `border-inline-start` **SHOULD** be preferred where practical. Why: Logical layout supports writing modes and bidirectional interfaces.
- Selectors **MUST** keep specificity low and predictable by avoiding IDs, long descendant chains, needless qualification, and duplicated specificity. Why: Low specificity makes isolation, composition, and overrides easier to reason about.
- `!important` **MUST** be limited to an exceptional, documented interoperability or accessibility case. Why: Unbounded importance defeats the cascade and hides ownership errors.
- Global cascade layers **MAY** be adopted only after compiled-bundle evidence shows they interact usefully with Blazor CSS isolation. Why: Native layers are valuable only when their ordering remains deterministic in the emitted application.
- A cascade-layer decision **MUST** record the tested bundle behavior and the reason for adoption or deferral. Why: The decision should be revisitable when the build pipeline changes.
- Refraction styles **MUST** use native CSS custom properties and platform features unless repository evidence requires another mechanism. Why: Readable native CSS keeps browser debugging and generated output straightforward.
- Sass and Less **MUST NOT** be introduced merely for variables, nesting, naming, or token generation. Why: Custom properties and build-time generation cover those needs without a new styling toolchain.
- CSS and token validation **MUST** detect invalid Refraction names, raw tokens, direct reference-token consumption, hard-coded design colors, unjustified `::deep`, IDs, exceptional `!important`, invalid token names, duplicate tokens, flattened output-name collisions, unresolved aliases, and stale generated output. Why: The architecture needs mechanical regression detection in addition to prose.
- Validation **SHOULD** use existing PowerShell and .NET infrastructure, adding Stylelint only when it materially improves coverage without an unrelated toolchain. Why: Enforcement should fit the repository's build model.
- New or changed CSS, Razor visual state, themes, token sources, and Refraction components **MUST** follow this standard immediately. Why: New drift is more expensive than a later migration.
- Existing legacy styles **MUST** be migrated in later independent, valid stack layers rather than hidden by undocumented compatibility rules. Why: Each migration layer remains reviewable while the target architecture stays clear.
- Temporary migration exclusions **MUST** name their scope, reason, owner, and removal layer in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405). Why: Explicit inventory prevents temporary exceptions from becoming permanent APIs.
- Compatibility aliases **MUST NOT** be permanent. Why: Pre-release freedom permits cleanup instead of preserving historical conventions.
- Final migration completion **MUST** remove all temporary migration exclusions and compatibility aliases. Why: The final layer must leave one enforceable standard without transitional escape hatches.
- Every other exception **MUST** have a documented technical reason and review condition. Why: Unexplained exceptions become accidental architecture.
- Substantial CSS and design-system work **MUST** be planned as small `gh-stack` layers. Why: Independent outcomes make architectural changes easier to review and recover.
- Each stack layer **MUST** be independently valid and pass its applicable build, test, token, CSS, and review advancement gate before dependent work starts. Why: A later layer cannot be the hidden prerequisite for an earlier one.
- Substantial CSS and design-system planning **MUST** retain the configured high-reasoning orchestrator. Why: Architecture decisions benefit from deliberate analysis before implementation is delegated.
- Implementation coding **MUST** use the installed GPT-5.6 Luna identifier (`gpt-5.6-luna`) at maximum supported reasoning. Why: Repetitive implementation work follows the approved model strategy.
- Implementation-worker concurrency **MUST NOT** exceed eight per session. Why: Eight is the policy maximum rather than a requirement to manufacture parallel work.
- The effective host concurrency ceiling **MAY** be lower than eight. Why: Host capacity remains a binding operational limit.
- When repository worker defaults are configured, they **MUST** remain centralized in one verified repository Codex configuration. Why: One authority prevents conflicting per-agent settings.
- Any configured worker defaults **MUST** be verified against the installed configuration contract before use or change. Why: Unsupported keys or stale assumptions can silently leave effective behavior unchanged.
- Agent files **SHOULD** link this standard for CSS model routing rather than restating operational settings. Why: A configured repository source remains authoritative; otherwise the selected runtime assignment provides the evidence.
- Each layer **MUST** inspect the relevant selectors, token references, state bindings, generated output, and rendered behavior before handoff. Why: Search, build evidence, and visual evidence catch different classes of styling regression.

## Scope and audience

This standard covers Refraction and consumer-facing styling work in CSS, Razor markup and code-behind, token JSON, and theme definitions. Refraction-specific naming and token ownership rules apply to Refraction-owned selectors; application-owned components keep their own namespace while following the general isolation and accessibility guidance. The Blazor guidance in [blazor-ux-guidelines.instructions.md](blazor-ux-guidelines.instructions.md) owns component behavior and delegates these styling concerns here.

## Architecture map

The naming relationship is:

```text
Atomic Design folders and composition
        |
        +-- rf-c-{block} with flat __elements
        +-- rf-l-{name} layout primitives
        +-- rf-u-{name} narrow utilities

ref.* values -> sys.* semantic roles -> optional comp.* public hooks -> component CSS
```

Private element selectors describe internal structure. The supported token API for theming and customization consists of semantic system tokens and deliberately exposed component tokens; deliberate root, layout, and utility class contracts are documented separately.

## Token naming map

The source/output distinction is illustrated by these mappings:

| DTCG source path | CSS custom property |
| --- | --- |
| `ref.color.neo-blue.300` | `--rf-ref-color-neo-blue-300` |
| `sys.color.action.primary` | `--rf-sys-color-action-primary` |
| `comp.pane.accent-border` | `--rf-comp-pane-accent-border` |

## Initial DTCG input profile

This selected DTCG 2025.10 input profile defines the accepted source shape and value types:

| Area | Profile |
| --- | --- |
| Documents | JSON groups may carry `$type`; nested groups inherit it and token-local `$type` overrides it. Tokens carry `$value`, with optional string `$description`. |
| Paths | Source paths begin `ref.*`, `sys.*`, or `comp.*`; segments use lowercase kebab-case, with numeric segments for ordered scales. |
| `color` | An sRGB object (`colorSpace: "srgb"`) with exactly three finite components in 0..1 and optional alpha in 0..1. CSS strings are not color values. |
| `dimension` | An object with a finite value and unit `px` or `rem`. CSS strings are not dimension values. |
| `duration` | An object with a finite non-negative value and unit `ms` or `s`. CSS strings are not duration values. |
| `number` / `fontFamily` | A finite numeric value; a non-empty name or a non-empty array of names. |
| `fontWeight` | A numeric value from 1 through 1000 or a lowercase alias defined by DTCG 2025.10. |
| `cubicBezier` | Four finite numbers; x coordinates are 0..1 and y coordinates are unrestricted finite values. |
| Aliases | A whole-token curly-brace alias such as `{ref.color.neo-blue.300}` may chain through targets. Type resolution uses explicit, inherited-group, and target-token types; cycles, unresolved targets, and type mismatches are errors. |
| Catalog scope | Documents in one catalog scope merge distinct source paths. Duplicate JSON properties, duplicate paths, and flattened CSS-name collisions are errors. |
| Rejected / conformance | Composite types, property-level references or JSON Pointer, `$root`, `$extends`, extensions, deprecation metadata, and unknown constructs; this is selected input support, not full-format DTCG tool conformance. |

## Illustrative future target

The following example uses the existing Pane shape but shows the post-migration class names. It describes a target for a later vertical migration; it is not a claim that the current checkout already exposes these selectors or token names.

### Razor

```razor
<section class="rf-c-pane"
         data-variant="@Variant"
         data-state="@State">
    <header class="rf-c-pane__header">
        <h2 class="rf-c-pane__title">@Title</h2>
    </header>

    <div class="rf-c-pane__content">
        @ChildContent
    </div>
</section>
```

### CSS

```css
.rf-c-pane {
    display: flex;
    flex-direction: column;
    background: var(--rf-sys-color-surface-elevated);
    border:
        var(--rf-sys-border-width-hairline)
        solid
        var(--rf-sys-color-border-muted);
    border-radius: var(--rf-sys-radius-sm);
}

.rf-c-pane__header {
    padding:
        var(--rf-sys-space-sm)
        var(--rf-sys-space-md);
}

.rf-c-pane[data-variant="accent"] {
    border-color: var(--rf-comp-pane-accent-border, var(--rf-sys-color-action-primary));
}
```

### Token relationship

This relationship distinguishes a value, a semantic role, an optional public component hook, and the selector that consumes it:
In this future illustrative target, a missing component hook falls back to the current scope's semantic system token.

```text
ref.color.neo-blue.300
        |
        v
sys.color.action.primary
        |
        v
comp.pane.accent-border   (only when a stable public hook is justified)
        |
        v
.rf-c-pane
```

## Status and references

This layer defines the forward-looking authoring contract. Token catalogs, generators, validators, compiled-bundle evidence, and complete consumer documentation belong to subsequent stack layers.

- [Blazor UX Guidelines](blazor-ux-guidelines.instructions.md)
- [Namespace and Folder Placement](namespace-folder-placement.instructions.md)
- [PR Size and Stacked Delivery](pr-size-and-stacking.instructions.md)
- [CSS migration issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405)
- [Worker configuration follow-up issue #675](https://github.com/Gibbs-Morris/mississippi/issues/675)
- [Official gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md)
- [DTCG format](https://www.designtokens.org/tr/2025.10/format/)
