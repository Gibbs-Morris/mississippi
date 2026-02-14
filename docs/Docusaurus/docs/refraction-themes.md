# Refraction Themes

Two visual themes shipped with the Refraction UX framework. Both themes share the same component set; the difference is the **feel**, **experience**, and **behavioral posture** expressed through theming.

---

## Refraction Luminous (2300)

### Theme statement
A luminous, spatial interface built from translucent "window" surfaces and controlled light—designed to feel like operating a high-precision system in a dark environment.

### Goal
Deliver a **cinematic, opinionated** experience that remains **high-signal and readable**: blue light defines identity and focus; darkness behaves as depth and transparency; a single warm accent is reserved for warnings and critical attention.

### Experience promise
- **Dark as space, not absence**: layered translucency and depth create a sense of operating "within" the UI, not merely looking at it.
- **Blue as the operating wavelength**: structure, focus, and system "energy" are expressed through a coherent blue spectrum.
- **Warm accent as meaning**: the warm channel is scarce; it means warning, risk, or urgent exception—never decoration.
- **Motion as explanation**: movement preserves continuity and clarifies cause→effect; it never exists to entertain. (https://www.sciencedirect.com/science/article/abs/pii/B9781558609150500159)

### Design principles
- **Instrument first, spectacle second**: dramatic aesthetics are permitted only when they increase clarity.
- **Legibility over atmosphere**: text always lands on surfaces that actively protect contrast (blur/scrim, simplified backdrops, or solidification when needed). (https://www.nngroup.com/articles/glassmorphism/)
- **Multiple signifiers**: state, interactivity, and severity are communicated with shape/weight/iconography in addition to color. (https://www.w3.org/WAI/WCAG21/Understanding/use-of-color.html)
- **Adaptive comfort**: the experience includes a low-motion path and a higher-contrast / lower-transparency path for users who need it. (https://www.w3.org/WAI/WCAG22/Understanding/animation-from-interactions.html)
- **Attention is managed deliberately**: the interface stays quiet until engaged; focus "energizes" components in a controlled gradient rather than abrupt jumps.

### Behavioral feel
- **Motion temperament**: smooth and restrained; transitions preserve orientation and relationship (where it came from, where it went). Non-essential motion can be reduced/disabled. (https://www.w3.org/WAI/WCAG22/Understanding/animation-from-interactions.html)
- **Surface behavior**: translucent layers intensify on focus/interaction; reading-heavy content uses "protected" surfaces that prioritize legibility over transparency. (https://www.nngroup.com/articles/glassmorphism/)
- **Status semantics**: warnings are unmistakable, time-bounded, and accompanied by non-color cues.

### Range: phone → 8K (how it scales)
- **Small screens (touch-first)**: prioritize clarity and hit-target ergonomics; reduce simultaneous layers; simplify/strengthen text backplates so translucency never harms legibility.
- **Medium screens (laptop/desktop)**: restore depth cues and layered composition; keep "quiet by default" so dense work remains calm.
- **Large/8K displays**: avoid "empty oceans" and excessively long line lengths; use structured spacing, sensible maximum content widths, and hierarchical grouping so the interface still reads as an instrument panel rather than a billboard.
- **Performance posture across devices**: allow translucency/blur intensity to adapt to device capability; preserve the *same* theme identity even when effects are reduced.

### Non-goals
- Not a default for reading-heavy, office-bright workflows (it may be offered, but not forced). (https://www.nngroup.com/articles/dark-mode/)
- Not a "full-time glow show": glow is a signifier, not a constant aesthetic blanket.

### Success criteria
- Feels futuristic and opinionated **without sacrificing legibility** in variable backgrounds. (https://www.nngroup.com/articles/glassmorphism/)
- Motion improves understanding and can be reduced for sensitivity. (https://www.sciencedirect.com/science/article/abs/pii/B9781558609150500159)
- Warning accent retains high semantic value (rare, serious, unmissable).
- Works credibly from mobile to ultra-high-resolution displays without changing the underlying experience promise.

---

## Refraction Enterprise (2025)

### Theme statement
A neutral, trustworthy foundation theme optimized for predictable enterprise workflows—clean, conventional, and highly adaptable to brand guidelines without fragmenting the product.

### Goal
Provide a **stable default** that scales across teams and products: familiar patterns, clear signifiers, strong accessibility posture, and many safe customization points that preserve coherence.

### Experience promise
- **Predictability**: users can rely on established conventions and consistent behaviors.
- **Clarity**: hierarchy is carried by typography, spacing, and contrast—not special effects.
- **Brand-fit**: identity is expressed through controlled knobs (color, type, shape, density, emphasis), not bespoke component rework. (https://www.w3.org/community/design-tokens/2025/10/28/design-tokens-specification-reaches-first-stable-version/)
- **Operational confidence**: states, validation, and system feedback are explicit, actionable, and calm.

### Design principles
- **Conformity with user expectations**: interaction patterns and component behaviors prioritize convention and consistency. (https://cdn.standards.iteh.ai/samples/75258/83c8cf072187487686645aad04eff40e/ISO-9241-110-2020.pdf)
- **Self-descriptive interfaces**: controls communicate what they are and what will happen—no hidden affordances. (https://cdn.standards.iteh.ai/samples/75258/83c8cf072187487686645aad04eff40e/ISO-9241-110-2020.pdf)
- **Minimal, not ambiguous**: simplify without removing signifiers; clickable vs non-clickable must be obvious. (https://www.nngroup.com/articles/flat-design-best-practices/)
- **Error robustness**: mistakes are recoverable; feedback is specific and constructive. (https://cdn.standards.iteh.ai/samples/75258/83c8cf072187487686645aad04eff40e/ISO-9241-110-2020.pdf)
- **Accessibility by design**: contrast and non-color cues are foundational, not optional. (https://www.w3.org/TR/WCAG22/)

### Behavioral feel
- **Motion temperament**: subtle and utilitarian; preserves context and confirms outcomes; avoid motion that competes with data work. Respect reduced-motion preferences.
- **Surface behavior**: primarily solid/opaque surfaces for consistent legibility; translucency (if any) is rare and always protected by contrast rules. (https://www.nngroup.com/articles/glassmorphism/)
- **Density posture**: defaults to comfortable readability; supports compact variants for expert/data-heavy work when needed (without changing the underlying visual logic). (https://cdn.standards.iteh.ai/samples/75258/83c8cf072187487686645aad04eff40e/ISO-9241-110-2020.pdf)

### Range: phone → 8K (how it scales)
- **Small screens (touch-first)**: prioritize a single primary action per view, clear hierarchy, and frictionless navigation; keep layouts shallow and predictable.
- **Medium screens (laptop/desktop)**: support data-heavy workflows with stable scanning patterns, consistent spacing rhythm, and clear state signifiers.
- **Large/8K displays**: preserve readability via maximum line lengths and structured multi-column composition; scale density deliberately rather than simply enlarging everything.
- **Input modality consistency**: behavior remains predictable across touch, keyboard, mouse, and assistive tech—no "special cases" that surprise users.

### Non-goals
- Not a fashion statement; avoid stylings that age quickly or reduce affordance clarity. (https://www.nngroup.com/articles/flat-design-best-practices/)
- Not "one-off brand skins"; customization must remain within guardrails to preserve consistency. (https://www.w3.org/community/design-tokens/2025/10/28/design-tokens-specification-reaches-first-stable-version/)

### Success criteria
- Fits common enterprise brand constraints with minimal friction while staying coherent at scale. (https://www.w3.org/community/design-tokens/2025/10/28/design-tokens-specification-reaches-first-stable-version/)
- Maintains predictability, consistency, and recoverability across the entire component set. (https://cdn.standards.iteh.ai/samples/75258/83c8cf072187487686645aad04eff40e/ISO-9241-110-2020.pdf)
- Meets baseline accessibility expectations for contrast and non-color communication. (https://www.w3.org/TR/WCAG22/)
- Works from mobile to ultra-high-resolution displays without diverging into separate "products."
