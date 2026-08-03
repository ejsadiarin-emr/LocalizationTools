## ADDED Requirements

### Requirement: Export button visibility
The Export button SHALL be visible in the DataBank toolbar and SHALL be enabled only when `filteredEntries` contains at least one entry.

#### Scenario: Button disabled when no data
- **WHEN** `filteredEntries` is empty (no entries match current filters)
- **THEN** the Export button SHALL be disabled

#### Scenario: Button enabled when data exists
- **WHEN** `filteredEntries` contains at least one entry
- **THEN** the Export button SHALL be enabled

### Requirement: Export JSON structure
The exported JSON SHALL conform to the data-bank.json schema version 3 with fields: `version`, `generated`, `basePath`, and `entries`.

#### Scenario: Export schema compliance
- **WHEN** user triggers export
- **THEN** the exported JSON SHALL contain `version: 3`, `generated` (ISO 8601 timestamp), `basePath` (from loaded data), and `entries` array

### Requirement: Locale filtering in export
The exported entries' `values` and `sources` SHALL be filtered to include only the currently selected locales.

#### Scenario: Export with locale filter applied
- **WHEN** user has selected locales "en" and "zh-CN" in the filter
- **AND** user triggers export
- **THEN** each entry's `values` array SHALL contain only objects with `locale` equal to "en" or "zh-CN"
- **AND** each entry's `sources` object SHALL contain only keys for "en" and "zh-CN"

#### Scenario: Export with all locales selected
- **WHEN** all available locales are selected in the filter
- **AND** user triggers export
- **THEN** each entry's `values` and `sources` SHALL contain all available locales (same as unfiltered)

### Requirement: File save dialog
The system SHALL display a native SaveFileDialog when the user triggers export, allowing them to choose the save location and filename.

#### Scenario: User saves file
- **WHEN** user clicks Export and selects a location in the SaveFileDialog
- **THEN** the JSON SHALL be written to the selected file path
- **AND** a success notification SHALL be displayed

#### Scenario: User cancels save
- **WHEN** user clicks Export and cancels the SaveFileDialog
- **THEN** no file SHALL be written
- **AND** no error notification SHALL be displayed

### Requirement: Default filename format
The SaveFileDialog SHALL suggest a default filename in the format `databank-export-{timestamp}-{locales}.json`.

#### Scenario: Default filename generation
- **WHEN** user triggers export
- **THEN** the SaveFileDialog SHALL suggest filename `databank-export-{ISO-timestamp}-{locale-list}.json`
- **AND** timestamp SHALL use local time with seconds precision
- **AND** locale list SHALL be hyphen-separated (e.g., `en-zh-CN`)
