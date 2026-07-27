## ADDED Requirements

### Requirement: Table displays localization entries
The system SHALL display a table in the web frontend (inside WebView2) showing all localization entries with required columns.

> **TEMPORARY NOTE**: For快速 validation, entries are loaded from `data-bank.json` file via C# code-behind instead of API. The table rendering, filtering, and search logic remain identical regardless of data source.

#### Scenario: Table renders with all columns
- **WHEN** user navigates to table view
- **THEN** web frontend fetches entries from DataBank API (`GET /api/entries`)
- **AND** displays columns: Key, Source (EN), Translation, Locale, Format, Status

#### Scenario: Table sorts by column
- **WHEN** user clicks column header
- **THEN** web frontend sorts table by that column in ascending order; second click sorts descending

### Requirement: Table supports client-side filtering
The system SHALL allow users to filter entries by locale, format, and translation status. All filtering happens client-side in the web layer after fetching from API.

#### Scenario: Filter by locale
- **WHEN** user selects a locale from filter dropdown
- **THEN** web frontend filters cached entries where Locale matches selected value
- **AND** re-renders table with filtered results

#### Scenario: Filter by format
- **WHEN** user selects a format (resx/rc/fhx/ahc) from filter dropdown
- **THEN** web frontend filters cached entries where Source.Format matches selected value
- **AND** re-renders table with filtered results

#### Scenario: Filter by translation status
- **WHEN** user selects a status (translated/untranslated/needs review/do not translate) from filter dropdown
- **THEN** web frontend filters cached entries where Metadata.TranslationStatus matches selected value
- **AND** re-renders table with filtered results

#### Scenario: Combined filters
- **WHEN** user applies multiple filters
- **THEN** web frontend applies AND logic across all active filters
- **AND** re-renders table with intersection of matching entries

### Requirement: Table supports client-side search
The system SHALL allow users to search entries by key or source string. Search happens client-side.

#### Scenario: Search by key
- **WHEN** user enters text in search box
- **THEN** web frontend filters entries where Key contains search text (case-insensitive substring match)

#### Scenario: Search by source string
- **WHEN** user enters text in search box
- **THEN** web frontend filters entries where Source (EN) contains search text (case-insensitive substring match)

#### Scenario: Search is debounced
- **WHEN** user types in search box
- **THEN** web frontend debounces search input (300ms) to avoid excessive re-renders

### Requirement: Table rows are color-coded by status
The system SHALL display row background color based on translation status.

#### Scenario: Translated entries show green
- **WHEN** entry status is "translated"
- **THEN** table row background is green

#### Scenario: Untranslated entries show red
- **WHEN** entry status is "untranslated"
- **THEN** table row background is red

#### Scenario: Needs review entries show yellow
- **WHEN** entry status is "needs review"
- **THEN** table row background is yellow

#### Scenario: Do not translate entries show gray
- **WHEN** entry status is "do not translate"
- **THEN** table row background is gray

### Requirement: Table supports client-side pagination
The system SHALL paginate results when entries exceed page size. Pagination is handled client-side.

#### Scenario: Pagination controls display
- **WHEN** total filtered entries exceed page size (default 50)
- **THEN** web frontend displays pagination controls (previous, next, page numbers)

#### Scenario: Page navigation
- **WHEN** user clicks next page or page number
- **THEN** web frontend displays corresponding page of filtered entries
