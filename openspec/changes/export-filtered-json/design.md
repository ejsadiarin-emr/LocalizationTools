## Context

The DataBank Desktop app (WPF + WebView2) loads localization JSON data, filters it by locale/format/status, and displays it in a table. Users can filter to specific locales (e.g., "en" + "zh-CN") but cannot export the filtered view. The app follows a pattern where C# handles file I/O and JS handles UI logic, communicating via WebView2 messages.

Current data flow:
```
C# reads JSON → WebView2 message → JS allEntries → applyFilters() → filteredEntries → renderTable()
```

Proposed export flow:
```
JS filteredEntries → buildExportJson() → WebView2 message → C# SaveFileDialog → write file
```

## Goals / Non-Goals

**Goals:**
- Enable export of filtered localization data as JSON
- Maintain data-bank.json schema compatibility (version 3)
- Filter values and sources to selected locales only
- Provide user-friendly file save dialog
- Generate descriptive filename with timestamp and locales

**Non-Goals:**
- Export all entries without filtering (user can export the original file directly)
- Export to non-JSON formats (CSV, XLIFF, etc.)
- Server-side persistence of filtered views
- Batch/multiple file export
- GRF entry export (separate tab, not part of main filter flow)

## Decisions

### 1. C# SaveFileDialog over JS Blob download

**Choice:** C# backend handles file save via SaveFileDialog

**Alternatives considered:**
- JS Blob + `<a download>` trigger: Simpler, no C# changes, but no native file dialog, unreliable in WebView2

**Rationale:** Matches existing load pattern (C# reads file → sends to JS). Provides native OS file dialog experience. More reliable file write in WebView2 context.

### 2. Filter sources to match values

**Choice:** Filter `sources` object keys to match selected locales (same as values filtering)

**Alternatives considered:**
- Keep all sources regardless of locale filter

**Rationale:** Consistency — if values only has "en" + "zh-CN", sources for other locales would be confusing. Export represents a specific locale-filtered view.

### 3. Export all filteredEntries, not just current page

**Choice:** Export entire `filteredEntries` array

**Rationale:** User expectation — filtered view represents the complete result set. Current page is a rendering concern, not a data concern.

### 4. Dynamic filename with metadata

**Choice:** `databank-export-{timestamp}-{locales}.json`

**Example:** `databank-export-2026-08-03T12-30-00-en-zh-CN.json`

**Rationale:** Makes exported files self-describing. Timestamp prevents overwrites. Locale list shows content at a glance.

## Risks / Trade-offs

- **[Large exports]** → No mitigation needed — JSON.stringify handles large arrays efficiently. If issues arise, can add progress indicator later.
- **[Schema drift]** → If data-bank.json schema changes version, export code needs update. Mitigated by hardcoding version 3 and keeping export logic simple.
- **[WebView2 message size]** → JSON string passed via postMessage. For very large datasets (100k+ entries), this could be slow. Mitigation: not expected in current use case; can optimize later if needed.
