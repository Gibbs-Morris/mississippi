# Refraction Component Anatomy Sheet

Refraction is a holographic HUD design system for Blazor, inspired by sci-fi interfaces. This document catalogs each component's anatomy, states, parameters, and usage patterns.

## Design Principles

| Principle | Description |
|-----------|-------------|
| **No Walls, Only Depth** | Depth bands (near/mid/far) replace modal layering |
| **Void Canvas** | Near-black (#0A0C10) OLED-safe background |
| **Glass Planes** | Frosted translucent surfaces with edge glow |
| **Linework First** | Diagrams preferred over photos/icons |
| **Focus Reticle** | Universal selection indicator |
| **Radial Command** | Context actions orbit emitters |
| **Reserved Red** | Only for destructive/critical actions |

## Color Palette

### Neo Blue (5-step)

| Token | HSL | Usage |
|-------|-----|-------|
| `--rf-raw-neo-blue-n1` | `hsl(200, 100%, 80%)` | Brightest highlight |
| `--rf-raw-neo-blue-n2` | `hsl(200, 95%, 70%)` | Hover state |
| `--rf-raw-neo-blue-n3` | `hsl(200, 90%, 60%)` | Base accent |
| `--rf-raw-neo-blue-n4` | `hsl(200, 85%, 45%)` | Pressed state |
| `--rf-raw-neo-blue-n5` | `hsl(200, 80%, 30%)` | Darkest |

### Status Colors

| Token | Color | Usage |
|-------|-------|-------|
| `--rf-raw-amber` | Amber | Warning |
| `--rf-raw-red` | Red | Error/Destructive (reserved) |
| `--rf-raw-green` | Green | Success |

---

## Atoms

### Emitter

Origin point for materialize/dematerialize gestures and command reticle invocation.

#### Anatomy

| Part | Description |
|------|-------------|
| E0 Seed | Minimal idle indicator (dot) |
| E1 Ring | Expands when armed/busy |
| E2 Pulse | Subtle activity indicator |
| E3 Eject line | Indicates direction of materialization |
| E4 Mode glyph | Indicates palette state or power mode |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `State` | `string` | `"idle"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### Reticle

Universal focus/selection indicator.

#### Anatomy

| Part | Description |
|------|-------------|
| R0 Center dot | Current focus point |
| R1 Ring | Encircles target |
| R2 Snap ticks | Cardinal alignment cues |
| R3 Label arc | Optional text label |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Mode` | `string` | `"focus"` | Reticle mode (focus/select/command) |
| `State` | `string` | `"idle"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### ProgressArc

Arc-based progress indicator.

#### Anatomy

| Part | Description |
|------|-------------|
| P0 Track | Faint background arc (0-360°) |
| P1 Fill arc | Animated progress stroke |
| P2 Endpoint | Optional glyph at current position |
| P3 Center label | Percentage or status text |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Value` | `double` | `0` | Current value |
| `Min` | `double` | `0` | Minimum value |
| `Max` | `double` | `100` | Maximum value |
| `State` | `string` | `"determinate"` | State (determinate/indeterminate/complete) |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### InputField

Instrument-style input with HUD aesthetics.

#### Anatomy

| Part | Description |
|------|-------------|
| I0 Label | Upper-left descriptor |
| I1 Field | Monospace input area |
| I2 Constraint tick | Optional unit or format indicator |
| I3 Error slot | Appears below on invalid state |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Id` | `string` | `""` | Input id |
| `Label` | `string?` | `null` | Label text |
| `Type` | `string` | `"text"` | Input type |
| `Value` | `string` | `""` | Current value |
| `Placeholder` | `string?` | `null` | Placeholder text |
| `IsDisabled` | `bool` | `false` | Whether input is disabled |
| `IsReadOnly` | `bool` | `false` | Whether input is read-only |
| `State` | `string` | `"idle"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### NotificationPulse

Non-intrusive alert indicator.

#### Anatomy

| Part | Description |
|------|-------------|
| N0 Dot | Small attention indicator |
| N1 Badge count | Optional numeric badge |
| N2 Preview tick | Optional one-line summary |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | `null` | Child content |
| `State` | `string` | `"new"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### CalloutLine

Anchors labels to schematic elements.

#### Anatomy

| Part | Description |
|------|-------------|
| C0 Anchor dot | Attaches to target element |
| C1 Leader line | Angled segment to label |
| C2 Horizontal segment | Runs parallel to label |
| C3 Label | Text/badge block |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Label` | `string?` | `null` | Label text |
| `State` | `string` | `"idle"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

## Molecules

### TelemetryStrip

Read-only status display strip.

#### Anatomy

| Part | Description |
|------|-------------|
| T0 Strip container | Horizontal bar |
| T1 Segment slots | Multiple cells (label + value pairs) |
| T2 Dividers | Optional separator marks |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | `null` | Child content (segments) |
| `State` | `string` | `"quiet"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### CommandOrbit

Radial action menu around an emitter.

#### Anatomy

| Part | Description |
|------|-------------|
| O0 Center anchor | Origin point (reticle/emitter) |
| O1 Orbit ring | Circular path for actions |
| O2 Action nodes | 2-8 action items on ring |
| O3 Sector highlight | Indicates current selection |
| O4 Tooltip arc | Text near selected node |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | `null` | Child content (action items) |
| `State` | `string` | `"latent"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

## Organisms

### Pane

Primary content surface (Glass Plane).

#### Anatomy

| Part | Description |
|------|-------------|
| G0 Glass surface | Frosted translucent background |
| G1 Header bar | Title + meta strip |
| G2 Content well | Scrollable interior |
| G3 Footer slot | Optional action row |
| G4 Edge glow | Focus indicator |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string?` | `null` | Pane title |
| `Variant` | `string` | `"primary"` | Pane variant |
| `Depth` | `string` | `"mid"` | Depth band |
| `State` | `string` | `"idle"` | Current component state |
| `ChildContent` | `RenderFragment?` | `null` | Main content |
| `Footer` | `RenderFragment?` | `null` | Footer content |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### SchematicViewer

Primary representation for complex objects using linework.

#### Anatomy

| Part | Description |
|------|-------------|
| S0 Viewport | Zoom/pan surface |
| S1 Grid/reticle | Very faint, not always on |
| S2 Entities | Linework layers |
| S3 Selection overlay | Reticle + highlight ring |
| S4 Callouts | Anchored labels |
| S5 Depth layers | Near/mid/far information staging |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | `null` | Viewport content |
| `Caption` | `string?` | `null` | Caption text |
| `State` | `string` | `"idle"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

### SmokeConfirm

Rare occlusion surface for destructive confirmation only.

#### Anatomy

| Part | Description |
|------|-------------|
| D0 Smoke veil | Low opacity backdrop |
| D1 Confirm pane | Small glass plane |
| D2 Action pair | Confirm/cancel as reticle-selectable nodes |
| D3 Consequence line | One-line explanation only |

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string?` | `null` | Dialog title |
| `Consequence` | `string?` | `null` | Consequence description |
| `ConfirmText` | `string` | `"Confirm"` | Confirm button text |
| `CancelText` | `string` | `"Cancel"` | Cancel button text |
| `OnConfirm` | `EventCallback` | - | Callback when confirmed |
| `OnCancel` | `EventCallback` | - | Callback when cancelled |
| `State` | `string` | `"latent"` | Current component state |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Additional HTML attributes |

---

## Component States

All components support the following states via the `State` parameter:

| State | Usage |
|-------|-------|
| `idle` | Default resting state |
| `active` | Accepting input |
| `focused` | Has keyboard focus |
| `locked` | Frozen during transition |
| `error` | Error condition |
| `busy` | Processing |
| `armed` | Ready for action |
| `dormant` | Present but inactive |
| `tracking` | Following movement |
| `critical` | Critical attention needed |
| `latent` | Hidden until intent |
| `pinned` | Forced open |
| `expanded` | Expanded view |
| `disabled` | Cannot interact |

## Depth Bands

| Band | Usage |
|------|-------|
| `near` | Interaction and focus elements |
| `mid` | Primary content |
| `far` | Context and telemetry |

---

## Usage Examples

### Basic Pane

```razor
<Pane Title="System Status" Variant="@RefractionPaneVariants.Primary">
    <p>All systems operational.</p>
</Pane>
```

### Input with Error State

```razor
<InputField
    Id="target-coordinates"
    Label="Target Coordinates"
    Placeholder="0.000, 0.000"
    State="@RefractionStates.Invalid" />
```

### Confirmation Dialog

```razor
<SmokeConfirm
    Title="Confirm Deletion"
    Consequence="This action cannot be undone."
    ConfirmText="Delete"
    CancelText="Cancel"
    State="@RefractionStates.Expanded"
    OnConfirm="@HandleDelete"
    OnCancel="@HandleCancel" />
```
