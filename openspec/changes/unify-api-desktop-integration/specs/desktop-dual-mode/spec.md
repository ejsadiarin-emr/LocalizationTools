## ADDED Requirements

### Requirement: Desktop application modes
The Desktop application SHALL support two modes: Local mode (loads JSON directly) and Remote mode (connects to API).

#### Scenario: Default mode
- **WHEN** user launches Desktop for the first time
- **THEN** application starts in Local mode

#### Scenario: Mode toggle
- **WHEN** user changes mode setting in the application
- **THEN** application switches to the selected mode and reloads data accordingly

### Requirement: Local mode operation
In Local mode, the Desktop application SHALL load `data-bank.json` directly from the local filesystem without connecting to any API.

#### Scenario: Load JSON file
- **WHEN** user clicks "Load DataBank JSON" button in Local mode
- **THEN** application shows file dialog, reads selected JSON file, parses entries, and displays them in the UI

#### Scenario: No API connection in Local mode
- **WHEN** application is in Local mode
- **THEN** application does not make any HTTP requests to the API

### Requirement: Remote mode operation
In Remote mode, the Desktop application SHALL connect to the DataBank API and fetch data from MongoDB.

#### Scenario: Connect to API
- **WHEN** application switches to Remote mode
- **THEN** application calls `GET /api/health` to verify connectivity, then fetches entries via `GET /api/entries`

#### Scenario: API unreachable
- **WHEN** application is in Remote mode and API is unreachable
- **THEN** application displays error message with option to retry or switch to Local mode

### Requirement: Mode persistence
The Desktop application SHALL persist the selected mode setting between sessions.

#### Scenario: Mode remembered on restart
- **WHEN** user changes mode and restarts the application
- **THEN** application starts in the previously selected mode

### Requirement: Data consistency across modes
Both modes SHALL display the same data structure and support the same UI features (filtering, search, pagination).

#### Scenario: Same UI in both modes
- **WHEN** user switches between Local and Remote modes
- **THEN** application displays entries with the same columns, filters, and search functionality
