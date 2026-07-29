## ADDED Requirements

### Requirement: Health check endpoint
The system SHALL provide a `GET /api/health` endpoint that returns the API status and basic metadata.

#### Scenario: Healthy API
- **WHEN** client sends `GET /api/health`
- **THEN** system returns `200 OK` with `{ status: "healthy", entryCount: N, version: 2 }`

#### Scenario: API unhealthy
- **WHEN** client sends `GET /api/health` and MongoDB is unreachable
- **THEN** system returns `503 Service Unavailable` with `{ status: "unhealthy", error: "..." }`

### Requirement: Health check for connectivity testing
The Desktop application SHALL use the health check endpoint to verify API connectivity before switching to Remote mode.

#### Scenario: Connectivity test before mode switch
- **WHEN** user attempts to switch to Remote mode
- **THEN** application calls `GET /api/health` first, only switches if response is `200 OK`

#### Scenario: Connectivity test failure
- **WHEN** user attempts to switch to Remote mode and health check fails
- **THEN** application displays error message and remains in current mode
