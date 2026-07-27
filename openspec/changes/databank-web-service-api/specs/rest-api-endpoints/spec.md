## ADDED Requirements

### Requirement: List entries endpoint
The system SHALL provide a GET endpoint at `/api/entries` that returns a list of localized string entries.

#### Scenario: Get all entries
- **WHEN** client sends GET request to `/api/entries`
- **THEN** system returns a JSON array of all LocalizedStringEntry objects with HTTP 200 status

#### Scenario: Filter by locale
- **WHEN** client sends GET request to `/api/entries?locale=en-US`
- **THEN** system returns only entries where Locale property equals "en-US"

#### Scenario: Filter by format
- **WHEN** client sends GET request to `/api/entries?format=resx`
- **THEN** system returns only entries where Source.Format property equals "resx"

#### Scenario: Filter by translation status
- **WHEN** client sends GET request to `/api/entries?status=translated`
- **THEN** system returns only entries where Metadata.TranslationStatus equals "translated"

#### Scenario: Pagination
- **WHEN** client sends GET request to `/api/entries?page=1&pageSize=50`
- **THEN** system returns up to 50 entries and includes pagination metadata in response headers

### Requirement: Get single entry endpoint
The system SHALL provide a GET endpoint at `/api/entries/{id}` that returns a single localized string entry.

#### Scenario: Entry exists
- **WHEN** client sends GET request to `/api/entries/{id}` where id exists
- **THEN** system returns the LocalizedStringEntry object with HTTP 200 status

#### Scenario: Entry does not exist
- **WHEN** client sends GET request to `/api/entries/{id}` where id does not exist
- **THEN** system returns HTTP 404 Not Found status

### Requirement: Create entry endpoint
The system SHALL provide a POST endpoint at `/api/entries` that creates a new localized string entry.

#### Scenario: Valid entry
- **WHEN** client sends POST request to `/api/entries` with valid LocalizedStringEntry body
- **THEN** system creates new entry, returns HTTP 201 Created with Location header and created entry

#### Scenario: Invalid entry
- **WHEN** client sends POST request to `/api/entries` with invalid body (missing required fields)
- **THEN** system returns HTTP 400 Bad Request with validation errors

### Requirement: Update entry endpoint
The system SHALL provide a PUT endpoint at `/api/entries/{id}` that updates an existing localized string entry.

#### Scenario: Entry exists and valid update
- **WHEN** client sends PUT request to `/api/entries/{id}` with valid update body
- **THEN** system updates entry, returns HTTP 200 OK with updated entry

#### Scenario: Entry does not exist
- **WHEN** client sends PUT request to `/api/entries/{id}` where id does not exist
- **THEN** system returns HTTP 404 Not Found

#### Scenario: Invalid update
- **WHEN** client sends PUT request to `/api/entries/{id}` with invalid body
- **THEN** system returns HTTP 400 Bad Request with validation errors

### Requirement: Delete entry endpoint
The system SHALL provide a DELETE endpoint at `/api/entries/{id}` that deletes a localized string entry.

#### Scenario: Entry exists
- **WHEN** client sends DELETE request to `/api/entries/{id}` where id exists
- **THEN** system deletes entry, returns HTTP 204 No Content

#### Scenario: Entry does not exist
- **WHEN** client sends DELETE request to `/api/entries/{id}` where id does not exist
- **THEN** system returns HTTP 404 Not Found

### Requirement: Data provider service
The system SHALL provide a service that loads and serves localization entry data from data-bank.json.

#### Scenario: Service initialization
- **WHEN** application starts
- **THEN** system loads data-bank.json file into memory and provides access through service interface

#### Scenario: Data file not found
- **WHEN** application starts and data-bank.json does not exist
- **THEN** system returns HTTP 503 Service Unavailable for all entry endpoints with descriptive error message

### Requirement: Entry filtering
The system SHALL support combined filtering on multiple criteria.

#### Scenario: Multiple filters
- **WHEN** client sends GET request to `/api/entries?locale=en-US&format=resx&status=translated`
- **THEN** system returns only entries matching all three criteria