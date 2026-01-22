# Refraction Design Language Specification

**Version:** 0.1
**Targets:** Mobile (touch), Web (touch + non-touch), Desktop browsers, OLED-centric displays

---

## 1. North Star

### 1.1 Intent

Build a UI that behaves like a **holographic instrument HUD**:

* **Task-first** (get something done fast)
* **Content-first** (chrome is minimal and transient)
* **Spatial** (depth and anchoring replace "pages")
* **Emanating** (UI "materializes" from a single emitter point)

### 1.2 Non-goals

* Not a "web app with a sci-fi skin"
* Not component-dense dashboards
* Not persistent navigation furniture

---

## 2. Reference Aesthetic Constraints

### 2.1 Diegetic anchor: the Emitter

Treat the UI as projected/emitted—hence an origin point for UI birth/death.

### 2.2 Adaptive interface premise

Personalization is **adaptive density, sizing, and emphasis** within strict constraints.

### 2.3 Transparency as a real constraint

The UI must remain legible on visually noisy backgrounds and through transparency.

---

## 3. Principles

1. **One task → one primary pane.** Secondary panes are contextual and dismissible.
2. **Three depth bands max** on screen at once:

   * Near (interaction + focus)
   * Mid (content)
   * Far (context/telemetry)
3. **Neo-blue is primary.** Any other color must justify itself as semantics (alert, critical).
4. **Lines over blocks.** Prefer stroke, glow, and negative space to filled rectangles.
5. **Motion explains.** Open/close/confirm/error are distinct.
6. **Input-agnostic.** Everything works with keyboard + pointer, without touch.
7. **OLED-safe default.** Avoid static bright elements and provide mitigation.

---

## 4. Foundations

### 4.1 Material model

UI "materials" implementable in CSS/WebGL/native:

#### M0 — Void

* Near-black field enabling emissive readability.
* Avoid large pure-white regions.

#### M1 — Glass Plane

* Transparent plane with:

  * edge line (1–2px equivalent)
  * faint inner haze (very low opacity)
  * subtle outer glow bleed

#### M2 — Emissive Ink

* Strokes, text, ticks, outlines.
* No filled buttons by default; selection uses **reticle + glow intensification**.

#### M3 — Smoke (rare)

* Only for safety-critical occlusion (destructive confirmation).
* Never used for normal navigation.

---

### 4.2 Color system

#### Palette

* **Neo Blue (primary hue):** 5 luminance steps (N1–N5)
* **Neutral Ink:** 3 steps (for long-form text when needed)
* **Alert Amber:** 2 steps
* **Critical Red:** 2 steps

#### Rules

* **90% of UI** uses Neo Blue + Neutral.
* Amber/Red are **events**, not themes.

#### Semantic mapping

* Neo Blue: active, selectable, normal focus
* Neutral: passive labels, metadata, long content
* Amber: caution / waiting / attention
* Red: error / destructive / dangerous state

---

### 4.3 Typography

#### Type roles

* **Human (UI copy):** readable, minimal decoration
* **Instrument (telemetry):** numeric clarity, tabular alignment

#### Typography rules

* Prefer short labels and structured fragments, not paragraphs.
* Use alignment rails: numbers align vertically; units are secondary.

---

### 4.4 Iconography

* Stroke-only icon set
* Two stroke weights:

  * normal
  * emphasis (focus/active)
* No filled glyphs unless critical.

---

### 4.5 Layout model

#### The "Scene" is the unit of navigation

A Scene is a stable task context composed of:

* Primary Pane (content)
* Telemetry Strip (optional)
* Contextual Orbitals (optional, transient)

Navigation is:

* scene-to-scene transitions (materialize/dematerialize)
* command reticle invocation (actions without chrome)

---

## 5. Motion Language

### 5.1 The Emitter

Default emitter location:

* **Mobile:** bottom-center (thumb zone)
* **Desktop:** bottom-center OR anchored to focused reticle (configurable)
* **Accessibility override:** may relocate to avoid obscuring content

---

### 5.2 Core transitions

#### Pane Materialize (open)

1. Seed appears at emitter (dot)
2. Reticle ring expands
3. Plane resolves upward
4. Content draws on (staggered by groups)

#### Pane Dematerialize (close)

1. Content drains (fade + retract)
2. Plane collapses toward emitter
3. Reticle snaps shut
4. Seed extinguishes

---

### 5.3 Meaningful motion mapping

* **Confirm:** inward snap + brief brightening
* **Error:** fracture jitter + red pulse + retract
* **Loading:** rotating arc in far band (never blocks primary content)

---

### 5.4 Reduced motion

Provide a mode that replaces travel with:

* fade + instant positioning
* no jitter, no large-scale movement

---

## 6. Interaction Model

### 6.1 Universal interaction rules

All actions exist in three forms:

* Direct manipulation (touch/pointer)
* Reticle command (radial)
* Keyboard command palette

---

### 6.2 Focus system (non-touch)

#### Reticle Focus Indicator

* Focus is always visible.
* Focus indicator must be high-contrast and not rely on color alone.

Design: neon-blue reticle bracket that "locks" onto the focused element; critical focus becomes a double ring.

---

### 6.3 Touch gestures

* Swipe down toward emitter: collapse pane
* Drag reticle: reposition orbital
* Two-finger pinch: schematic zoom

---

### 6.4 Mouse/trackpad

* Hover reveals latent affordances (handles, anchors)
* Scroll wheel:

  * normal: scroll content
  * with modifier: move between depth bands

---

### 6.5 Keyboard

* `Tab` cycles within depth band
* `Ctrl/Cmd+K` opens command palette (reticle mode)
* `Esc` collapses transient orbitals → then primary pane → then returns to Scene baseline

---

## 7. Components

### 7.1 Emitter

* Always present; nearly invisible when idle.
* States: idle / armed / busy / error

### 7.2 Pane (Glass Plane)

* Anatomy: edge, header tick, content field, anchor points
* States: dormant, active, focused, locked, error

### 7.3 Reticle

* Used for: focus, selection, targeting, drag handles
* Types: focus reticle, selection reticle, command reticle

### 7.4 Telemetry Strip (optional)

* Far band only
* Shows: status, time, connection, progress arcs
* Must never become a "top nav"

### 7.5 Schematic Viewer

* Default representation for complex objects
* Uses linework layers; labels are callouts, not paragraphs

### 7.6 Notification Pulse

* Appears as a small pulse on an edge rail (not toast stacks)
* Expands to a pane only on intent (tap/enter)

---

## 8. Patterns

### 8.1 Task Scene pattern

Scene baseline: minimal telemetry + empty space
User intent: opens one primary pane
Secondary info: orbitals anchored to primary content

---

### 8.2 Progressive disclosure (anti-busyness rule)

Default: show only the next required decision.
Other controls remain latent until:

* hover/focus
* press-and-hold
* command reticle invocation

---

### 8.3 Error handling

* Prefer prevent over recover
* On error:

  * keep content visible
  * highlight the exact field with reticle fracture + red pulse
  * provide a single corrective action first

---

## 9. Accessibility Requirements

1. Text contrast meets WCAG guidance (e.g., 4.5:1 for normal text).
2. Focus is visible and meets focus appearance requirements.
3. Keyboard-only completion of every task.
4. Reduced motion option (system preference aware).
5. Screen reader plan:

   * semantic DOM for text/actions even if visuals are canvas/WebGL.

---

## 10. OLED Safety

### 10.1 Required behaviors

* Idle dimming: reduce emissive intensity after inactivity.
* Micro-variation: subtle noise/pulse shifting in long-lived lines (low amplitude).
* Pixel drift (software): tiny sub-pixel translation over time for persistent HUD elements (recommended).
* Avoid static bright logos/time readouts fixed for long durations.
* "Presentation mode" toggle to increase brightness temporarily.

---

## 11. Implementation Guidance

### 11.1 Rendering approach

Hybrid rendering recommended:

* DOM for text/accessibility/focus order
* Canvas/WebGL/SVG for strokes, glows, schematics

### 11.2 Token system

Two layers:

* Raw tokens: `hue.neoBlue`, `alpha.glass`, `glow.radius`
* Semantic tokens: `color.action.primary`, `surface.pane`, `focus.reticle`

### 11.3 Performance budgets

* Target 60fps for motion
* Cap simultaneous glow layers
* Provide "low power" mode for mobile/OLED longevity
