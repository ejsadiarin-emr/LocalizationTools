## ADDED Requirements

### Requirement: All CRUD operations use MongoDB
The system SHALL persist all localization entries in MongoDB via `IDataBankRepository`. No file-based or in-memory stores SHALL be used for entry persistence.

#### Scenario: Create entry via API
- **WHEN** a POST request is made to `/api/entries` with a valid `DataBankEntryDocument` body
- **THEN** the entry is inserted into the MongoDB `DataBankEntry` collection and returned with 201 status

#### Scenario: Read entry from API
- **WHEN** a GET request is made to `/api/entries/{id}` for an existing entry
- **THEN** the entry is retrieved from MongoDB and returned with 200 status

#### Scenario: Update entry via API
- **WHEN** a PUT request is made to `/api/entries/{id}` with a valid body
- **THEN** the entry is replaced in MongoDB and returned with 200 status

#### Scenario: Delete entry via API
- **WHEN** a DELETE request is made to `/api/entries/{id}` for an existing entry
- **THEN** the entry is removed from MongoDB and 204 No Content is returned

#### Scenario: List entries with filtering
- **WHEN** a GET request is made to `/api/entries` with optional `locale`, `format`, or `key` query parameters
- **THEN** matching entries are retrieved from MongoDB using indexed queries and returned as a list

### Requirement: Extraction writes to MongoDB
The system SHALL persist extracted entries directly into MongoDB. The extraction service SHALL NOT write to files or in-memory stores.

#### Scenario: Extract resx files to MongoDB
- **WHEN** a POST request is made to `/api/extract` with a valid `SourceDirectory` containing `.resx` files
- **THEN** entries are parsed using `ResxParser`, inserted into MongoDB via `IDataBankRepository`, and a job ID is returned

#### Scenario: Extract rc files to MongoDB
- **WHEN** extraction is triggered on a directory containing `.rc` files
- **THEN** entries are parsed using `RcParser` and inserted into MongoDB

#### Scenario: Extract fhx files to MongoDB
- **WHEN** extraction is triggered on a directory containing `.fhx` files
- **THEN** entries are parsed using `FhxParser` and inserted into MongoDB

#### Scenario: Extract ahc files to MongoDB
- **WHEN** extraction is triggered on a directory containing `.ahc` files
- **THEN** entries are parsed using `AhcParser` and inserted into MongoDB

#### Scenario: Extraction updates metadata
- **WHEN** extraction completes successfully
- **THEN** the `DataBankMetadataDocument` is updated with the current timestamp and total entry count

### Requirement: Statistics computed from MongoDB
The system SHALL compute statistics and coverage using MongoDB aggregation pipelines, not in-memory computation.

#### Scenario: Get statistics
- **WHEN** a GET request is made to `/api/stats`
- **THEN** statistics (total entries, unique keys, by-locale counts, by-format counts, translation status breakdown) are computed via MongoDB aggregation and returned

#### Scenario: Get coverage
- **WHEN** a GET request is made to `/api/stats/coverage` with optional `locale` and `format` parameters
- **THEN** coverage percentages per locale are computed from MongoDB and returned

### Requirement: No file-based persistence
The system SHALL NOT use `FileDataBankService`, `IDataBankService`, or any file-based storage for entry data. The `data-bank.json` file SHALL NOT be read or written by the API.

#### Scenario: API starts without data file
- **WHEN** the API starts and no `data-bank.json` file exists
- **THEN** the API starts normally and returns empty results (not 503)

#### Scenario: No file writes on CRUD
- **WHEN** any CRUD operation is performed via the API
- **THEN** no file system writes occur (only MongoDB writes)
