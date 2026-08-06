## ADDED Requirements

### Requirement: Value edits persist to local data-bank.json

When a user edits a translation value in local mode, the system SHALL write the updated value back to the loaded data-bank.json file automatically after the source file write-back succeeds.

#### Scenario: Value edit updates data-bank.json in local mode
- **WHEN** a user edits a locale value in local mode and the FileWriter edit succeeds
- **THEN** the system SHALL update the corresponding `values[]` entry in the in-memory data-bank model and write the full data-bank.json back to the file that was loaded at startup

#### Scenario: Value edit updates source line number in data-bank.json
- **WHEN** a user edits a locale value and the FileWriter returns a new line number
- **THEN** the system SHALL update `sources[locale].line` in the in-memory data-bank model to reflect the new line number before writing data-bank.json

#### Scenario: data-bank.json write failure shows warning
- **WHEN** writing data-bank.json to disk fails after a successful FileWriter edit
- **THEN** the system SHALL display a warning in the status bar indicating the source file was saved but the data-bank.json write failed

### Requirement: Value edits persist to remote MongoDB via API

When a user edits a translation value in remote mode, the system SHALL call the API to update MongoDB automatically after the source file write-back succeeds.

#### Scenario: Value edit updates MongoDB via API in remote mode
- **WHEN** a user edits a locale value in remote mode and the FileWriter edit succeeds
- **THEN** the system SHALL call `PUT /api/entries/{key}/locales/{locale}` with the new value to persist the change to MongoDB

#### Scenario: API call failure shows warning
- **WHEN** the API call to update MongoDB fails after a successful FileWriter edit
- **THEN** the system SHALL display a warning in the status bar indicating the source file was saved but the remote database update failed

### Requirement: Metadata fields are editable in the detail panel

The system SHALL provide editable UI controls for metadata fields (comment, doNotTranslate, formatSpecifiers) in the entry detail panel.

#### Scenario: Comment field is editable
- **WHEN** a user views an entry in the detail panel
- **THEN** the system SHALL display the comment as an editable text input

#### Scenario: DoNotTranslate checkbox is editable
- **WHEN** a user views an entry in the detail panel
- **THEN** the system SHALL display doNotTranslate as a toggleable checkbox

#### Scenario: FormatSpecifiers field is editable
- **WHEN** a user views an entry in the detail panel
- **THEN** the system SHALL display formatSpecifiers as a text input (comma-separated or free-text)

### Requirement: Metadata edits persist to storage backend

When a user edits a metadata field, the system SHALL persist the change to the appropriate storage backend.

#### Scenario: Metadata edit persists to data-bank.json in local mode
- **WHEN** a user changes a metadata field (comment, doNotTranslate, formatSpecifiers) in local mode
- **THEN** the system SHALL update the in-memory data-bank model and write data-bank.json to disk

#### Scenario: Metadata edit persists to MongoDB via API in remote mode
- **WHEN** a user changes a metadata field in remote mode
- **THEN** the system SHALL call `PUT /api/entries/{key}` with the full updated entry to persist the metadata change to MongoDB

### Requirement: Local mode stores loaded file path for persistence

The system SHALL remember the file path of the loaded data-bank.json file so that subsequent saves can write back to the same location.

#### Scenario: Loaded file path is retained
- **WHEN** a user loads a data-bank.json file in local mode
- **THEN** the system SHALL store the file path and use it for all subsequent auto-save writes without prompting the user for a save location
