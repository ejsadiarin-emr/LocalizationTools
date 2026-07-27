## ADDED Requirements

### Requirement: GET /api/entries endpoint
The API SHALL expose a `GET /api/entries` endpoint that returns all DataBankEntry documents. The endpoint SHALL support optional query parameters: `locale` (filter by locale), `format` (filter by source format), `key` (filter by key substring).

#### Scenario: Get all entries
- **WHEN** client sends `GET /api/entries`
- **THEN** the API returns a JSON array of all entries with HTTP 200

#### Scenario: Filter by locale
- **WHEN** client sends `GET /api/entries?locale=en`
- **THEN** the API returns only entries where Locale equals "en"

#### Scenario: Filter by format
- **WHEN** client sends `GET /api/entries?format=rc`
- **THEN** the API returns only entries where Source.Format equals "rc"

#### Scenario: Filter by key substring
- **WHEN** client sends `GET /api/entries?key=About`
- **THEN** the API returns only entries where Key contains "About"

### Requirement: GET /api/entries/{id} endpoint
The API SHALL expose a `GET /api/entries/{id}` endpoint that returns a single entry by its ID.

#### Scenario: Entry exists
- **WHEN** client sends `GET /api/entries/{id}` and the entry exists
- **THEN** the API returns the entry with HTTP 200

#### Scenario: Entry not found
- **WHEN** client sends `GET /api/entries/{id}` and no entry matches
- **THEN** the API returns HTTP 404 with error message

### Requirement: POST /api/entries endpoint
The API SHALL expose a `POST /api/entries` endpoint that creates a new entry. The request body SHALL contain Key, Value, Locale, Source, and Metadata fields. The API SHALL generate the ID from the entry properties.

#### Scenario: Create valid entry
- **WHEN** client sends `POST /api/entries` with valid entry data
- **THEN** the API creates the entry, returns HTTP 201 with the created entry

#### Scenario: Create duplicate key
- **WHEN** client sends `POST /api/entries` with a Key that already exists
- **THEN** the API returns HTTP 409 Conflict

### Requirement: PUT /api/entries/{id} endpoint
The API SHALL expose a `PUT /api/entries/{id}` endpoint that updates an existing entry.

#### Scenario: Update existing entry
- **WHEN** client sends `PUT /api/entries/{id}` with updated data
- **THEN** the API updates the entry and returns HTTP 200 with the updated entry

#### Scenario: Update non-existent entry
- **WHEN** client sends `PUT /api/entries/{id}` and no entry matches
- **THEN** the API returns HTTP 404

### Requirement: DELETE /api/entries/{id} endpoint
The API SHALL expose a `DELETE /api/entries/{id}` endpoint that removes an entry.

#### Scenario: Delete existing entry
- **WHEN** client sends `DELETE /api/entries/{id}` and the entry exists
- **THEN** the API deletes the entry and returns HTTP 204

#### Scenario: Delete non-existent entry
- **WHEN** client sends `DELETE /api/entries/{id}` and no entry matches
- **THEN** the API returns HTTP 404

### Requirement: GET /api/metadata endpoint
The API SHALL expose a `GET /api/metadata` endpoint that returns the DataBankMetadata document.

#### Scenario: Get metadata
- **WHEN** client sends `GET /api/metadata`
- **THEN** the API returns the metadata document with HTTP 200

### Requirement: GET /api/entries/count endpoint
The API SHALL expose a `GET /api/entries/count` endpoint that returns the total number of entries, optionally filtered by locale.

#### Scenario: Count all entries
- **WHEN** client sends `GET /api/entries/count`
- **THEN** the API returns JSON with `count` field containing total entries

#### Scenario: Count by locale
- **WHEN** client sends `GET /api/entries/count?locale=de`
- **THEN** the API returns count of entries where Locale equals "de"

### Requirement: Swagger/OpenAPI documentation
The API SHALL serve Swagger UI at `/swagger` and OpenAPI JSON at `/swagger/v1/swagger.json`.

#### Scenario: Access Swagger UI
- **WHEN** client navigates to `/swagger`
- **THEN** the Swagger UI page is displayed with all API endpoints documented
