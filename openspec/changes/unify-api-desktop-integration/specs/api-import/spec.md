## ADDED Requirements

### Requirement: Import data-bank.json via API
The system SHALL provide a REST endpoint that accepts a `data-bank.json` file upload and imports all entries into MongoDB.

#### Scenario: Successful import
- **WHEN** client sends `POST /api/import` with a valid `data-bank.json` file
- **THEN** system deserializes the file using `DataBank.Cli.Models.DataBankOutput`, upserts all entries into MongoDB, and returns `{ success: true, entryCount: N, version: 2 }`

#### Scenario: Invalid file format
- **WHEN** client sends `POST /api/import` with a file that is not valid JSON or missing required fields
- **THEN** system returns `400 Bad Request` with error message describing the validation failure

#### Scenario: Empty entries array
- **WHEN** client sends `POST /api/import` with a valid JSON file containing an empty entries array
- **THEN** system returns `{ success: true, entryCount: 0, version: 2 }` (no error)

### Requirement: Upsert semantics for import
The system SHALL use upsert semantics when importing entries, so re-running the import replaces existing entries instead of failing on duplicates.

#### Scenario: Re-import existing entries
- **WHEN** client sends `POST /api/import` with a file containing entries that already exist in MongoDB
- **THEN** system replaces existing entries with the new values (upsert) and returns success

#### Scenario: Import new entries
- **WHEN** client sends `POST /api/import` with a file containing entries that don't exist in MongoDB
- **THEN** system inserts the new entries and returns success

### Requirement: Import metadata update
The system SHALL update the metadata document after successful import with the new version and entry count.

#### Scenario: Metadata updated after import
- **WHEN** import completes successfully
- **THEN** system updates the `_id: "metadata"` document with `Generated` timestamp and `EntryCount` matching the imported data
