# CSS and Design Token Authoring

Governing thought: Refraction styling uses explicit ownership, isolated components, semantic tokens, and native CSS so visual changes remain composable and reviewable.

> Drift check: Keep this standard aligned with Refraction source, generated output, the compiled Blazor bundle, and the repository's canonical validation scripts; those artifacts are authoritative.

## Rules (RFC 2119)

- Agents editing CSS, Razor class names, visual state, variants, token sources, themes, or new Refraction components **MUST** follow this standard. Why: One policy prevents styling rules from diverging across code and agent guidance.
- Atomic Design levels **MUST** remain filesystem and composition concerns. Why: Moving a component between Atoms, Molecules, Organisms, Templates, or Pages should not rename its styling API.
- Atomic Design level names **MUST NOT** appear in CSS class names. Why: Class names describe ownership and behavior rather than a component's current composition level.
- Refraction-owned classes and custom properties **MUST** use the `rf` namespace. Why: Namespace ownership prevents application CSS from colliding with Refraction APIs.
- Refraction-owned keyframe identifiers **MUST** use `rf-k-{block}--{animation}`, with lowercase kebab-case block and animation names. Why: Blazor selector isolation does not isolate animation identifiers in the compiled stylesheet.
- Refraction component animation references **MUST** use their owning block's keyframe identifier. Why: Explicit ownership prevents one component from replacing another component's animation.
- Application and sample CSS **MUST NOT** invent `rf-*` classes, `rf-k-*` keyframes, or `--rf-*` properties. Why: Consumers need a clear boundary between their styles and the design system.
- Docusaurus CSS Modules and unrelated third-party styles **MUST** remain under their own conventions and be excluded from Refraction-specific naming and token rules. Why: External styling systems have separate ownership and build constraints.
- Refraction-owned component blocks **MUST** use lowercase kebab-case `rf-c-{block}` names. Why: The component namespace gives each public root a stable, predictable owner.
- Each Refraction component block name **MUST** have exactly one component owner across the repository. Why: Atomic folders do not isolate public block names or their global keyframes.
- Refraction-owned component elements **MUST** use flat `rf-c-{block}__{element}` names with lowercase kebab-case element segments. Why: Flat, consistently named elements keep private structure refactorable and avoid nested selector APIs.
- Authors **SHOULD** add an element class only when styling ownership requires it. Why: Naming every DOM node exposes unnecessary private structure.
- Refraction-owned reusable layout classes **MUST** use lowercase kebab-case `rf-l-{name}` names. Why: Layout primitives are intentionally separate from component ownership and remain mechanically recognizable.
- Refraction-owned deliberate single-purpose utilities **MUST** use lowercase kebab-case `rf-u-{name}` names. Why: Narrow utilities remain useful without turning Refraction into a utility-first framework and remain mechanically recognizable.
- Refraction layout classes **MUST NOT** use the `o-` prefix. Why: `Organism` already identifies a filesystem composition level in this repository.
- Generic state classes such as `.is-active` and `.has-error` **MUST NOT** be introduced. Why: Anonymous state classes make semantic ownership and accessibility harder to inspect.
- State styling **SHOULD** prefer native pseudo-classes, truthful ARIA attributes, explicit `data-*` attributes, and BEM modifiers in that order. Why: The selector should expose the most meaningful available state.
- Refraction component modifier syntax **MUST** use only `rf-c-{block}--{modifier}` or `rf-c-{block}__{element}--{modifier}` with lowercase kebab-case segments. Why: A small grammar keeps component state explicit without expanding utility APIs.
- Every modifier **MUST** accompany its corresponding base block or element class. Why: A modifier without its owner is ambiguous and difficult to compose.
- Layout and utility classes **MUST NOT** define BEM modifiers. Why: Layout and utility contracts stay narrow and predictable.
- BEM modifiers **SHOULD** be used only when native, ARIA, and `data-*` state mechanisms are unsuitable. Why: Modifiers remain a deliberate fallback rather than a generic state channel.
- Authors **MUST** document the reason for using a BEM modifier instead of the preferred state mechanisms. Why: Reviewers need an explicit basis for the fallback.
- BEM modifier visibility **MUST** default to private. Why: Private selectors remain implementation details rather than theming or customization APIs.
- Deliberate public block modifier contracts **MUST** be explicitly documented. Why: Private element modifiers remain internal under the existing BEM API boundary.
- Typed `State`, `Variant`, `Size`, and `Tone` values **SHOULD** bind to explicit `data-*` attributes when CSS needs them. Why: Stable data bindings avoid generated class permutations and preserve typed component state.
- CSS state **MUST** agree with the component's semantic and accessibility state. Why: Visual feedback must not contradict what assistive technology receives.
- Reusable components **MUST** keep their `.razor`, `.razor.cs`, and `.razor.css` files colocated in one component folder. Why: Vertical ownership keeps markup, behavior, and presentation reviewable together.
- Component tests **MUST** live in the matching component area of the test project. Why: Consistent test placement keeps behavioral coverage discoverable.
- Component-specific rules **MUST** live in the colocated `.razor.css` file. Why: Blazor isolation keeps private presentation with its component.
- Global CSS **MUST** be limited to generated tokens, themes, reset or normalization, required base typography, layout primitives, utilities, and global accessibility helpers. Why: A small global surface reduces accidental coupling.
- `::deep` **SHOULD** be avoided. Why: Deep selectors bypass ordinary component ownership and make composition fragile.
- Every unavoidable `::deep` selector **MUST** document its reason, target boundary, and removal or review condition. Why: Exceptions need an auditable ownership decision.
- A parent **MUST NOT** target another Refraction component's private element selectors. Why: Parent-child composition should use public roots, parameters, variants, or tokens.
- A component **MUST** own its internal layout while its parent owns external placement. Why: Components remain composable without hidden margins or page-specific positioning contracts.
- Component rules **MUST NOT** prescribe external placement through incidental margins or page-specific offsets. Why: Placement belongs to the parent layout context.
- The supported token API for theming and customization **MUST** consist of semantic system tokens and deliberately exposed component tokens. Why: Consumers can theme stable concepts without coupling to DOM details.
- The legacy `IRefractionTheme` contract **MUST** remain until the `legacy-theme-contract-removal` layer recorded in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405). Why: An existing public contract needs an explicit migration disposition.
- Consumers **MUST NOT** depend on private BEM element selectors. Why: Internal elements may evolve without breaking supported customization.
- Canonical token sources **MUST** be valid DTCG 2025.10 representations. Why: A standard source format enables validation and tooling without losing token meaning.
- The supported DTCG 2025.10 subset **MUST** be documented in the [initial DTCG input profile](#initial-dtcg-input-profile). Why: Contributors need to know which standard constructs the repository intentionally accepts.
- Token validation **MUST** reject constructs listed as unsupported in the [initial DTCG input profile](#initial-dtcg-input-profile) instead of silently reinterpreting them. Why: Silent reinterpretation can change token meaning during generation.
- Canonical token JSON **SHOULD** live under `src/Refraction.Client/Themes/Tokens/` in reference, system, and optional component catalogs. Why: A predictable source location keeps token ownership discoverable.
- Token catalog documents outside a `Themes` directory **MUST** use the `.tokens.json` suffix. Why: Every permitted catalog document needs a path or suffix selected by this instruction's `applyTo` metadata.
- DTCG source paths **MUST** begin with `ref.*`, `sys.*`, or optional `comp.*`. Why: The canonical JSON uses semantic source namespaces rather than CSS output names.
- Generated token custom properties **MUST** replace source dots with hyphens and prefix the source layer with `--rf-`, producing `--rf-ref-*`, `--rf-sys-*`, or deliberately exposed `--rf-comp-*`. Why: The output namespace preserves source ownership while remaining valid CSS.
- Whole-token DTCG aliases **MUST** emit a `var()` reference to the alias target's generated custom property rather than a flattened literal, preserving each link in a validated alias chain. Why: System and component defaults need live token bindings rather than copied values.
- Hand-authored generated CSS **MUST NOT** be the token source of truth. Why: Source edits need to survive regeneration without being overwritten or silently diverging.
- The future generator **MUST** write its complete token CSS artifact to `src/Refraction.Client/wwwroot/RefractionTokens.generated.css`. Why: Generation and freshness validation need one canonical output path.
- Generated token output **MUST** identify itself as generated. Why: Reviewers and tools must distinguish source from derived artifacts.
- Token generation **MUST** be deterministic for identical inputs. Why: Reproducible output makes changes reviewable and stale output detectable.
- Generated token CSS **MUST** follow the [canonical CSS serialization profile](#canonical-css-serialization-profile). Why: Literal values and file formatting need one reproducible representation.
- Repository validation **MUST** fail when generated token output is stale. Why: A source change without regenerated output would otherwise ship inconsistent themes.
- Reference tokens **MUST** describe values rather than UI purpose. Why: Semantic meaning belongs in the system layer.
- Component CSS **MUST NOT** consume `--rf-ref-*` properties directly. Why: The system layer is the boundary between values and component meaning.
- Reference scales **MUST** use an ordered direction such as `100`, `200`, and `300`. Why: Ordered scales make adjacent values predictable.
- Reference scales **MUST** document whether increasing numbers mean lighter, darker, larger, or smaller values. Why: The numeric order alone does not explain the scale's meaning.
- System tokens **MUST** describe semantic roles such as surface, text, action, status, border, focus, space, type, radius, motion, and layering. Why: Components should ask for meaning rather than palette implementation details.
- System tokens **SHOULD** reference reference tokens. Why: Theme semantics remain separated from raw values and can be rebound coherently.
- Component CSS **SHOULD** consume system tokens by default. Why: Semantic defaults keep components theme-agnostic and consistent.
- Component tokens **MUST** be added only for a stable public concept, intentional divergence, or a real per-component theming need. Why: Mechanical one-token-per-declaration APIs create noise and freeze implementation details.
- Component tokens **SHOULD** default to system tokens. Why: Consumers receive coherent defaults while retaining an intentional customization point.
- Reusable design decisions **SHOULD** use tokens for color, spacing, typography, radius, borders, shadows, opacity, motion, easing, focus, reusable sizes, and layering. Why: Repeated meaningful decisions need one semantic place to change.
- Reusable shadows **MUST** be assembled from named primitive system or component tokens while composite shadow types remain unsupported. Why: Shadow decisions need the supported scalar input profile rather than an invalid complete shadow token.
- Shadow blur-radius tokens **MUST** have non-negative values. Why: Negative blur radii make CSS shadows invalid.
- Authors **MUST** treat repeated meaningful design literals as evidence of a missing token. Why: Duplication causes visual drift even when every individual declaration looks reasonable.
- Structural values such as `0`, `100%`, `50%`, `auto`, `none`, `1fr`, and `currentColor` **MAY** remain literal. Why: Not every CSS value represents a reusable design decision.
- New work **MUST NOT** introduce additional `--rf-raw-*` variables. Why: The reference/system/component hierarchy replaces ambiguous raw aliases in this pre-release repository.
- New or changed CSS **MUST NOT** add references to existing `--rf-raw-*` variables except within documented temporary migration exclusions in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405). Why: Preserving unchanged legacy consumers must not expand the dependency set that later layers need to migrate.
- Consumed legacy Refraction variables, including `--rf-raw-*`, `--rf-color-*`, `--rf-focus-*`, and earlier spacing or typography properties, **MUST** remain available until their consumers are migrated. Why: Removing a consumed legacy property would make an intermediate layer invalid.
- The removal layer for each legacy Refraction token family **MUST** be recorded in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405). Why: Explicit removal ownership prevents consumed legacy properties from being deleted prematurely or forgotten.
- Final migration completion **MUST** remove every `--rf-raw-*` token and obsolete styling API. Why: The completed architecture cannot retain legacy selectors or token contracts.
- Generated built-in themes **MUST** follow the [initial theme input and output mapping](#initial-theme-input-and-output-mapping). Why: Catalog ownership and provider selectors need one deterministic contract.
- Built-in theme catalogs **MUST** preserve the same alias edges targeting system or component tokens in every mode. Why: Supported scoped overrides need one stable public dependency graph.
- Themes **SHOULD** rebind system tokens at a theme root. Why: Light, dark, high-contrast, and enterprise themes can change meaning without duplicating component selectors.
- Generated theme output **MUST** redeclare dependent reference, system, and component aliases within each theme scope that overrides their targets, including transitive dependencies. Why: Custom-property references resolve before inheritance, so aliases computed at an ancestor do not automatically rebind to a descendant's token override.
- Consumer-defined scopes overriding supported system or component tokens **MUST** redeclare dependent default system and component aliases in that scope, including transitive dependencies. Why: A custom scope needs the same rebinding that generated themes provide.
- Scope rebinding **MUST** preserve explicitly customized dependent token values. Why: Updating default aliases must not erase deliberate consumer customization.
- Documentation for supported scoped system or component-token overrides **MUST** identify the required alias redeclarations. Why: Consumers need a complete override contract without running the repository generator.
- Documentation for every supported color-token override **MUST** identify its same-scope forced-colors system-color mapping. Why: Token names do not establish the correct native accessibility color.
- Component CSS **SHOULD** remain theme-agnostic. Why: Theme behavior belongs in token values and semantic mappings.
- Theme definitions **MUST NOT** duplicate component selector rules solely to change theme values. Why: Semantic rebinding avoids a parallel theme-specific selector API.
- Text and images of text **MUST** meet [WCAG 2.2 AA contrast minimums](https://www.w3.org/TR/WCAG22/#contrast-minimum), including the criterion's large-text thresholds and exceptions, in every supported theme. Why: Light, dark, high-contrast, and branded themes need readable text.
- Control and graphical-object visuals **MUST** meet applicable [WCAG 2.2 AA non-text contrast requirements](https://www.w3.org/TR/WCAG22/#non-text-contrast) in every supported theme. Why: Authored colors must preserve the information needed to identify controls and their states.
- Interactive controls **MUST** retain a meaningful `:focus-visible` indication using shared focus tokens. Why: Keyboard users need a visible, consistent focus target.
- Non-essential motion **MUST** honor `@media (prefers-reduced-motion: reduce)`. Why: Motion preferences are an accessibility requirement.
- Refraction motion components **MUST** honor the named `RefractionReducedMotion` cascade from `CascadingRefractionProvider.IsReducedMotion`. Why: Hosts can request reduced motion even when the operating system has no preference.
- Refraction styles **MUST** account for forced-colors or high-contrast environments where their visual decisions affect meaning or control affordance. Why: System color modes can replace authored colors and expose contrast or state failures.
- The #405 integration layer **MUST** preserve native system-color mappings in `src/Refraction.Client/wwwroot/RefractionAccessibility.css`, loaded after generated tokens. Why: The bounded sRGB generator cannot encode forced-colors system keywords.
- The accessibility companion stylesheet **MUST** map both `:root` and every `[data-rf-theme]` scope to native system colors in forced-colors mode. Why: Root defaults are supported even when content is outside a theme provider.
- Forced-colors system mappings **MUST** take priority over normal inline branding through documented `!important` declarations or an equivalently validated cascade mechanism. Why: Loading a stylesheet later cannot override normal inline custom properties.
- Consumers overriding color tokens in a descendant scope **MUST** provide forced-colors system-color rebindings on that same scope. Why: An ancestor's important declarations cannot override a descendant's own declarations through inheritance.
- Consumer scope color rebindings **MUST** pass applicable forced-colors browser checks. Why: Supported branding must preserve native accessibility colors.
- The accessibility companion stylesheet **MUST** pass applicable CSS validation and forced-colors browser checks. Why: Hand-authored accessibility mappings need executable regression evidence.
- The accessibility companion stylesheet **MUST** preserve the built-in `color-scheme` mappings defined in the theme mapping. Why: Native controls need to agree with the provider mode after legacy theme rules are retired.
- A `color-scheme` migration **MUST** pass native-control browser checks in every built-in mode and forced-colors mode. Why: Token contrast checks do not establish user-agent rendering.
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
- The #405 validation layer **MUST** mechanically detect invalid Refraction class names. Why: Class ownership needs mechanical regression detection.
- The #405 validation layer **MUST** mechanically detect component block names assigned to multiple component owners. Why: Valid syntax does not prevent global ownership collisions.
- The #405 validation layer **MUST** mechanically detect invalid Refraction keyframe identifiers. Why: Animation names share a compiled namespace.
- The #405 validation layer **MUST** mechanically detect animation references that violate their owning-block contract. Why: A valid definition does not establish that references use the correct owner.
- The #405 validation layer **MUST** mechanically detect forbidden raw-token declarations and references. Why: Migration exclusions must not silently expand.
- The #405 validation layer **MUST** mechanically detect direct reference-token consumption in component CSS. Why: Components consume semantic tokens.
- The #405 validation layer **MUST** mechanically detect hard-coded design colors. Why: Reusable design values belong in tokens.
- The #405 validation layer **MUST** report every `::deep` occurrence with its file and location. Why: Mechanical detection does not establish whether an exception is justified.
- Reviewers **MUST** verify the documented justification of every reported `::deep` occurrence before delivery. Why: Boundary exceptions need a human ownership decision.
- The #405 validation layer **MUST** mechanically detect CSS ID selectors. Why: Component styling uses reusable ownership selectors.
- The #405 validation layer **MUST** report every `!important` occurrence with its file and location. Why: Mechanical detection does not establish whether an override is justified.
- Reviewers **MUST** verify the documented justification of every reported `!important` occurrence before delivery. Why: Accessibility and interoperability exceptions need an auditable decision.
- The #405 validation layer **MUST** mechanically detect invalid token names. Why: Source and output names need a valid namespace.
- The #405 validation layer **MUST** mechanically detect duplicate tokens. Why: One source path cannot have competing definitions.
- The #405 validation layer **MUST** mechanically detect flattened output-name collisions. Why: Distinct source paths cannot emit the same property.
- The #405 validation layer **MUST** mechanically detect unresolved aliases. Why: Every alias needs a valid target.
- The #405 validation layer **MUST** mechanically reject Refraction-owned CSS `var()` references absent from the validated token catalog and temporary legacy allowlist. Why: Catalog alias validation cannot catch a misspelled or removed property in a CSS consumer.
- The #405 validation layer **MUST** mechanically reject Refraction-owned custom-property declarations absent from the validated token catalog and temporary legacy allowlist, except definitions in the canonical generated artifact. Why: A misspelled override can remain unused without any invalid consumer reference.
- Existing private component properties outside the catalog **MUST** be recorded as scoped temporary migration exclusions in #405. Why: Legacy component-local names are not supported token properties.
- The #405 validation layer **MUST** mechanically detect stale generated output. Why: Source and derived output need to agree.
- The validation layer tracked in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405) **MUST** include rendered text and non-text contrast checks across every supported theme. Why: Valid token values alone do not establish sufficient contrast in composed controls and their states.
- Mechanical CSS and token checks **MUST** become required delivery gates only after their validators land with runnable commands and pipeline integration. Why: An unavailable validator cannot be an executable gate.
- Before those validators land, authors and reviewers **MUST** manually inspect the applicable authoring rules. Why: Applicable authoring requirements need review while enforcement is introduced in a later layer.
- Authors **MUST** record the manual authoring review in the PR until mechanical validators land. Why: Review evidence needs an attributable record.
- Authors **MUST** list unavailable mechanical checks in the PR until their validators land. Why: Unavailable checks must remain visible validation gaps.
- Validation **SHOULD** use existing PowerShell and .NET infrastructure, adding Stylelint only when it materially improves coverage without an unrelated toolchain. Why: Enforcement should fit the repository's build model.
- New Refraction components and changed styling surfaces **MUST** follow the immediate authoring requirements described in [Status and references](#status-and-references). Why: Styling ownership and accessibility can apply before token generation exists.
- Runtime adoption of new catalog-backed tokens **MUST** wait until the #405 generator integration is usable. Why: A catalog entry alone does not define a runtime CSS property.
- Styling added before generator integration **MUST** use existing defined semantic properties or a documented temporary migration exclusion in #405. Why: New styling needs a working source-to-runtime path.
- Existing legacy styles **MUST** be migrated in later independent, valid stack layers rather than hidden by undocumented compatibility rules. Why: Each migration layer remains reviewable while the target architecture stays clear.
- Temporary migration exclusions **MUST** name their scope, reason, owner, and removal layer in [issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405). Why: Explicit inventory prevents temporary exceptions from becoming permanent APIs.
- Compatibility aliases **MUST NOT** be permanent. Why: Pre-release freedom permits cleanup instead of preserving historical conventions.
- Final migration completion **MUST** remove all temporary migration exclusions and compatibility aliases. Why: The final layer must leave one enforceable standard without transitional escape hatches.
- Every other exception **MUST** have a documented technical reason and review condition. Why: Unexplained exceptions become accidental architecture.
- CSS and design-system work **MUST** follow [PR size and stacked delivery](pr-size-and-stacking.instructions.md) when choosing coherent PR boundaries and whether to stack. Why: The canonical policy preserves independent PRs and its documented fallback when a split or stack is unsuitable.
- Stack work **MUST** use the `gh-stack` skill. Why: Native GitHub stacks need the repository's supported lifecycle commands.
- Each stack layer **MUST** be independently valid and pass its applicable build, test, token, CSS, and review advancement gate before dependent work starts. Why: A later layer cannot be the hidden prerequisite for an earlier one.
- CSS and design-system work **MUST** follow the governing [optional model routing policy](codex-model-routing.instructions.md), including its selection, verification, role-precedence, and host-capacity requirements when the operator explicitly selects the supported profile. Why: Styling work does not establish a separate model strategy.
- CSS tasks **MUST NOT** select a model profile or introduce model, reasoning, or concurrency overrides on their own. Why: Sessions without an explicitly selected profile retain their host and user-selected settings.
- Agent files **SHOULD** link the governing model routing policy rather than restating operational settings. Why: One authority prevents stale or conflicting per-agent requirements.
- Each layer **MUST** inspect relevant selectors before handoff. Why: Selector ownership needs direct evidence.
- Each layer **MUST** inspect relevant token references before handoff. Why: Consumers need supported properties.
- Each layer **MUST** inspect relevant state bindings before handoff. Why: Visual and semantic state need to agree.
- Each layer **MUST** inspect relevant generated output before handoff. Why: Derived artifacts need source-backed verification.
- Each layer **MUST** inspect relevant rendered behavior before handoff. Why: Source and build evidence cannot establish the final user experience.

## Scope and Audience

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

A whole-token alias `{sys.color.action.primary}` in `comp.pane.accent-border` emits `--rf-comp-pane-accent-border: var(--rf-sys-color-action-primary);`. A nested theme root that overrides the system token redeclares that component alias in the same scope.

For the future token names above, a consumer-defined scope uses the same rebinding. The application supplies `--brand-primary-color`; this example overrides two supported Refraction properties rather than defining new Refraction tokens:

```css
.example-brand-scope {
    --rf-sys-color-action-primary: var(--brand-primary-color);
    --rf-comp-pane-accent-border: var(--rf-sys-color-action-primary);
}

@media (forced-colors: active) {
    .example-brand-scope {
        /* Accessibility: system colors take priority over normal branding. */
        --rf-sys-color-action-primary: LinkText !important;
        --rf-comp-pane-accent-border: var(--rf-sys-color-action-primary) !important;
    }
}
```

The override documentation lists every dependent alias to redeclare, including intermediate aliases in longer chains. This contract also applies when a supported component token is overridden and other component tokens alias it. If the consumer deliberately customizes the Pane accent border separately, that explicit component value remains in place in normal color modes instead of being replaced with its default alias. Forced-colors system mappings take priority within the consumer's own scope. These token names describe the future migration target, not the current checkout's API.

## Initial DTCG input profile

This selected DTCG 2025.10 input profile defines the accepted source shape and value types:

| Area | Profile |
| --- | --- |
| Documents | JSON groups may carry `$type`; nested groups inherit it. Every literal token uses its token-local `$type`, or the closest inherited group `$type` when no local type exists. Aliases use the precedence in the Aliases row. No value-based type inference is supported. Tokens carry `$value`, with optional string `$description`. |
| Paths | Source paths begin `ref.*`, `sys.*`, or `comp.*`; segments use lowercase kebab-case, with numeric segments for ordered scales. |
| `color` | An sRGB object with only `colorSpace: "srgb"`, exactly three finite `components` in 0..1, and optional `alpha` in 0..1. The optional DTCG `hex` fallback is rejected by this initial profile. CSS strings are not color values. |
| `dimension` | An object with a finite value and unit `px` or `rem`. CSS strings are not dimension values. |
| `duration` | An object with a finite non-negative value and unit `ms` or `s`. CSS strings are not duration values. |
| `number` / `fontFamily` | A finite numeric value; a non-empty name or a non-empty array of names. |
| `fontWeight` | A numeric value from 1 through 1000 or a lowercase alias defined by DTCG 2025.10. |
| `cubicBezier` | Four finite numbers; x coordinates are 0..1 and y coordinates are unrestricted finite values. |
| Aliases | A whole-token curly-brace alias such as `{ref.color.neo-blue.300}` may chain through targets. At every hop, `ref` aliases target only `ref`; `sys` aliases target `ref` or `sys`; `comp` aliases target `sys` or `comp` with the same component segment immediately after `comp` (for example, `comp.button.*` cannot target `comp.pane.*`). Shared cross-component concepts belong in `sys`. An alias uses its token-local `$type` when present; otherwise it uses the resolved target token's type, regardless of the alias's inherited group `$type`. Each target resolves its own type recursively; literal targets use their token-local or closest inherited group type. Invalid layer direction, cross-component aliases, cycles, unresolved targets, and type mismatches are errors. |
| Catalog scope | All `*.json` files directly in one selected catalog directory form one scope. The canonical directory is `src/Refraction.Client/Themes/Tokens/`; its reference, system, and component documents are loaded together before alias resolution. Additional catalog directories are independently selected, self-contained scopes without implicit inheritance or cross-directory aliases. Duplicate JSON properties, duplicate paths, and flattened CSS-name collisions are errors. |
| Rejected / conformance | Non-finite binary64 numeric results, NULL or unpaired surrogates in font-family names, composite types, property-level references or JSON Pointer, `$root`, `$extends`, extensions, deprecation metadata, and unknown constructs; this is selected input support, not full-format DTCG tool conformance. |

A reusable shadow uses `dimension` tokens for its offsets, blur, and spread, plus a `color` token for its color. Offsets and spread may be negative; blur values are non-negative, as required by the [CSS shadow value grammar](https://www.w3.org/TR/css-backgrounds-3/#box-shadow). Component CSS assembles the CSS shadow value; there is no complete shadow-valued token in the initial profile. For example:

```css
box-shadow:
  var(--rf-sys-shadow-elevation-offset-x)
  var(--rf-sys-shadow-elevation-offset-y)
  var(--rf-sys-shadow-elevation-blur)
  var(--rf-sys-shadow-elevation-spread)
  var(--rf-sys-shadow-elevation-color);
```

## Canonical CSS serialization profile

For the future generator, JSON numbers use finite IEEE 754 binary64 semantics, including the ordinary rounding that occurs when decimal input such as `0.1` is parsed. Non-finite parsing results are rejected. `N` is the culture-independent canonical serialization defined by [RFC 8785 section 3.2.2.3](https://www.rfc-editor.org/rfc/rfc8785.html#section-3.2.2.3), including negative zero becoming `0`. Serialization preserves the parsed binary64 value with a shortest round-trip representation; exact decimal equivalence with the source spelling is not required. No additional fixed decimal-place rounding or value clamping is used.

| Literal type | CSS value |
| --- | --- |
| `color` | `color(srgb N N N / N)` in component order, with alpha `1` when omitted. |
| `dimension` / `duration` | `N` followed immediately by the input's allowed unit; units are preserved without conversion. |
| `number` | `N`. |
| `fontWeight` | `N` for numeric values; DTCG named weights become their specified numeric equivalent before serialization. |
| `fontFamily` | Preserve family order and join entries with a comma and one space. The initial supported generic and compatibility keywords are `serif`, `sans-serif`, `system-ui`, `cursive`, `fantasy`, `math`, `monospace`, `emoji`, `fangsong`, `ui-serif`, `ui-sans-serif`, `ui-monospace`, and `ui-rounded`; case-insensitive matches emit lowercase keywords. Other names use [CSSOM string serialization](https://www.w3.org/TR/cssom-1/#serialize-a-string), including quotes and escapes. NULL and unpaired Unicode surrogates are rejected. |
| `cubicBezier` | `cubic-bezier(N, N, N, N)` in the input's four-coordinate order. |

Within each selector block, declarations sort by generated property name using ordinal comparison. The output uses UTF-8 without a BOM, LF line endings, two-space declaration indentation, `name: value;` declarations, and one final newline. Alias output remains the immediate-target `var()` binding defined above.

The complete generated file uses this exact framing. Each `<declarations>` line is replaced with that scope's sorted declaration lines, including their two-space indentation and LF terminators; the placeholder itself is not emitted. Selector lists stay on one line, opening braces follow one space, closing braces start at column zero, and exactly one blank line separates the header and each block. There are no other comments or provenance fields.

```text
/* Generated by Refraction token generator. Do not edit. */

:root, [data-rf-theme] {
<declarations>
}

[data-rf-theme="light"] {
<declarations>
}

[data-rf-theme="high-contrast"] {
<declarations>
}
```

The generator replaces the entire `RefractionTokens.generated.css` artifact on each run; freshness validation compares that file's complete bytes. This is an additional static asset, not a replacement for the current hand-authored `RefractionTokens.css`. The #405 integration layer loads the legacy stylesheet first and the generated asset after it, preserving consumed legacy properties and existing base or accessibility rules until their recorded migration layers. The integration layer retains the current forced-colors rules until the accessibility companion is wired and verified. That companion owns `@media (forced-colors: active)` system-color mappings for migrated system/component properties and their dependent aliases; it is deliberately hand-authored and is not part of the generated-file freshness comparison. Existing build, CSS, and browser gates cover it. No generated asset, companion stylesheet, or loading change is introduced here.

The generator layer includes byte-for-byte golden fixtures for each supported type, numeric edge cases, font-family escaping, aliases, and complete theme scopes before generated-output freshness becomes a gate. This serialization contract describes a future implementation; it does not introduce generated CSS here.

## Initial theme input and output mapping

The future generator uses complete, independent catalogs for the three existing provider modes. Each row loads only JSON files directly in its directory, using the catalog-scope rules above.

| Mode | Catalog directory | Generated selector |
| --- | --- | --- |
| Dark/default | `src/Refraction.Client/Themes/Tokens/` | `:root, [data-rf-theme]` |
| Light | `src/Refraction.Client/Themes/Tokens/Light/` | `[data-rf-theme="light"]` |
| High contrast | `src/Refraction.Client/Themes/Tokens/HighContrast/` | `[data-rf-theme="high-contrast"]` |

Output emits the default block first, then Light, then HighContrast. Each theme catalog includes all of its reference, system, and component tokens; it does not inherit JSON from the default catalog. All modes expose the same system/component paths and types. Aliases targeting `sys` or `comp` use the same immediate target in every mode; reference-token targets and literal values may vary. The generator rejects differences in that public dependency graph before output. Each selector declares the complete validated catalog, including alias declarations, so a nested provider resets its defaults locally.

`src/Refraction.Client/wwwroot/RefractionAccessibility.css` also owns hand-authored `color-scheme`: `dark` on `:root, [data-rf-theme]`, `light` on `[data-rf-theme="light"]`, and `dark` on `[data-rf-theme="high-contrast"]`. Its later `@media (forced-colors: active)` rule sets `light dark` on `:root, [data-rf-theme]`. These declarations stay outside the generated token artifact. Legacy declarations remain until this companion is loaded and the native-control checks pass.

The selector names match `CascadingRefractionProvider` today; the JSON directories and generated output are future migration targets. Additional catalogs remain independently valid input scopes. Additional generated theme IDs require an explicit input/selector mapping and provider contract in a later architecture change; they are not inferred from arbitrary directory names. Consumer-defined branding scopes use the supported token override contract.

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

The existing `ProgressArc.razor.css` references to `--rf-progress-size` and `--rf-progress-stroke-width` are temporary legacy exclusions, not private token APIs. Their scope is the current ProgressArc component, their owner is Refraction component maintenance, and their reason is preservation of existing geometry before migration. The #405 `progress-arc-token-migration` layer replaces them with catalog-backed properties or typed state and removes the allowlist entries after validation. The issue records this inventory; this guide does not add new consumers.

`src/Refraction.Abstractions/Theme/IRefractionTheme.cs` is a legacy interface with contract tests but no current runtime bridge to CSS tokens. It is not an input to generated catalogs. The #405 `legacy-theme-contract-removal` layer removes the interface and its contract tests after auditing usages, migrating any consumers found, and updating public documentation. This guide retains it in the interim and does not introduce a bridge.

This layer defines the forward-looking authoring contract. Token catalogs, generators, validators, compiled-bundle evidence, and complete consumer documentation belong to subsequent stack layers.

CSS ownership, class naming, component isolation, state semantics, accessibility, and migration inventory apply immediately. Mandatory adoption of newly cataloged tokens starts when the #405 generator integration validates the catalogs, emits the canonical asset, and loads it in the application. Until then, new styling uses existing defined semantic properties; a reusable value with no suitable existing property needs a scoped temporary exclusion for its hand-authored value, with a reason, owner, and removal layer recorded in #405. This interim route does not permit new `--rf-raw-*` declarations or undocumented raw-token references. Canonical catalogs authored in advance still follow the input profile, but undefined future properties are not consumed at runtime.

Mechanical CSS/token validation and generated-output freshness checks become required gates when the validation layer lands with runnable commands and pipeline integration. Until then, PRs record manual authoring-review evidence and unavailable checks while continuing all existing applicable build, cleanup, lint, test, and browser gates.

- [Blazor UX Guidelines](blazor-ux-guidelines.instructions.md)
- [Namespace and Folder Placement](namespace-folder-placement.instructions.md)
- [PR Size and Stacked Delivery](pr-size-and-stacking.instructions.md)
- [CSS migration issue #405](https://github.com/Gibbs-Morris/mississippi/issues/405)
- [Worker configuration follow-up issue #675](https://github.com/Gibbs-Morris/mississippi/issues/675)
- [Official gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md)
- [DTCG format](https://www.designtokens.org/tr/2025.10/format/)
- [CSS custom-property resolution and inheritance](https://www.w3.org/TR/css-variables-1/#cycles)
