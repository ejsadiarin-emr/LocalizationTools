## ADDED Requirements

### Requirement: Frontend connects to DataBank API
The web frontend (inside WebView2) SHALL establish connection to DataBank REST API endpoints.

#### Scenario: API base URL configuration
- **WHEN** web frontend starts
- **THEN** system reads API base URL from configuration (default `http://localhost:5000`)
- **AND** configuration is defined in `app.js` as a constant or passed from C# code-behind

#### Scenario: API connection test
- **WHEN** frontend attempts to connect to API on startup
- **THEN** system sends GET request to `/api/entries` with pageSize=1
- **AND** if connection fails, displays error message with configurable API URL prompt

### Requirement: Frontend fetches entries from API
The web frontend SHALL retrieve localization entries from the API via fetch().

#### Scenario: Fetch all entries
- **WHEN** user navigates to table view or dashboard
- **THEN** web frontend fetches all entries from `GET /api/entries` and stores in memory

#### Scenario: Entries cached in memory
- **WHEN** entries are fetched from API
- **THEN** web frontend stores full entries array in a JavaScript variable for client-side filtering/search

#### Scenario: Refresh re-fetches from API
- **WHEN** user clicks refresh or navigates to a view
- **THEN** web frontend re-fetches entries from API and updates in-memory cache

### Requirement: Frontend fetches statistics from API
The web frontend SHALL retrieve or compute coverage statistics.

#### Scenario: Compute statistics client-side
- **WHEN** user navigates to dashboard
- **THEN** web frontend fetches all entries from `GET /api/entries`
- **AND** computes coverage statistics client-side (total, translated count, untranslated count, etc.)

#### Scenario: Per-locale statistics
- **WHEN** dashboard loads
- **THEN** web frontend groups entries by locale and computes per-locale translation completion percentages

### Requirement: Frontend handles API errors gracefully
The web frontend SHALL handle API errors and display appropriate messages.

#### Scenario: Network error handling
- **WHEN** API request fails due to network error
- **THEN** web frontend displays error message with retry button

#### Scenario: API error response handling
- **WHEN** API returns error status code (4xx, 5xx)
- **THEN** web frontend displays error message with status code and response body

#### Scenario: Loading states display
- **WHEN** API request is in progress
- **THEN** web frontend displays loading spinner or skeleton screen

### Requirement: Frontend supports CORS with file:// origin
The web frontend SHALL function correctly when loaded from `file:///` protocol by WebView2.

#### Scenario: CORS considerations
- **WHEN** web frontend loaded from `file:///` makes fetch() request to API
- **THEN** API must accept requests from `file://` origin OR web frontend uses C# code-behind as HTTP proxy via WebMessageReceived

#### Scenario: Fallback to C# proxy if CORS blocks requests
- **WHEN** direct fetch() fails due to CORS
- **THEN** web frontend falls back to requesting data via `window.chrome.webview.postMessage()` to C# code-behind which proxies the API call
