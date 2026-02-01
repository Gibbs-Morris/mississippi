---
id: refraction-component-roadmap
title: Component Roadmap
sidebar_label: Component Roadmap
sidebar_position: 3
description: Planned components for the Refraction library organized by atomic design level and implementation phase.
---

# Component Roadmap

## Overview

This page lists all planned Refraction components, organized by atomic design level and implementation phase. Each component includes its purpose, atomic classification, and priority.

## Project Assignment

Components are split across two projects based on their state requirements:

| Project | Contains | Dependencies |
| --- | --- | --- |
| **Refraction** | Stateless presentation components | None (pure UI) |
| **Refraction.Pages** | Stateful page-level components | Inlet, Reservoir |

**Rule**: If a component needs to dispatch commands (Inlet) or subscribe to state (Reservoir), it belongs in `Refraction.Pages`. All other components belong in `Refraction`.

## Atomic Design Mapping

Components are classified into atomic design levels:

| Level | Description | Examples |
| --- | --- | --- |
| **Atoms** | Foundational primitives that cannot be broken down further | Tokens, typography, icons, basic surfaces |
| **Molecules** | Small composites that combine atoms into functional units | Input groups, action chips, callouts |
| **Organisms** | Feature-complete components with significant functionality | Lens panels, data grids, charts, shells |
| **Templates** | Reference compositions showing how organisms combine | Situational overlay, workbench mode |

## Implementation Phases

### Phase 1: Foundation (Minimal Viable Set)

The smallest coherent set that establishes the design language. All Phase 1 components are stateless and belong in the `Refraction` project.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **Tokens** | Atom | Refraction | None (foundation) | Color, spacing, elevation, timing values (Spectrum, Radiance, Veil, Depth, Flow, Signal) |
| **Typography** | Atom | Refraction | Tokens | Font families, sizes, weights, typographic scale |
| **CausticField** | Atom | Refraction | Tokens | Subtle background light patterns/texture |
| **AnchorPoint** | Atom | Refraction | Tokens | Dot/locator origin for MooringLine attachment |
| **Beacon** | Molecule | Refraction | Tokens | Attention indicator/system alert light |
| **Ripple** | Molecule | Refraction | Tokens | Confirmation pulse/commit feedback |
| **MooringLine** | Molecule | Refraction | AnchorPoint, Tokens | Tether/callout leader line connecting anchor to label |
| **Buoy** | Molecule | Refraction | MooringLine (optional), AnchorPoint, Tokens | Map marker/anchored point with optional label |
| **Lens** | Organism | Refraction | Tokens, CausticField (optional) | Primary working panel/plane; the main surface for user interaction |
| **Horizon** | Organism | Refraction | Tokens, Beacon | Persistent status ring/arc navigation; system context display |
| **TideRibbon** | Organism | Refraction | Tokens, Beacon (optional) | Progressive disclosure panel; never blocks world view |

**Motion verbs for Phase 1**: Emerge, Coalesce, Moor, Ripple, Disperse, Current

### Phase 2: Navigation and Instrumentation

Expand navigation and measurement capabilities. All stateless presentation components.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **Frame** | Atom | Refraction | Tokens | Partial corner brackets/HUD framing |
| **Gauge** | Molecule | Refraction | Tokens | Instrument-style meter (arc or bar with fill) |
| **Sounding** | Molecule | Refraction | Typography, Tokens | Measurement readout (depth/latency/units) |
| **Bearing** | Molecule | Refraction | Tokens, Gauge (shares styling) | Compass/heading widget with directional indicator |
| **Orbit** | Organism | Refraction | Tokens, Frame | Radial navigation control with rotatable segments |

### Phase 3: Surfaces and Containers

Complete the surface and container vocabulary. All stateless presentation components.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **Pane** | Molecule | Refraction | Tokens | Simple translucent panel (secondary to Lens) |
| **Prism** | Organism | Refraction | Pane, Tokens | Container that "refracts" content (split views, multi-focus) |
| **Ledger** | Organism | Refraction | Typography, Tokens | Dense, aligned, tabular diagnostic list (events/metrics) |
| **Dock** | Organism | Refraction | Tokens, Pane | Pinned tools area (rare, compact) |

### Phase 4: Feedback and Transient UI

Micro-interactions and transient feedback elements. All stateless presentation components.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **Wake** | Atom | Refraction | Tokens | Pointer trail/motion echo for drag operations |
| **Spray** | Atom | Refraction | Tokens | Burst micro-particles (sparingly, for emphasis) |
| **Flare** | Atom | Refraction | Ripple, Tokens | Critical attention pulse (rare; reserved for hard errors) |
| **FocusReticle** | Molecule | Refraction | Frame, Tokens | Selection indicator (thin ring + corner brackets) |
| **ActionChip** | Molecule | Refraction | Typography, Ripple, Tokens | Fast command entry capsule with verb label |

### Phase 5: Volumes and Ambient

3D representations and ambient background motifs. All stateless presentation components.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **DriftGrid** | Atom | Refraction | CausticField (styling), Tokens | Ambient grid drift for coordinate reference |
| **Scatter** | Atom | Refraction | Tokens | Particle haze layer for depth |
| **Shoals** | Organism | Refraction | AnchorPoint, MooringLine, Frame, Tokens | 3D data representation (models, routes, constraints) |

### Phase 6: Data Visualization

Pure C# Blazor chart and data components using SVG rendering. All stateless; data passed via parameters. **No JavaScript**.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **MetricCard** | Molecule | Refraction | Typography, Tokens | Single-value display with trend indicator |
| **LineChart** | Organism | Refraction | Typography, Tokens | Time-series and trend visualization |
| **BarChart** | Organism | Refraction | Typography, Tokens | Categorical comparison |
| **PieChart** | Organism | Refraction | Typography, Tokens | Part-to-whole relationships |
| **DataGrid** | Organism | Refraction | Typography, FocusReticle, Tokens | Virtualized table with sorting/filtering |

### Phase 7: Forms and Input

Enterprise form components. All stateless; binding via parameters and `EventCallback`.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **Toggle** | Atom | Refraction | Tokens | Boolean switch |
| **Checkbox** | Atom | Refraction | Tokens | Multi-select option |
| **TextField** | Molecule | Refraction | Typography, FocusReticle, Tokens | Text input with label, validation, and states |
| **Slider** | Molecule | Refraction | Tokens, Gauge (styling) | Range input |
| **RadioGroup** | Molecule | Refraction | Typography, Tokens | Single-select from options |
| **SelectField** | Molecule | Refraction | TextField, Pane, Tokens | Dropdown selection |
| **DatePicker** | Organism | Refraction | TextField, Pane, ActionChip, Tokens | Date/time selection |

### Phase 8: Application Shell

Top-level application structure. Stateless shell components in `Refraction`; stateful page compositions in `Refraction.Pages`.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **Breadcrumb** | Molecule | Refraction | Typography, Tokens | Location indicator |
| **NavRail** | Organism | Refraction | Beacon, Tokens | Vertical navigation rail |
| **CommandBar** | Organism | Refraction | ActionChip, Tokens | Horizontal command surface |
| **TabBar** | Organism | Refraction | FocusReticle, Typography, Tokens | Horizontal tab navigation |
| **Shell** | Template | Refraction | NavRail, CommandBar, Breadcrumb, TabBar, Lens | Application frame with navigation and content areas |

### Phase 9: Page Components

Stateful page-level components that integrate with Mississippi infrastructure.

| Component | Level | Project | Dependencies | Purpose |
| --- | --- | --- | --- | --- |
| **FormPage** | Template | Refraction.Pages | Form components (Phase 7), Shell, Inlet | Form submission via Inlet commands |
| **ListPage** | Template | Refraction.Pages | DataGrid, Shell, Reservoir | Paginated list with live updates from Reservoir |
| **EntityDetailPage** | Template | Refraction.Pages | Lens, TideRibbon, Shell, Inlet, Reservoir | Entity view with state + command integration |
| **DashboardPage** | Template | Refraction.Pages | MetricCard, LineChart, DataGrid, Shell, Reservoir | Live dashboard with Reservoir state subscription |

## Component Catalog (Full Reference)

All components in this catalog belong to the **Refraction** project (stateless) unless explicitly marked as **Refraction.Pages** (stateful).

### Surfaces and Containers

#### Lens

- **Atomic Level**: Organism
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens, CausticField (optional)
- **Purpose**: Primary working panel/plane
- **Anatomy**: Translucent plane + edge glow + contrast plates
- **States**: Available / Focused / Editing
- **Motion**: Stabilizes when focused (Emerge → stable)
- **Notes**: Only one primary Lens at a time

#### Prism

- **Atomic Level**: Organism
- **Phase**: 3
- **Project**: Refraction
- **Dependencies**: Pane, Tokens
- **Purpose**: Container that "refracts" content (split views, multi-focus)
- **Anatomy**: Subdivided translucent panes
- **States**: Single / Split
- **Motion**: Coalesce on split
- **Notes**: Use sparingly

#### Pane

- **Atomic Level**: Molecule
- **Phase**: 3
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Simple translucent panel (secondary)
- **Anatomy**: Basic translucent surface
- **States**: Visible / Hidden
- **Motion**: Emerge / Disperse
- **Notes**: Subordinate to Lens

#### Frame

- **Atomic Level**: Atom
- **Phase**: 2
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Partial corner brackets/HUD framing
- **Anatomy**: Corner lines, not full boxes
- **States**: Idle
- **Motion**: Coalesce on appearance
- **Notes**: Keeps content airy

### Navigation and Status

#### Horizon

- **Atomic Level**: Organism
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens, Beacon
- **Purpose**: Persistent status ring/arc navigation
- **Anatomy**: Radial ring + tiny ticks + 1–2 word labels
- **States**: Normal / Warning / Critical
- **Motion**: Subtle pulse on changes
- **Notes**: Keep outside primary focal region

#### Orbit

- **Atomic Level**: Organism
- **Phase**: 2
- **Project**: Refraction
- **Dependencies**: Tokens, Frame
- **Purpose**: Radial navigation control
- **Anatomy**: Rotatable ring with segments
- **States**: Idle / Active
- **Motion**: Gyre on hover
- **Notes**: Contextual navigation only

#### Beacon

- **Atomic Level**: Molecule
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Attention indicator/system alert light
- **Anatomy**: Pulsing dot or ring
- **States**: Info / Warning / Critical
- **Motion**: Ripple pulse
- **Notes**: Reserved for important alerts

#### Buoy

- **Atomic Level**: Molecule
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: MooringLine (optional), AnchorPoint, Tokens
- **Purpose**: Map marker/anchored point
- **Anatomy**: Anchor dot + optional label
- **States**: Idle / Selected
- **Motion**: Emerge on placement
- **Notes**: Always anchored to context

#### Bearing

- **Atomic Level**: Molecule
- **Phase**: 2
- **Project**: Refraction
- **Dependencies**: Tokens, Gauge
- **Purpose**: Compass/heading widget
- **Anatomy**: Directional indicator + numeric readout
- **States**: Idle / Tracking
- **Motion**: Smooth rotation
- **Notes**: Instrument-style precision

### Detail and Progressive Disclosure

#### TideRibbon

- **Atomic Level**: Organism
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens, Beacon (optional)
- **Purpose**: Progressive disclosure without blocking world
- **Anatomy**: Thin side/bottom ribbon + stacked chips
- **States**: Collapsed / Expanded / Pinned
- **Motion**: Slide + fade, no bounce
- **Notes**: Never exceed ~30–40% of visible area

#### Ledger

- **Atomic Level**: Organism
- **Phase**: 3
- **Project**: Refraction
- **Dependencies**: Typography, Tokens
- **Purpose**: Dense, aligned, tabular diagnostic list (events/metrics)
- **Anatomy**: Aligned columns with tabular numerals
- **States**: Collapsed / Expanded
- **Motion**: Slide in
- **Notes**: Use for timelines/event logs

#### Dock

- **Atomic Level**: Organism
- **Phase**: 3
- **Project**: Refraction
- **Dependencies**: Tokens, Pane
- **Purpose**: Pinned tools area (rare, compact)
- **Anatomy**: Minimal strip with action chips
- **States**: Visible / Hidden
- **Motion**: Emerge / Disperse
- **Notes**: Use sparingly

### Callouts, Anchoring, Measurement

#### MooringLine

- **Atomic Level**: Molecule
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: AnchorPoint, Tokens
- **Purpose**: Tether/callout leader line
- **Anatomy**: Leader line connecting anchor to label
- **States**: Growing / Complete
- **Motion**: Moor (line grows from anchor → label)
- **Notes**: Essential for "precision" tone; never teleport

#### AnchorPoint

- **Atomic Level**: Atom
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: The dot/locator origin
- **Anatomy**: Small dot at attachment point
- **States**: Idle / Highlighted
- **Motion**: Pulse on connection
- **Notes**: Origin for MooringLine

#### Sounding

- **Atomic Level**: Molecule
- **Phase**: 2
- **Project**: Refraction
- **Dependencies**: Typography, Tokens
- **Purpose**: Measurement readout (depth/latency/units)
- **Anatomy**: Numeric display with unit label
- **States**: Idle / Updating
- **Motion**: Value transitions
- **Notes**: Use tabular numerals

#### Gauge

- **Atomic Level**: Molecule
- **Phase**: 2
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Instrument-style meter
- **Anatomy**: Arc or bar with fill indicator
- **States**: Normal / Warning / Critical
- **Motion**: Smooth fill transitions
- **Notes**: Compact instrument display

### Feedback and Transient UI

#### Ripple

- **Atomic Level**: Molecule
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Confirmation pulse/commit feedback
- **Anatomy**: Expanding ring + brief luminance bloom
- **States**: Success / Warning / Failure
- **Motion**: 120ms pulse + fade
- **Notes**: Never block flow

#### Wake

- **Atomic Level**: Atom
- **Phase**: 4
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Pointer trail/motion echo
- **Anatomy**: Fading trail behind movement
- **States**: Active only
- **Motion**: Follows pointer with decay
- **Notes**: Subtle feedback for drag operations

#### Spray

- **Atomic Level**: Atom
- **Phase**: 4
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Burst micro-particles (sparingly, for emphasis)
- **Anatomy**: Particle burst
- **States**: Triggered only
- **Motion**: Brief burst + fade
- **Notes**: Reserved for significant events

#### Flare

- **Atomic Level**: Atom
- **Phase**: 4
- **Project**: Refraction
- **Dependencies**: Ripple, Tokens
- **Purpose**: Critical attention pulse (rare; reserved for hard errors)
- **Anatomy**: Bright flash + ripple
- **States**: Critical only
- **Motion**: Intense Ripple
- **Notes**: Very rare; hard stops only

### Ambient Background Motifs

#### CausticField

- **Atomic Level**: Atom
- **Phase**: 1
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Subtle background light patterns/texture
- **Anatomy**: Animated light caustics
- **States**: Idle only
- **Motion**: Slow Current
- **Notes**: Never competes with content

#### DriftGrid

- **Atomic Level**: Atom
- **Phase**: 5
- **Project**: Refraction
- **Dependencies**: CausticField, Tokens
- **Purpose**: Ambient grid drift
- **Anatomy**: Faint grid lines
- **States**: Idle only
- **Motion**: Slow Current
- **Notes**: Coordinate reference only

#### Scatter

- **Atomic Level**: Atom
- **Phase**: 5
- **Project**: Refraction
- **Dependencies**: Tokens
- **Purpose**: Particle haze layer
- **Anatomy**: Floating particles
- **States**: Idle only
- **Motion**: Slow Current
- **Notes**: Adds depth; never distracting

### Volumes and Reticles

#### Shoals (Object Volume)

- **Atomic Level**: Organism
- **Phase**: 5
- **Project**: Refraction
- **Dependencies**: AnchorPoint, MooringLine, Frame, Tokens
- **Purpose**: 3D data representation (models, routes, constraints)
- **Anatomy**: Wireframe + ghosted layers + anchor point
- **States**: Ambient / Focused / Inspected
- **Motion**: Parallax; slow Gyre optional
- **Notes**: Always anchored (no free-floating without an origin)

#### FocusReticle

- **Atomic Level**: Molecule
- **Phase**: 4
- **Project**: Refraction
- **Dependencies**: Frame, Tokens
- **Purpose**: Clarify what is currently selected
- **Anatomy**: Thin ring + corner brackets
- **States**: Acquiring / Locked
- **Motion**: Quick draw-on + settle
- **Notes**: Avoid thick outlines

### Action Components

#### ActionChip

- **Atomic Level**: Molecule
- **Phase**: 4
- **Project**: Refraction
- **Dependencies**: Typography, Ripple, Tokens
- **Purpose**: Fast command entry without menus
- **Anatomy**: Small capsules with verb labels
- **States**: Available / Pressed / Queued
- **Motion**: Micro compress + pulse
- **Notes**: Keep verbs consistent ("Sweep", "Route", "Moor", "Export")

## Layout Templates

### Template A — Situational Overlay

Use when the user needs awareness more than editing.

- Horizon + CausticField / Mist
- 1–3 highlighted Shoals
- Minimal TideRibbons (only Beacons)

### Template B — Workbench Mode

Use for precise configuration/planning.

- One dominant Lens
- TideRibbons for parameters
- Shoals anchored above Lens
- Frequent MooringLines + Soundings

### Template C — Diagnostic / Investigation

Use for root-cause analysis.

- Stacked Ledgers (events, metrics, traces)
- FocusReticle + spotlight effect
- Sweep motifs for refresh operations

## Visual State Model

All components follow this state model:

| State | Characteristics |
| --- | --- |
| Idle | Low opacity, slow Current |
| Hover/Pre-focus | Slight halo, increased local contrast |
| Focused | Higher opacity, crisp edges, background de-emphasized |
| Active editing | Persistent anchors + snapping guides |
| Disabled | Dim + reduced motion (never remove structure) |
| Alert | Beacon/pulse, not flashing chaos |

## Dependency Rules

Components must be built in an order that respects their dependencies. Key constraints:

### Phase 1 Build Order

Within Phase 1, build in this sequence:

1. **Tokens** — Foundation for all components
2. **Typography** — Depends on Tokens
3. **CausticField** — Depends on Tokens (background layer)
4. **AnchorPoint** — Depends on Tokens (origin point)
5. **Beacon** — Depends on Tokens (simple indicator)
6. **Ripple** — Depends on Tokens (feedback primitive)
7. **MooringLine** — Depends on AnchorPoint + Tokens
8. **Buoy** — Depends on MooringLine (optional) + AnchorPoint + Tokens
9. **Lens** — Depends on Tokens + CausticField (optional)
10. **Horizon** — Depends on Tokens + Beacon
11. **TideRibbon** — Depends on Tokens + Beacon (optional)

### Cross-Phase Dependencies

| Component | Phase | Depends On | From Phase |
| --- | --- | --- | --- |
| **Orbit** | 2 | Frame | 2 |
| **Bearing** | 2 | Gauge | 2 |
| **FocusReticle** | 4 | Frame | 2 |
| **Flare** | 4 | Ripple | 1 |
| **ActionChip** | 4 | Typography, Ripple | 1 |
| **Prism** | 3 | Pane | 3 |
| **Dock** | 3 | Pane | 3 |
| **Shoals** | 5 | AnchorPoint, MooringLine, Frame | 1, 2 |
| **DriftGrid** | 5 | CausticField | 1 |
| **DataGrid** | 6 | FocusReticle | 4 |
| **SelectField** | 7 | TextField, Pane | 7, 3 |
| **DatePicker** | 7 | TextField, Pane, ActionChip | 7, 3, 4 |
| **Slider** | 7 | Gauge | 2 |
| **NavRail** | 8 | Beacon | 1 |
| **CommandBar** | 8 | ActionChip | 4 |
| **TabBar** | 8 | FocusReticle | 4 |
| **Shell** | 8 | NavRail, CommandBar, Breadcrumb, TabBar, Lens | 8, 1 |
| **FormPage** | 9 | Form components, Shell, Inlet | 7, 8 |
| **ListPage** | 9 | DataGrid, Shell, Reservoir | 6, 8 |
| **EntityDetailPage** | 9 | Lens, TideRibbon, Shell, Inlet, Reservoir | 1, 8 |
| **DashboardPage** | 9 | MetricCard, LineChart, DataGrid, Shell, Reservoir | 6, 8 |

### External Dependencies

| Component | External Dependency | Notes |
| --- | --- | --- |
| **FormPage** | Inlet | Mississippi command infrastructure |
| **ListPage** | Reservoir | Mississippi state subscription |
| **EntityDetailPage** | Inlet, Reservoir | Both command and state |
| **DashboardPage** | Reservoir | Live state subscription |

## Summary

The component roadmap defines 9 implementation phases, starting with a minimal viable set (Tokens, Typography, CausticField, AnchorPoint, Beacon, Ripple, MooringLine, Buoy, Lens, Horizon, TideRibbon) and expanding through navigation, surfaces, feedback, volumes, data visualization, forms, application shell, and stateful page components. Each component is classified by atomic design level and includes purpose, anatomy, states, motion specifications, and explicit dependencies to ensure correct build order.

## Next Steps

- Begin implementation with Phase 1 components.
- See the [Implementation Guide](./implementation.md) for tokens and CSS patterns.
- Review the [Design Language](./design-language.md) for holistic concepts.

