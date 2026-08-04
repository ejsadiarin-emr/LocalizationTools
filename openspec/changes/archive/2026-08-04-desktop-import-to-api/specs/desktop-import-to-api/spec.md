## ADDED Requirements

### Requirement: Import data to API from Remote mode
In Remote mode, the Desktop application SHALL provide an "Import Data to API" control that uploads a selected local `data-bank.json` to the API `POST /api/import` endpoint using upsert semantics.

#### Scenario: Import button visible only in Remote mode
- **WHEN** application is in Local mode
- **THEN** the "Import Data to API" control is hidden
- **WHEN** application is in Remote mode
- **THEN** the "Import Data to API" control is visible

#### Scenario: Select and import data-bank.json
- **WHEN** user clicks "Import Data to API" and selects a valid `data-bank.json` file
- **THEN** application uploads the file to `POST /api/import`
- **THEN** application reports the imported entry count in the status bar

#### Scenario: Import failure
- **WHEN** the upload fails or the API returns an error
- **THEN** application displays the error message in the status bar without crashing

#### Scenario: Refresh entries after successful import
- **WHEN** an import completes successfully
- **THEN** application refreshes the displayed entries from the API

#### Scenario: Prevent concurrent imports
- **WHEN** an import is in progress
- **THEN** the "Import Data to API" control is disabled until the import completes
