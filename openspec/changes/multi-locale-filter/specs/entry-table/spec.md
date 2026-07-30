## MODIFIED Requirements

### Requirement: Table supports client-side filtering
The system SHALL allow users to filter entries by locale, format, and translation status. All filtering happens client-side in the web layer after fetching from API. The locale filter SHALL support multi-select with OR logic.

#### Scenario: Filter by multiple locales
- **WHEN** user selects one or more locales from the multi-select checkbox dropdown
- **THEN** web frontend filters cached entries where ANY selected locale has a non-empty value
- **AND** re-renders table with filtered results

#### Scenario: No locales selected shows all entries
- **WHEN** no locales are selected in the multi-select dropdown
- **THEN** web frontend shows all entries (no locale filtering applied)
- **AND** all locale columns are displayed in the table

#### Scenario: Filter by locale updates table columns
- **WHEN** user selects locales in the multi-select dropdown
- **THEN** table columns update to display only the selected locales
- **AND** Key, Format, and Status columns remain visible

#### Scenario: Filter by format
- **WHEN** user selects a format (resx/rc/fhx/ahc) from filter dropdown
- **THEN** web frontend filters cached entries where Source.Format matches selected value
- **AND** re-renders table with filtered results

#### Scenario: Filter by translation status
- **WHEN** user selects a status (translated/untranslated/needs review/do not translate) from filter dropdown
- **THEN** web frontend filters cached entries where Metadata.TranslationStatus matches selected value
- **AND** re-renders table with filtered results

#### Scenario: Combined filters
- **WHEN** user applies multiple filters (locale, format, status, search)
- **THEN** web frontend applies AND logic across filter types (locale OR logic within, AND across types)
- **AND** re-renders table with intersection of matching entries

### Requirement: Locale filter dropdown with checkboxes
The system SHALL provide a multi-select dropdown for locale filtering with checkbox selection, Select All, and Clear All actions.

#### Scenario: Dropdown displays selected locales
- **WHEN** locale dropdown is closed
- **THEN** trigger button shows comma-separated list of selected locale codes
- **AND** shows "All Locales" when no locales are selected

#### Scenario: Select All action
- **WHEN** user clicks "Select All" in the dropdown
- **THEN** all locale checkboxes become checked
- **AND** table re-renders with all locales shown as columns

#### Scenario: Clear All action
- **WHEN** user clicks "Clear All" in the dropdown
- **THEN** all locale checkboxes become unchecked
- **AND** table re-renders showing all entries and all locale columns

#### Scenario: Click outside closes dropdown
- **WHEN** dropdown is open and user clicks outside the dropdown
- **THEN** dropdown closes
