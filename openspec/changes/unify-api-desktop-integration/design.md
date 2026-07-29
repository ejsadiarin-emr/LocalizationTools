## Context

The DataBank system currently has three separate write paths to MongoDB:
1. `DataBank.Import` console app (reads data-bank.json, upserts via raw BsonDocument)
2. `DataBank.Api` extract endpoint (parses source files, inserts via typed repository)
3. `DataBank.Desktop` (can write directly to MongoDB in some configurations)

This fragmentation creates race conditions, duplicated logic, and makes the system hard to maintain. The Import tool has its own private DTOs that duplicate `DataBank.Cli.Models`, and uses raw BsonDocument operations instead of the typed repository.

**Current State:**
- CLI tool outputs `data-bank.json` with entries array
- Import tool reads JSON and upserts to MongoDB (standalone console app)
- API has `/api/extract` that parses source files directly (skips JSON)
- Desktop loads JSON directly (temporary phase) or connects to API
- No centralized write gateway

**Reference Pattern (`src/LocalizationAnalyzers.Desktop/`):**
- WPF + WebView2 pattern already proven
- C# code-behind handles file I/O, posts to web layer
- Web frontend calls API directly via `fetch()`

## Goals / Non-Goals

**Goals:**
- API becomes the single write gateway to MongoDB
- Desktop has two clean modes: Local (JSON) and Remote (API)
- Import tool is deprecated and replaced by API endpoint
- Health check endpoint for connectivity testing
- Upsert semantics for import (idempotent re-runs)

**Non-Goals:**
- Removing Import tool immediately (deprecate first, remove later)
- Changing existing read endpoints
- Adding authentication/authorization
- Changing the data-bank.json format
- Modifying CLI tool behavior

## Decisions

**1. API Import Endpoint Design**

- **Decision**: Add `POST /api/import` that accepts `data-bank.json` file upload, deserializes using `DataBank.Cli.Models.DataBankOutput`, and upserts via repository
- **Rationale**: Replaces Import tool with a REST endpoint that can be called from Desktop or CI/CD pipelines
- **Alternatives Considered**:
  - Accept JSON in request body: Works but file upload is more natural for large JSON files
  - Accept source directory path: This is what `/api/extract` does; import is specifically for pre-built JSON
  - Keep Import tool separate: Adds unnecessary complexity and maintenance burden

**2. Upsert Strategy**

- **Decision**: Use `ReplaceOneModel` with `IsUpsert = true` (same as Import tool) instead of `InsertManyAsync`
- **Rationale**: Idempotent — re-running import replaces existing entries instead of failing on duplicates
- **Alternatives Considered**:
  - `InsertManyAsync`: Fails on duplicate keys, not idempotent
  - Delete-then-insert: Risk of data loss during the delete window
  - MongoDB bulk upsert: More efficient but requires manual BsonDocument mapping

**3. Desktop Dual-Mode Architecture**

- **Decision**: Desktop has a toggle switch in settings. Local mode loads JSON via file dialog. Remote mode connects to API. Default to Local mode.
- **Rationale**: Clean separation, no magic detection, user explicitly chooses
- **Alternatives Considered**:
  - Auto-detect API availability: Adds complexity, unreliable if API is slow to respond
  - Always require API: Forces MongoDB setup even for simple local use
  - Always load JSON: No shared state capability

**4. Health Check Endpoint**

- **Decision**: `GET /api/health` returns `{ status: "healthy", entryCount: N, version: 2 }`
- **Rationale**: Desktop can test connectivity before switching to Remote mode, show clear error if API is unreachable
- **Alternatives Considered**:
  - Use existing `/api/entries` endpoint: Returns full dataset, wasteful for connectivity test
  - No health check: Desktop would show generic errors, poor UX

**5. Import Tool Deprecation**

- **Decision**: Mark `DataBank.Import` as `[Obsolete]` in this change, remove in a future change
- **Rationale**: Allows gradual migration, existing scripts continue to work
- **Alternatives Considered**:
  - Remove immediately: Breaking change for any existing automation
  - Keep indefinitely: Confusing, two ways to do the same thing

**6. Desktop API Client**

- **Decision**: Create a simple `ApiClient` service class in Desktop that wraps `HttpClient` calls to API endpoints
- **Rationale**: Centralizes API logic, makes it easy to swap implementations or add retry logic
- **Alternatives Considered**:
  - Direct `fetch()` calls in JavaScript: Works for simple cases, but C# needs to handle auth headers, retry, error mapping
  - No abstraction: Scatter API calls throughout code-behind, hard to maintain

## Risks / Trade-offs

**[Risk] Large JSON file upload** →
- Mitigation: Stream upload instead of loading entire file into memory. Use `MultipartFormDataContent` with stream.
- Trade-off: Slightly more complex implementation

**[Risk] Desktop mode switching mid-session** →
- Mitigation: Mode is a session-level setting. Changing mode requires restart or explicit data reload.
- Trade-off: Less seamless, but avoids complex state management

**[Risk] Import tool deprecation breaks existing scripts** →
- Mitigation: Keep Import tool functional but marked obsolete. Document migration path.
- Trade-off: Maintenance burden until removal

**[Trade-off] No real-time sync** →
- Chose session-based data loading over WebSocket sync
- Acceptable for current use case (teams don't need live updates)

**[Trade-off] File upload instead of JSON body** →
- Chose file upload for better UX with large files
- More complex than simple JSON body, but worth it for UX
