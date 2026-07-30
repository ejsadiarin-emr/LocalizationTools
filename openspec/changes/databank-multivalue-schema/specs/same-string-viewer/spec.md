## ADDED Requirements

### Requirement: Grouped key display
The frontend SHALL display entries grouped by key, showing all locale values in a single row or expandable section. Each row SHALL show the key and a summary of available locales.

#### Scenario: Multi-locale key display
- **WHEN** the user views the entries table and a key has translations in 4 locales
- **THEN** the row shows the key name and indicators for each locale's value (or a column per locale)

#### Scenario: Missing locale indication
- **WHEN** a key has no translation for a specific locale
- **THEN** the corresponding locale column or cell shows an empty state (grayed out or dash)

### Requirement: Locale column rendering
The frontend SHALL render a column for each supported locale (en, zh-CN, ru, ja by default). The locale columns SHALL display the translated value for that locale.

#### Scenario: Default locale set
- **WHEN** the data contains entries with locales "en", "zh-CN", "ru", "ja"
- **THEN** the table displays columns for all four locales

#### Scenario: Dynamic locale columns
- **WHEN** the data contains locales not in the default set
- **THEN** the table adds additional columns for those locales

### Requirement: Locale filter for grouped view
The frontend SHALL provide a locale filter that operates on the grouped view. Filtering by a locale SHALL show only keys that have a non-empty value for that locale.

#### Scenario: Filter by locale
- **WHEN** the user selects "zh-CN" in the locale filter
- **THEN** only keys with a non-empty Chinese translation are displayed

#### Scenario: Clear locale filter
- **WHEN** the user clears the locale filter
- **THEN** all keys are displayed regardless of locale

### Requirement: Search across all locale values
The frontend SHALL search across all locale values when the user types in the search box. A key SHALL match if any of its locale values contain the search term.

#### Scenario: Search in any locale
- **WHEN** the user searches for "CRITICAL"
- **THEN** keys where any locale value contains "CRITICAL" are shown

#### Scenario: Search in key name
- **WHEN** the user searches for "@ALARM"
- **THEN** keys matching "@ALARM" in their name are shown regardless of locale values

### Requirement: Dashboard stats for grouped entries
The frontend SHALL calculate dashboard statistics based on grouped entries. Total entry count SHALL equal the number of unique keys. Locale-specific counts SHALL reflect how many keys have a non-empty value for that locale.

#### Scenario: Total count
- **WHEN** the data contains 609 unique keys
- **THEN** the dashboard shows "609" as total entries

#### Scenario: Locale coverage
- **WHEN** 550 out of 609 keys have a Chinese translation
- **THEN** the dashboard shows zh-CN coverage as 550 (90%)

### Requirement: Detail panel for grouped entries
The frontend SHALL display a detail panel showing all locale values and sources for a selected key. The panel SHALL show each locale's value, source file, and format.

#### Scenario: View all translations
- **WHEN** the user clicks on a key row
- **THEN** the detail panel shows all locale values, their source files, and metadata (doNotTranslate, isTranslated, comment, formatSpecifiers)
