## Why

The current locale filter is a single-select dropdown, forcing users to view one locale at a time. When comparing translations across languages (e.g., checking English vs Chinese side-by-side), users must manually switch between locales repeatedly. This is inefficient for teams working with multiple locales simultaneously.

## What Changes

- Replace the single-select locale dropdown with a multi-select checkbox dropdown
- Change filter logic from single-locale match to OR-based multi-locale matching
- Update table columns to display only the selected locales (instead of always showing all)
- Add "Select All" and "Clear All" convenience actions in the dropdown

**BREAKING**: The locale filter behavior changes from single-select to multi-select. Users who relied on "All Locales" as default will now see all locales when nothing is explicitly selected (same visual result, different internal state).

## Capabilities

### Modified Capabilities

- `entry-table`: The locale filtering requirement changes from single-select to multi-select with OR logic. Table column rendering changes to show only selected locales.

## Impact

- **Frontend files**: `wwwroot/index.html`, `wwwroot/styles.css`, `wwwroot/app.js`
- **State management**: `localeFilter.value` (string) replaced by `selectedLocales` (Set)
- **Filter logic**: `applyFilters()` function changes locale matching from single-value to multi-value OR
- **Table rendering**: `renderTable()` column logic changes from DEFAULT_LOCALES + extras to selected-only
- **No API changes**: Filtering remains client-side
- **No C# changes**: WebView2 communication unchanged
