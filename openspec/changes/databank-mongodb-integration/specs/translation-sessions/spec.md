## ADDED Requirements

### Requirement: GET /api/sessions endpoint
The API SHALL expose a `GET /api/sessions` endpoint that returns all TranslationSession documents. The endpoint SHALL support optional query parameter `status` to filter by session status.

#### Scenario: Get all sessions
- **WHEN** client sends `GET /api/sessions`
- **THEN** the API returns a JSON array of all sessions with HTTP 200

#### Scenario: Filter by status
- **WHEN** client sends `GET /api/sessions?status=pending`
- **THEN** the API returns only sessions where Status equals "pending"

### Requirement: GET /api/sessions/{id} endpoint
The API SHALL expose a `GET /api/sessions/{id}` endpoint that returns a single session by its ObjectId.

#### Scenario: Session exists
- **WHEN** client sends `GET /api/sessions/{id}` and the session exists
- **THEN** the API returns the session with HTTP 200

#### Scenario: Session not found
- **WHEN** client sends `GET /api/sessions/{id}` and no session matches
- **THEN** the API returns HTTP 404

### Requirement: POST /api/sessions endpoint
The API SHALL expose a `POST /api/sessions` endpoint that creates a new translation session. The request body SHALL contain SessionName, SourceLocale, TargetLocale. Status SHALL default to "pending". CreatedAt and UpdatedAt SHALL be set to current UTC time.

#### Scenario: Create session
- **WHEN** client sends `POST /api/sessions` with valid data
- **THEN** the API creates the session with Status="pending", sets timestamps, returns HTTP 201

### Requirement: PUT /api/sessions/{id}/status endpoint
The API SHALL expose a `PUT /api/sessions/{id}/status` endpoint that updates the session status. Valid status transitions: pending → in-progress, in-progress → completed. The UpdatedAt timestamp SHALL be refreshed.

#### Scenario: Start session
- **WHEN** client sends `PUT /api/sessions/{id}/status` with `{"status": "in-progress"}` and current status is "pending"
- **THEN** the API updates Status to "in-progress" and refreshes UpdatedAt

#### Scenario: Complete session
- **WHEN** client sends `PUT /api/sessions/{id}/status` with `{"status": "completed"}` and current status is "in-progress"
- **THEN** the API updates Status to "completed" and refreshes UpdatedAt

#### Scenario: Invalid transition
- **WHEN** client sends `PUT /api/sessions/{id}/status` with an invalid transition (e.g., completed → pending)
- **THEN** the API returns HTTP 400 with error message describing valid transitions

### Requirement: POST /api/sessions/{id}/entries endpoint
The API SHALL expose a `POST /api/sessions/{id}/entries` endpoint that adds entry IDs to a session's EntryIds array.

#### Scenario: Add entries to session
- **WHEN** client sends `POST /api/sessions/{id}/entries` with `{"entryIds": ["id1", "id2"]}`
- **THEN** the API adds the IDs to the session's EntryIds array

#### Scenario: Add to non-existent session
- **WHEN** client sends `POST /api/sessions/{id}/entries` and no session matches
- **THEN** the API returns HTTP 404

### Requirement: DELETE /api/sessions/{id} endpoint
The API SHALL expose a `DELETE /api/sessions/{id}` endpoint that removes a session.

#### Scenario: Delete session
- **WHEN** client sends `DELETE /api/sessions/{id}` and the session exists
- **THEN** the API deletes the session and returns HTTP 204

#### Scenario: Delete non-existent session
- **WHEN** client sends `DELETE /api/sessions/{id}` and no session matches
- **THEN** the API returns HTTP 404
