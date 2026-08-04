## Why

The desktop app can browse data from the API in Remote mode, but there is no way to push a new or updated `data-bank.json` from the desktop into the API. Users must currently fall back to `curl` (or Swagger) to upsert data, breaking the desktop-centric workflow.

## What Changes

- Add an "Import Data to API" button to the desktop toolbar that is visible only in Remote mode.
- The button opens a file dialog for selecting a `data-bank.json`, reads the file, and uploads it to the existing `POST /api/import` endpoint (upsert semantics).
- Show progress and result feedback in the status bar (success with entry count, or error message).
- After a successful import, refresh the displayed entries from the API.
- Wire up the already-implemented `ApiClient.ImportJsonAsync` method (currently unused).

## Capabilities

### New Capabilities
- `desktop-import-to-api`: Desktop application can upload a local `data-bank.json` to the API in Remote mode using upsert semantics

### Modified Capabilities
<!-- No existing spec-level behavior changes -->

## Impact

**Affected Code:**
- `DatabankTool/DataBank.Desktop/MainWindow.xaml` — add Import button to toolbar
- `DatabankTool/DataBank.Desktop/MainWindow.xaml.cs` — wire button click to file dialog + `ApiClient.ImportJsonAsync` + refresh entries

**APIs:**
- Existing: `POST /api/import` (unchanged, already supports upsert)

**Dependencies:**
- None new — uses existing `Microsoft.Win32.OpenFileDialog` and `ApiClient`

**Systems:**
- Desktop app remains a pure client; all writes continue through the API
