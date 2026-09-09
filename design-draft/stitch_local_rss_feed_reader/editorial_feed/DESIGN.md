---
name: Editorial Feed
colors:
  surface: '#f7f9fb'
  surface-dim: '#d8dadc'
  surface-bright: '#f7f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#eceef0'
  surface-container-high: '#e6e8ea'
  surface-container-highest: '#e0e3e5'
  on-surface: '#191c1e'
  on-surface-variant: '#45474c'
  inverse-surface: '#2d3133'
  inverse-on-surface: '#eff1f3'
  outline: '#75777d'
  outline-variant: '#c5c6cd'
  surface-tint: '#545f73'
  primary: '#091426'
  on-primary: '#ffffff'
  primary-container: '#1e293b'
  on-primary-container: '#8590a6'
  inverse-primary: '#bcc7de'
  secondary: '#0051d5'
  on-secondary: '#ffffff'
  secondary-container: '#316bf3'
  on-secondary-container: '#fefcff'
  tertiary: '#240f00'
  on-tertiary: '#ffffff'
  tertiary-container: '#422000'
  on-tertiary-container: '#d97705'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d8e3fb'
  primary-fixed-dim: '#bcc7de'
  on-primary-fixed: '#111c2d'
  on-primary-fixed-variant: '#3c475a'
  secondary-fixed: '#dbe1ff'
  secondary-fixed-dim: '#b4c5ff'
  on-secondary-fixed: '#00174b'
  on-secondary-fixed-variant: '#003ea8'
  tertiary-fixed: '#ffdcc3'
  tertiary-fixed-dim: '#ffb77d'
  on-tertiary-fixed: '#2f1500'
  on-tertiary-fixed-variant: '#6e3900'
  background: '#f7f9fb'
  on-background: '#191c1e'
  surface-variant: '#e0e3e5'
  paper-base: '#F9F9FB'
  surface-card: '#FFFFFF'
  surface-subtle: '#F1F5F9'
  text-primary: '#0F172A'
  text-secondary: '#64748B'
  text-muted: '#94A3B8'
  border-subtle: '#E2E8F0'
  border-hairline: rgba(15, 23, 42, 0.06)
  status-ok: '#10B981'
  status-warning: '#F59E0B'
  status-error: '#EF4444'
  bookmark-gold: '#D97706'
typography:
  display-lg:
    fontFamily: Newsreader
    fontSize: 40px
    fontWeight: '600'
    lineHeight: 48px
  display-lg-mobile:
    fontFamily: Newsreader
    fontSize: 30px
    fontWeight: '600'
    lineHeight: 38px
  headline-lg:
    fontFamily: Newsreader
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
  headline-lg-mobile:
    fontFamily: Newsreader
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 30px
  headline-md:
    fontFamily: Newsreader
    fontSize: 20px
    fontWeight: '500'
    lineHeight: 28px
  headline-sm:
    fontFamily: Newsreader
    fontSize: 17px
    fontWeight: '500'
    lineHeight: 24px
  body-reading:
    fontFamily: Newsreader
    fontSize: 19px
    fontWeight: '400'
    lineHeight: 32px
  body-reading-mobile:
    fontFamily: Newsreader
    fontSize: 17px
    fontWeight: '400'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  label-lg:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 18px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  label-sm:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
  label-meta:
    fontFamily: Inter
    fontSize: 10px
    fontWeight: '600'
    lineHeight: 12px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  spacing-2xs: 0.25rem
  spacing-xs: 0.5rem
  spacing-sm: 0.75rem
  spacing-md: 1rem
  spacing-lg: 1.25rem
  spacing-xl: 1.5rem
  spacing-2xl: 2rem
  spacing-3xl: 3rem
  margin-mobile: 1rem
  margin-tablet: 1.5rem
  margin-desktop: 2.5rem
  gutter-stream: 0.75rem
  reader-max-width: 42rem
---

## Brand & Style

The design system embodies a tranquil, distraction-free editorial experience inspired by modern native iOS reading environments like Apple News, NetNewsWire, and Reeder. It bridges literary tradition and purposeful utility: the warmth of tactile print paired with the precision of an Apple-native interface.

The target audience consists of avid readers, journalists, researchers, and information curators who value typographic clarity, rapid feed triage, and mindful consumption. The UI evokes focus, calm, and authority—eliminating algorithmic noise in favor of reader sovereignty.

The design movement combines **Minimalism** with an **Editorial iOS Native** aesthetic:
- **Surface Serenity:** Tinted warm paper tones prevent eye strain and frame typography as the primary visual architecture.
- **Typographic Dialectic:** High-literary serif display typography pairs with utilitarian, micro-spaced geometric sans-serif for metadata, controls, and system state.
- **Physical Cadence:** Subtle hair-thin dividers, translucent floating control bars, and fluid tactile interactions that feel rooted directly within the iOS Human Interface ecosystem.

## Colors

The color palette establishes an environment optimized for sustained reading, content hygiene, and calm utility.

### Role Assignments
- **Primary (`#1E293B`):** Deep editorial slate. Applied to dominant titles, prominent navigation anchors, active state icons, and structural text.
- **Secondary (`#2563EB`):** Cobalt ink accent. Used for interactive links, active pagination indicators, selection rings, and progress markers.
- **Tertiary / Accent (`#D97706`):** Warm ochre gold. Reserved for saved articles, active bookmarks, and starring states.
- **Neutral (`#F8FAFC` / `#F9F9FB`):** Warm paper tint providing a soft, non-glaring canvas that softens high-contrast screens.

### Semantic Status & Health Hierarchy
Feed health monitoring tokens communicate operational state with immediate clarity:
- **`status-ok` (`#10B981`):** Feed active, synchronized, and healthy.
- **`status-warning` (`#F59E0B`):** Feed stale, high sync latency, or partial payload parsing.
- **`status-error` (`#EF4444`):** Feed broken, 404/500 HTTP errors, or dead syndication endpoint.

All semantic dot indicators must maintain minimum 3:1 contrast against surface backgrounds and couple with text or icon equivalents for accessibility.

## Typography

The typography pairs **Newsreader** for literary contemplation with **Inter** for utilitarian UI clarity.

### Hierarchy & Scale Rules
- **Editorial Headlines & Longform (`Newsreader`):** Used exclusively for article headlines, publication titles, and full-screen reading text (`body-reading`). Line heights for long-form reading maintain a generous 1.6–1.7 ratio to prevent ocular tracking fatigue.
- **System Interface & Metadata (`Inter`):** Used for navigation headers, publication names, timestamps, badges, filter chips, reading progress, and settings screens.
- **Punctuation & Numerals:** Optical quotes and proportional figures are prioritized in article views, while tabular figures (`font-variant-numeric: tabular-nums`) are mandated for reading time indicators, sync timestamps, and feed counts.

## Layout & Spacing

The layout model adapts seamlessly between high-density RSS skimming and focused reading views.

### Structure
- **Mobile (< 768px):** Single-column fluid stream bounded by `margin-mobile` (16px). Stream cards stack with `gutter-stream` (12px) separation or render as an edge-to-edge separated list with `1px` hairlines. Bottom floating bars maintain safe-area padding (`env(safe-area-inset-bottom) + 12px`).
- **Tablet (768px – 1024px):** 2-column split master-detail layout. Left navigation and feed list occupies 340px fixed width; right reading pane centers content within a fixed 640px column.
- **Desktop (> 1024px):** 3-pane workstation structure (Feeds & Folders: 260px; Article Stream: 380px; Reader View: flexible, constrained by `reader-max-width` of 672px / 42rem for optimal line length between 65–75 characters).

### Spacing Principles
Spacing adheres strictly to a 4px baseline unit. Metadata clusters use tight horizontal rhythm (`spacing-xs` to `spacing-sm`), while reading view sections employ generous vertical rhythm (`spacing-2xl` to `spacing-3xl`) to establish breathing room.

## Elevation & Depth

Visual depth follows an **Ambient iOS Layered** model, favoring translucent surfaces and hairline borders over heavy artificial drop shadows.

### Elevation Hierarchy
- **Level 0 (Canvas Base):** `paper-base` (`#F9F9FB`). Solid background canvas across feed views.
- **Level 1 (Card & Content Panes):** Pure white (`#FFFFFF`) with a delicate hairline boundary (`1px solid #E2E8F0` or `1px solid rgba(15, 23, 42, 0.06)`). No box-shadow is used on static list items.
- **Level 2 (Active/Hovered Stream Cards):** Ambient shadow: `0 4px 12px -2px rgba(15, 23, 42, 0.04), 0 2px 4px -1px rgba(15, 23, 42, 0.02)`.
- **Level 3 (Floating Bars, Action Sheets, Modals):** Translucent backdrop blur (`backdrop-filter: blur(20px) saturate(180%)`) paired with `rgba(255, 255, 255, 0.85)` fill, an ultra-subtle top hairline `rgba(255, 255, 255, 0.6)`, and ambient elevation: `0 12px 32px -4px rgba(15, 23, 42, 0.08), 0 4px 12px -2px rgba(15, 23, 42, 0.04)`.

## Shapes

The design uses a balanced **Rounded (level 2)** shape vocabulary that aligns naturally with iOS continuous rounded corners (squircle physics).

- **Cards & Reading Panels:** 16px (`1rem` / `rounded-lg`) corner radii for contained card items, delivering structural softness while retaining high content efficiency.
- **Pill Chips & Badges:** Fully circular/pill boundaries (`9999px`) for category filters, status indicators, and micro metadata tags.
- **Interactive Controls & Toggles:** 8px to 12px corner radii for text fields, icon action buttons, and segmented selection bars.
- **Thumbnail Images & Favicons:** 6px radius on site favicons (16x16px or 24x24px); 10px radius on article thumbnail previews.

## Components

### 1. Article Stream Card
- **Structure:** Compact horizontal or stacked layout. Top line displays the publication favicon (16px rounded), source title in `label-sm` slate-500, time elapsed in `label-sm` slate-400, and unread dot indicator (6px cobalt circle).
- **Title:** `headline-md` Newsreader serif, maximum 2 lines with graceful truncation. Read articles drop to `text-secondary` (`#64748B`) regular weight.
- **Footer:** Reading progress bar (2px height, cobalt fill over slate-100 track) anchored to the bottom card edge, accompanied by reading time estimate (e.g., "4 min read") and optional ochre bookmark flag.

### 2. Category & Filter Chips
- **Aesthetic:** Horizontal scrollable row of pill-shaped selectors.
- **Default State:** Transparent or `surface-card` background, `1px solid #E2E8F0` border, `label-md` text in `#64748B`.
- **Active State:** Deep slate background (`#1E293B`), white text (`#FFFFFF`), zero border, subtle scale compression on tap (`transform: scale(0.97)`).
- **Count Capsule:** Embedded numeric capsule inside chip displaying unread items with muted inverse opacity.

### 3. Status Badges & Dot Indicators
- **Format:** Micro pill (`padding: 2px 8px`) displaying a 6px status dot alongside `label-meta` text.
- **States:**
  - *OK:* Dot `#10B981`, badge background `rgba(16, 185, 129, 0.1)`, text `#047857`.
  - *Warning:* Dot `#F59E0B`, badge background `rgba(245, 158, 11, 0.1)`, text `#B45309`.
  - *Error:* Dot `#EF4444`, badge background `rgba(239, 68, 68, 0.1)`, text `#B91C1C`.

### 4. Floating Reader Control Bar
- **Placement:** Docked to bottom viewport, floating above content with 16px bottom and horizontal margins.
- **Material:** Frosted glass acrylic (`rgba(255, 255, 255, 0.82)`, `backdrop-blur: 24px`, border `1px solid rgba(226, 232, 240, 0.8)`).
- **Controls:** Symmetrical 5-action cluster: Back/Close, Typographic Appearance (`Aa`), Bookmark Toggle, Mark Read/Unread, and Native Share Sheet. Height is 52px with pill geometry (`border-radius: 9999px`).

### 5. Native Form Inputs & Range Sliders
- **Settings Toggle:** iOS native-style 51x31px switch, emerald or cobalt fill when active, slate-200 track when inactive.
- **Typographic Range Sliders:** Slender 4px track with a 24px pure-white thumb casting a soft ambient drop shadow. Left and right track endpoints feature micro icon cues (e.g., small 'A' and large 'A' for text-scaling controls).
- **Input Fields (Feed URL / Search):** `surface-subtle` background (`#F1F5F9`), zero outline, 10px rounded corners, integrated search glyph, and clear button (`x`) matching iOS search specifications.