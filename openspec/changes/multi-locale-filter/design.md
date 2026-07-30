## Context

The DataBank Desktop app is a WPF + WebView2 application with a vanilla JavaScript frontend. The locale filter currently uses a native HTML `<select>` element for single-locale selection. Filtering happens client-side in `app.js` after all entries are loaded.

**Current State:**
- Single-select `<select id="locale-filter">` dropdown
- Filter logic: `if (locale && getLocaleValue(e, locale) === '') return false`
- Table columns: always show DEFAULT_LOCALES (en, zh-CN, ru, ja) + any extras from data
- State: `localeFilter.value` is a single string

**Constraints:**
- Vanilla JavaScript only (no framework, no build step)
- Must not break existing filter functionality (format, status, search, pagination)
- Must remain client-side filtering (no API changes)
- Dark theme UI (VS Code-inspired colors)

## Goals / Non-Goals

**Goals:**
- Multi-locale selection via checkbox dropdown
- OR-based filtering: show entries that have ANY of the selected locales
- Table columns update to show only selected locales
- Backwards compatible: no selection = show all (same as current "All Locales")
- Clean UI consistent with existing dark theme

**Non-Goals:**
- AND-based locale filtering
- Multi-select for format or status filters
- Server-side filtering changes
- New dependencies or libraries

## Decisions

**1. UI Pattern: Dropdown with Checkboxes**

- **Decision**: Custom dropdown with checkbox list, triggered by a button showing selected locales
- **Rationale**: Compact, familiar pattern, works well with 5-15 locales. Avoids horizontal overflow issues of inline checkboxes.
- **Alternatives Considered**:
  - Native `<select multiple>`: Ugly, poor UX, inconsistent rendering across platforms
  - Inline checkboxes: Takes too much horizontal space when many locales exist
  - Tag/chip selector: More complex to build, overkill for this use case

**2. Filter Logic: OR (Any Match)**

- **Decision**: Show entries that have a non-empty value for ANY of the selected locales
- **Rationale**: Matches user expectation from the request ("select en and zh-CN → entries with these locale"). OR is more useful for cross-locale comparison.
- **Alternatives Considered**:
  - AND (all match): Too restrictive, would exclude entries that have one locale but not the other

**3. State Management: Set of Selected Locales**

- **Decision**: Use `selectedLocales = new Set()` instead of `localeFilter.value`
- **Rationale**: Set provides O(1) lookup, easy add/delete, natural for multi-select. Replaces the single string value.
- **Pattern**: Existing code uses plain JS variables; Set fits this pattern.

**4. Table Columns: Selected-Only**

- **Decision**: When locales are selected, table columns show only those locales. When nothing selected, show all.
- **Rationale**: Reduces visual clutter, focuses view on relevant data. Backwards compatible default.
- **Alternatives Considered**:
  - Always show all columns: Wastes space, defeats purpose of filtering

**5. Dropdown Positioning: Absolute Below Trigger**

- **Decision**: Dropdown menu positioned absolutely below the trigger button
- **Rationale**: Standard pattern, avoids layout shifts, works within the filter bar flex container

## Risks / Trade-offs

**[Risk] Dropdown overlaps table on small windows** →
- Mitigation: Dropdown has max-height with scroll, z-index above table
- Trade-off: Acceptable for desktop app with resizable window

**[Risk] Many locales (20+) make checkbox list long** →
- Mitigation: Scrollable dropdown with max-height
- Trade-off: Uncommon scenario for this app's use case

**[Risk] Breaking "All Locales" default behavior** →
- Mitigation: Empty selection = show all locales, preserving current visual behavior
- Trade-off: Internal state changes, but user-facing behavior is backwards compatible

**[Trade-off] No "Select All" toggle in filter bar** →
- Chose to put Select All inside dropdown for compactness
- Users can still select all via individual checkboxes
