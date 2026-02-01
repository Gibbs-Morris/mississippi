# Implementation Plan (Revised)

## Changes from initial draft

- Incorporates existing Refraction atoms/molecules/organisms and token file.
- Adds explicit bUnit-based L0 tests for new components.
- Adds explicit docs placement under docs/Docusaurus/docs/refraction.
- Adds requirement to clear legacy Refraction component content before reimplementation.

## Detailed step-by-step checklist

1. Documentation-first deliverables
	- Create docs/Docusaurus/docs/refraction with _category_.json and required pages.
	- Draft design language spec reflecting the authoritative hologram brief and expanded handoff (z-layers, palette hex values, typography, motion primitives, acceptance checklist).
	- Draft token/theming spec, including Opacity/Stroke/Glow/Depth/Motion values and the new palette/alpha tiers.
	- Draft component inventory (atomic design) and reference examples.
	- Ensure all docs include YAML front matter and markdownlint-compliant structure.

2. Token and theming foundation
	- Extend src/Refraction/Themes/RefractionTokens.css with required token families and theme variants.
	- Add Neon Blue (dark) and Water Blue (light + dark) themes using the provided hex palette and alpha tiers.
	- Document how to override tokens via CSS variables.

3. Component architecture alignment
	- Clear legacy Refraction component content to start fresh, retaining folder structure where needed.
	- Keep atomic design folders: src/Refraction/Components/{Atoms,Molecules,Organisms}.
	- Add Templates/Pages compositions under src/Refraction.Pages where appropriate.
	- Ensure components follow state-down / events-up contract and DX conventions.

3.1. Legacy wipe validation
	- Scan solution references to Refraction components to avoid breaking builds.
	- Remove or replace old components after new baseline primitives are in place.

4. Signature hologram components
	- Implement or align GlassPlane, RingNav, RibbonDetail, CalloutLine, VolumetricObject, PulseConfirm.
	- Ensure accessibility, keyboard navigation, and mobile variants are defined.

5. Data-centric components
	- Add enterprise data components (tables, filters, summaries) with virtualization.
	- Add Streamlit-inspired parameter panels and report blocks.

6. Chart.js integration (Blazor-first)
	- Create internal JS interop wrapper within Refraction (no JS types exposed).
	- Define strongly typed .NET chart configuration models and events.
	- Ensure responsive resizing and hover/click selection events.

7. Tests and examples
	- Add bUnit L0 tests for each component (rendering, states, callbacks, accessibility attributes).
	- Add example snippets to docs pages for each component.
	- Validate mobile variants and keyboard navigation behavior.

8. Quality gates and verification
	- Run cleanup and build scripts and fix warnings.
	- Run unit tests for Mississippi solution.
	- Run mutation tests if required by touched projects.

## Files and modules likely to change

- docs/Docusaurus/docs/refraction/**
- docs/Docusaurus/docs/refraction/_category_.json
- src/Refraction/Components/**
- src/Refraction/Themes/RefractionTokens.css
- src/Refraction.Pages/** (templates/pages and samples)
- tests/Refraction.L0Tests/**
- tests/Refraction.Pages.L0Tests/**

## Data model and configuration changes

- CSS token additions under RefractionTokens.css.
- Optional JS interop module file for Chart.js (internal only).

## API/contract changes and compatibility

- New public components and parameters in Mississippi.Refraction namespaces.
- Avoid breaking changes in existing components; add new parameters with defaults.

## Observability changes

- Ensure ARIA attributes and keyboard focus styles are defined; avoid runtime logging unless required.

## Test plan (commands)

- Build: pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
- Cleanup: pwsh ./clean-up.ps1
- Unit tests: pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
- Mutation tests (if required): pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1

## Rollout plan

- Introduce new components alongside existing ones; avoid breaking changes.
- Provide migration notes if any existing components change behavior.

## Risks and mitigations

- Scope creep: enforce component inventory and phase delivery by iterations.
- Performance risk: require virtualization for large data components.
- Accessibility gaps: define keyboard patterns and add bUnit tests for ARIA roles.
