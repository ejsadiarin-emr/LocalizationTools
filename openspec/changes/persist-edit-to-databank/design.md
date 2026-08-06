## Context

The LocalizationTools Desktop app (WPF + WebView2) lets users edit translation values inline. Currently, edits write back to the original source resource files (RC, FHX, RESX, AHC, JSON) via `FileWriter.EditEntry()`, but the changes are **not** persisted to `data-bank.json` (local mode) or MongoDB (remote mode). This means the data-bank.json becomes stale after any edit, and remote mode MongoDB is never updated by the GUI.

The app loads data-bank.json into a `JsonElement` in C# (`MainWindow.xaml.cs`) and forwards it to JavaScript for rendering. C# does not maintain a persistent `DataBankOutput` object — it only stores `_basePath` and the raw JSON element. JavaScript holds the live `allEntries` array.

## Goals / Non-Goals

**Goals:**
- Auto-save value edits to data-bank.json (local) or MongoDB via API (remote) after every FileWriter success
- Make metadata fields (comment, doNotTranslate, formatSpecifiers) editable and persistable
- Update `sources[locale].line` in the in-memory model after FileWriter edits
- Keep changes minimal and follow existing patterns

**Non-Goals:**
- Concurrency/conflict resolution
- Batch or debounced saves (write on every change)
- Editing format specifiers as a structured list (free-text for now)
- Re-extraction or line-number recovery after edits

## Decisions

### 1. Store `_dataBankPath` and `_dataBankJson` in C# for local mode persistence

**Decision**: After loading data-bank.json in local mode, store both the file path (`_dataBankPath`) and the full `JsonElement` (`_dataBankJson`) as instance fields on `MainWindow`. After each successful FileWriter edit, update the in-memory JSON (find the entry by key, update the value in `values[]`, update `sources[locale].line` if shifted), then serialize and write back to `_dataBankPath`.

**Rationale**: The C# side already has the JSON element from `HandleLoadJson()`. Storing it avoids re-reading the file. Updating the JSON element in-place is straightforward with `System.Text.Json`.

**Alternative considered**: Have JS send the full data-bank object back to C# after each edit. Rejected because it duplicates serialization logic and sends large payloads unnecessarily.

### 2. Call existing API endpoints for remote mode persistence

**Decision**: After FileWriter succeeds in remote mode, call `PUT /api/entries/{key}/locales/{locale}` with the new value. The API already handles MongoDB updates. For metadata changes, call `PUT /api/entries/{key}` with the full updated entry.

**Rationale**: API endpoints already exist and work. No new backend code needed for value persistence. Metadata updates use the existing full-entry replace endpoint.

### 3. Add metadata editing to the detail panel in JS

**Decision**: Add editable input fields for `comment`, `doNotTranslate` (checkbox), and `formatSpecifiers` (text input) in the detail panel sidebar. On change, update the in-memory entry and trigger persistence via a new WebView2 message (`persistEdit`).

**Rationale**: The detail panel already displays metadata. Making fields editable there is the natural UI extension. A separate message type (`persistEdit`) keeps concerns separated from value write-back.

### 4. Line number update after FileWriter

**Decision**: The `FileWriter.EditEntry()` result already returns the line number. After a successful edit, update `sources[locale].line` in the in-memory JSON to the new line. This is a best-effort update — if the line shifts, the next edit uses the updated number.

**Rationale**: Simple and sufficient. The FileWriter already targets by line number, and shifts are usually minor. Full line-recovery (re-scanning the file) is overkill for this scope.

### 5. Silent write-back (no dialog for data-bank.json save)

**Decision**: Write data-bank.json silently after each edit, updating the status bar text to confirm. No save dialog — the file path was already chosen at load time.

**Rationale**: Auto-save means no user intervention. The status bar already shows write-back results. A dialog would interrupt rapid editing.

## Risks / Trade-offs

- **[Risk] Frequent file writes on rapid editing** → Mitigated by the fact that data-bank.json is typically < 50MB and writes are fast on local disk. If needed later, debounce with a 500ms timer.
- **[Risk] Line number drift after edits** → Accepted as low-risk. Most edits don't shift lines (same-length replacements). If an edit inserts/deletes lines, the next edit may target the wrong line — user can re-load to reset.
- **[Risk] data-bank.json overwrite loses manual changes** → The file is the source of truth at load time. If someone edits data-bank.json externally while the app is open, those changes will be overwritten. This is acceptable for the current single-user workflow.
- **[Trade-off] Metadata uses `PUT /api/entries/{key}` (full replace)** → The API doesn't have a partial metadata update endpoint. Full replace is safe since we send the complete entry.
