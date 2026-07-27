## ADDED Requirements

### Requirement: Coverage summary endpoint
The system SHALL provide a GET endpoint at `/api/coverage` that returns coverage summary information.

#### Scenario: Get coverage summary
- **WHEN** client sends GET request to `/api/coverage`
- **THEN** system returns CoverageReport object with coverage metrics for all locales

#### Scenario: Coverage by locale
- **WHEN** client sends GET request to `/api/coverage?locale=en-US`
- **THEN** system returns coverage data specific to "en-US" locale

#### Scenario: Coverage by format
- **WHEN** client sends GET request to `/api/coverage?format=resx`
- **THEN** system returns coverage data for entries from .resx files only

### Requirement: Statistics endpoint
The system SHALL provide a GET endpoint at `/api/stats` that returns comprehensive statistics.

#### Scenario: Get all statistics
- **WHEN** client sends GET request to `/api/stats`
- **THEN** system returns statistics including total entries, entries by locale, entries by format, and translation status breakdown

#### Scenario: Statistics include translation metrics
- **WHEN** statistics are computed
- **THEN** system includes count of translated, untranslated, and partially translated entries

### Requirement: Statistics computation
The system SHALL compute statistics from the loaded data in real-time.

#### Scenario: Statistics reflect current data
- **WHEN** data is updated through CRUD operations or extraction
- **THEN** statistics endpoints return updated values reflecting current state

#### Scenario: Empty data handling
- **WHEN** no data is loaded (empty or missing data-bank.json)
- **THEN** statistics endpoints return zero values or appropriate empty state indicators