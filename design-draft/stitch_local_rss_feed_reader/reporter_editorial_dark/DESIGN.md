---
name: Reporter Editorial Dark
colors:
  surface: '#0f131c'
  surface-dim: '#0f131c'
  surface-bright: '#353942'
  surface-container-lowest: '#0a0e16'
  surface-container-low: '#181c24'
  surface-container: '#1c2028'
  surface-container-high: '#262a33'
  surface-container-highest: '#31353e'
  on-surface: '#dfe2ee'
  on-surface-variant: '#d8c3ad'
  inverse-surface: '#dfe2ee'
  inverse-on-surface: '#2c3039'
  outline: '#a08e7a'
  outline-variant: '#534434'
  surface-tint: '#ffb95f'
  primary: '#ffc174'
  on-primary: '#472a00'
  primary-container: '#f59e0b'
  on-primary-container: '#613b00'
  inverse-primary: '#855300'
  secondary: '#4edea3'
  on-secondary: '#003824'
  secondary-container: '#00a572'
  on-secondary-container: '#00311f'
  tertiary: '#ffbbbe'
  on-tertiary: '#67001b'
  tertiary-container: '#ff919a'
  on-tertiary-container: '#8c0028'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffddb8'
  primary-fixed-dim: '#ffb95f'
  on-primary-fixed: '#2a1700'
  on-primary-fixed-variant: '#653e00'
  secondary-fixed: '#6ffbbe'
  secondary-fixed-dim: '#4edea3'
  on-secondary-fixed: '#002113'
  on-secondary-fixed-variant: '#005236'
  tertiary-fixed: '#ffdadb'
  tertiary-fixed-dim: '#ffb2b7'
  on-tertiary-fixed: '#40000d'
  on-tertiary-fixed-variant: '#92002a'
  background: '#0f131c'
  on-background: '#dfe2ee'
  surface-variant: '#31353e'
typography:
  display:
    fontFamily: Newsreader
    fontSize: 44px
    fontWeight: '400'
    lineHeight: 52px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Newsreader
    fontSize: 36px
    fontWeight: '500'
    lineHeight: 44px
    letterSpacing: -0.015em
  headline-lg-mobile:
    fontFamily: Newsreader
    fontSize: 28px
    fontWeight: '500'
    lineHeight: 36px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Newsreader
    fontSize: 26px
    fontWeight: '500'
    lineHeight: 34px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Newsreader
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  lead:
    fontFamily: Newsreader
    fontSize: 22px
    fontWeight: '400'
    lineHeight: 32px
    letterSpacing: -0.005em
  body-lg:
    fontFamily: Newsreader
    fontSize: 19px
    fontWeight: '400'
    lineHeight: 32px
  body-md:
    fontFamily: Inter
    fontSize: 15px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
  ui-label-lg:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.01em
  ui-label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
  ui-label-mono:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.05em
  caption:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  reading-measure: 68ch
  gutter-mobile: 1rem
  gutter-tablet: 1.5rem
  gutter-desktop: 2rem
  sidebar-width: 18rem
  stream-width: 26rem
  space-2xs: 0.25rem
  space-xs: 0.5rem
  space-sm: 0.75rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
  space-2xl: 3rem
  space-3xl: 4.5rem
---
<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->


## Brand & Style

This design system is engineered for the deep, focused reader—journalists, researchers, and dedicated news consumers parsing high-density feeds during late-night and low-light sessions. The aesthetic combines the time-honored typography of classical print broadsheets with the quiet, functional precision of modern developer-grade dark environments.

The visual direction merges **Editorial Minimalism** with **Tonal Surface Layering**. Instead of relying on pure pitch black, the system uses deep slate and ink-tinted charcoal tones to reduce ocular fatigue, preserve visual depth, and eliminate high-contrast glare. Warm amber accents invoke the sensation of a shaded library lamp, while hairline borders structure content with architectural clarity. The interface steps back, prioritizing legibility, pacing, and uninterrupted textual immersion.

## Colors

The color palette is calibrated specifically for nighttime consumption, balancing rich darkness against focused typographic contrast:

- **Canvas & Underlays**:
  - Base Background: `#0b0f17` (Deep Obsidian Slate)
  - Surface Default: `#111827` (Charcoal Sheet)
  - Surface Raised: `#182234` (Ink Surface Container)
  - Surface Hover / Focus: `#1f293d` (Illuminated Dark Container)
  - Surface Selected / Modal: `#273549` (Elevated Reading Plane)

- **Typography & Content Hierarchy**:
  - Primary Text: `#f1f5f9` (Crisp Off-White, 88% optical brightness to avert blooming against dark backdrops)
  - Secondary Text: `#cbd5e1` (Soft Slate, for deck copy and lead summaries)
  - Muted Text & Metadata: `#94a3b8` (Muted Silver, for timestamps, sources, and bylines)
  - Disabled / Whispered: `#475569` (Subdued Charcoal, for passive counters and dividers)

- **Accents & States**:
  - Primary Accent: `#f59e0b` (Warm Amber, for bookmarks, active reading cursors, and unread indicator lights)
  - Accent Subtle: `#451a03` (Deep Amber Wash, for active row backgrounds)
  - Success / Synced: `#10b981` (Emerald Green, for feed connectivity and fully-read state confirmations)
  - Error / Attention: `#f43f5e` (Crimson Rose, for broken feeds, parse warnings, and failed syncs)
  - Structural Border: `#1e293b` (Subtle boundary) and `#334155` (Crisp separation rule)

## Typography

The type system is divided intentionally between literary reading immersion and utilitarian app chrome:

- **Editorial Body & Headlines (`Newsreader`)**: Applied across long-form article rendering, article cards, headlines, and pull quotes. Newsreader delivers variable optical sizes, warm serifs, and high rhythm in justified or ragged reading contexts. The article body uses a roomy 1.68 line-height (`32px` line height on `19px` font size) to ensure sustained visual tracking in pitch-dark environments.
- **Interface & Mechanics (`Inter`)**: Deployed for feed lists, source filters, buttons, menus, tooltips, and system notifications. The neutral grotesque aesthetic prevents visual competition with the headline serif.
- **Data & Telemetry (`JetBrains Mono`)**: Utilized selectively for RSS synchronization timestamps, unread counts, status codes, and keyboard navigation indicators.

## Layout & Spacing

The reading experience employs a responsive, column-structured workspace that transitions from a single distraction-free column to a classical 3-pane editorial desk:

- **Desktop (1280px+)**: A balanced 3-pane architecture:
  1. *Feed Navigator*: `18rem` fixed sidebar for subscriptions, folders, and tags.
  2. *Story Stream*: `26rem` list view displaying incoming articles, summaries, and unread stamps.
  3. *Reading Chamber*: Flexible main reading column constrained to a maximum prose measure of `68ch` centered within the canvas, surrounded by generous breath margins to avoid edge fatigue.
- **Tablet (768px - 1279px)**: Dual pane featuring collapsible feed navigation drawer, visible story stream, and full-fidelity reader view.
- **Mobile (< 768px)**: Strict stack flow using stacked full-width panels (Feeds → List → Story) with fixed `1rem` screen margin and anchored bottom bar navigation.

Vertical rhythm relies strictly on a `0.25rem` (4px) base scale, pairing tight groupings (`space-xs`, `space-sm`) inside metadata modules with open cadence (`space-xl`, `space-2xl`) separating major editorial sections.

## Elevation & Depth

To prevent optical blur and "muddy" contrast in dark rooms, this design system rejects heavy drop shadows in favor of **Tonal Stratification** accompanied by **Subtle Hairline Outlines**:

- **Ground Level (Base Canvas `#0b0f17`)**: The lowest plane. Hosts feed backdrops and canvas rails.
- **Layer 1 (Card & Stream Level `#111827`)**: Feed entries and listing rows rest here, framed by a `1px` crisp border in `#1e293b`.
- **Layer 2 (Reading Plane & Hover States `#182234`)**: Active reading panes, floating sidebars, and hovered list tiles. Border brightens slightly to `#334155`.
- **Layer 3 (Modals, Context Menus, and Palettes `#1f293d`)**: Higher surface plane with a faint 1px border `#334155` and a soft ambient presence: `0 16px 32px -8px rgba(0, 0, 0, 0.65)`.
- **Amber Glow Signaling**: Active states or key landmarks (such as an unread pip or active article indicator) cast an ultra-soft, micro-diffused ambient glow: `0 0 12px rgba(245, 158, 11, 0.25)`.

## Shapes

The interface embraces a structured, architectural geometry with restrained roundedness (`0.25rem` / `4px` default radius) reminiscent of cut newsprint blocks and precision instruments.

- **Base Radius (`0.25rem`)**: Default for buttons, inputs, badge indicators, chips, and list selections.
- **Structural Containers (`rounded-lg` / `0.5rem`)**: Used on reader panes, floating popovers, command palettes, and modal windows.
- **Pill Exceptions**: Small unread counters, tags, and status dots leverage full roundedness (`rounded-full`) exclusively to differentiate status metadata from structural containers.
- **Hairlines**: All interactive and framing borders must remain strictly `1px` solid to maintain crisp definition on high-density displays.

## Components

### Buttons
- **Primary**: Solid Amber `#f59e0b` fill with `#0b0f17` bold typography (`ui-label-lg`). Hover shifts to `#d97706`. Focus ring is `2px` offset with `#f59e0b`.
- **Secondary / Ghost**: Transparent fill with `1px` `#334155` border, text in `#f1f5f9`. On hover, background renders `#182234` with border transitioning to `#94a3b8`.
- **Icon Actions**: Subtle square bounding boxes (`32px` or `36px`), transparent fill, text `#94a3b8`. On hover: `#182234` with `#f59e0b` icon tint.

### Chips & Source Tags
- Background `#182234`, border `1px` `#1e293b`, typography `ui-label-mono` in `#94a3b8`.
- Active/Filter selected: `#451a03` background, `#f59e0b` text, `1px` solid border `#f59e0b`.

### Feeds & Article Stream List
- List items feature no drop shadow; they sit edge-to-edge separated by `1px` hairline rules in `#1e293b`.
- **Unread Item**: Headline rendered in `Newsreader` SemiBold `#f1f5f9`, paired with a `6px` circular `#f59e0b` pip.
- **Read Item**: Headline shifts to muted `Newsreader` Regular `#94a3b8`; unread pip resolves to transparent or a soft `#10b981` subtle ring.
- **Active Selection**: Left border marker (`2px` solid `#f59e0b`), background `#182234`.

### Reader View (Editorial Article Canvas)
- **Header**: Primary publication source in `ui-label-mono` (`#f59e0b`), followed by an editorial display title (`Newsreader` Display, `#f1f5f9`), followed by byline and date metadata in `ui-label-md` (`#94a3b8`).
- **Body**: Contained to `68ch` width. Paragraphs in `Newsreader` Body-LG (`#f1f5f9`). Blockquotes carry a `2px` left border in `#f59e0b` with italicized text in `#cbd5e1`. Links styled in `#f59e0b` with a subtle dashed underline.

### Inputs & Command Palette
- Input background is `#111827`, border `1px` `#334155`, text `#f1f5f9`, placeholder text `#475569`.
- Active focus state: border shifts to `#f59e0b` with no thick outer glow, only a hairline focus ring.

### Status Indicators
- **Syncing / Healthy**: Small dot in `#10b981`.
- **Warning / Stale Feed**: Small dot in `#f59e0b`.
- **Sync Failure / Offline**: Small dot in `#f43f5e`.