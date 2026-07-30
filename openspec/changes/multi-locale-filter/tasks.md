## 1. HTML Structure

- [x] 1.1 Replace `<select id="locale-filter">` with custom multi-select dropdown markup in `index.html`
- [x] 1.2 Add dropdown trigger button, dropdown container, checkbox list, and action buttons (Select All, Clear All)

## 2. CSS Styling

- [x] 2.1 Add `.locale-multiselect` container styles (relative positioning)
- [x] 2.2 Add `.locale-multiselect-trigger` button styles (matches existing filter bar look)
- [x] 2.3 Add `.locale-multiselect-dropdown` panel styles (absolute positioning, z-index, max-height, scroll)
- [x] 2.4 Add `.locale-multiselect-option` checkbox label styles
- [x] 2.5 Add `.locale-multiselect-actions` styles for Select All / Clear All buttons

## 3. JavaScript State & Initialization

- [x] 3.1 Replace `localeFilter` DOM ref with new multi-select element references in `app.js`
- [x] 3.2 Add `selectedLocales = new Set()` state variable
- [x] 3.3 Update `populateFilters()` to build checkbox list from unique locales instead of dropdown options

## 4. JavaScript Dropdown Behavior

- [x] 4.1 Implement `toggleLocaleDropdown()` to open/close the dropdown panel
- [x] 4.2 Implement `toggleLocale(locale)` to add/remove locale from `selectedLocales`
- [x] 4.3 Implement `selectAllLocales()` to add all locales to `selectedLocales`
- [x] 4.4 Implement `clearAllLocales()` to empty `selectedLocales`
- [x] 4.5 Implement `updateTriggerDisplay()` to show selected locale text on trigger button
- [x] 4.6 Add click-outside listener to close dropdown

## 5. JavaScript Filter Logic

- [x] 5.1 Update `applyFilters()` to use OR logic across `selectedLocales` Set
- [x] 5.2 Ensure empty selection means no locale filtering (show all entries)

## 6. JavaScript Table Rendering

- [x] 6.1 Update `renderTable()` to build column list from `selectedLocales` instead of DEFAULT_LOCALES
- [x] 6.2 Show all locale columns when `selectedLocales` is empty (backwards compatible default)

## 7. Event Listeners

- [x] 7.1 Wire up trigger button click to `toggleLocaleDropdown()`
- [x] 7.2 Wire up checkbox change events to `toggleLocale()`
- [x] 7.3 Wire up Select All button to `selectAllLocales()`
- [x] 7.4 Wire up Clear All button to `clearAllLocales()`

## 8. Verification

- [x] 8.1 Test: selecting one locale filters entries and shows only that locale column
- [x] 8.2 Test: selecting multiple locales shows entries with OR logic and multiple columns
- [x] 8.3 Test: no selection shows all entries and all locale columns
- [x] 8.4 Test: Select All checks all boxes and shows all columns
- [x] 8.5 Test: Clear All unchecks all boxes and shows all entries
- [x] 8.6 Test: format, status, and search filters still work correctly with multi-locale filter
- [x] 8.7 Test: click outside closes dropdown
