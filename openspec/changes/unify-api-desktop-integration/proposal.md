## Why

The current DataBank architecture has fragmented data paths: the Import tool writes to MongoDB directly, the API's extract endpoint writes to MongoDB, and the Desktop app can write to MongoDB directly. This creates race conditions, duplicated logic, and makes the system hard to maintain. We need to unify the API as the single gateway to MongoDB, with Desktop as a pure client that either loads JSON locally or connects to the API.

## What Changes

- **New API endpoint**: `POST /api/import` accepts `data-bank.json` upload, deserializes using `DataBank.Cli.Models`, upserts entries into MongoDB via repository
- **Desktop dual-mode architecture**: Desktop app has two modes — Local (loads JSON directly from disk) and Remote (connects to API endpoints). User can toggle between modes.
- **Remove Import tool dependency**: The standalone `DataBank.Import` console app is replaced by the API's import endpoint
- **API as single write gateway**: Desktop never writes to MongoDB directly. All persistence goes through API endpoints.
- **Health check endpoint**: `GET /api/health` for Desktop to test API connectivity before switching to Remote mode

## Capabilities

### New Capabilities

- `api-import`: REST endpoint for importing data-bank.json files into MongoDB with upsert semantics
- `desktop-dual-mode`: Desktop application with Local (JSON file) and Remote (API) modes, configurable via settings
- `api-health-check`: Health check endpoint for connectivity testing

### Modified Capabilities

None - this refactors existing functionality without changing spec-level behavior.

## Impact

**Affected Code:**
- `DatabankTool/DataBank.Api/`: New import endpoint, health check endpoint, upsert repository method
- `DatabankTool/DataBank.Desktop/`: Two-mode architecture, API client service, mode toggle UI
- `DatabankTool/DataBank.Import/`: Deprecate (mark as obsolete, optionally remove)

**APIs:**
- New: `POST /api/import` (accepts `data-bank.json` file upload)
- New: `GET /api/health` (returns status and entry count)
- Existing: All read endpoints remain unchanged

**Dependencies:**
- API: No new dependencies (uses existing MongoDB driver and `DataBank.Cli.Models`)
- Desktop: No new dependencies (uses existing `System.Net.Http` for API calls)

**Systems:**
- API becomes the single write gateway to MongoDB
- Desktop becomes a pure client (no direct DB access)
- Import tool is deprecated and can be removed
