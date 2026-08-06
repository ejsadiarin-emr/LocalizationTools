## Why

When a user edits a translation value in the Desktop GUI (local or remote mode), the change is written back to the source resource file but **not** persisted to `data-bank.json` (local mode) or MongoDB (remote mode). This means:
- In local mode, `data-bank.json` becomes stale immediately after an edit — values and metadata diverge from the source files.
- In remote mode, MongoDB is never updated by the Desktop GUI — only the API has write access, creating a disconnected editing experience.
- Metadata fields (comments, doNotTranslate, formatSpecifiers) are displayed but completely non-editable and non-persistable.

This change makes edits durable across both storage backends, so data-bank.json / MongoDB always reflect the current state.

## What Changes

- **Local mode auto-save**: After every successful FileWriter edit, the in-memory `DataBankOutput` is re-serialized and written back to the loaded `data-bank.json` file (or imported file path).
- **Remote mode auto-save**: After every successful FileWriter edit, the Desktop GUI calls the existing API (`PUT /api/entries/{key}/locales/{locale}`) to update MongoDB with the new value.
- **Metadata persistence**: Metadata fields (comment, doNotTranslate, formatSpecifiers) become editable in the detail panel UI, and changes persist to data-bank.json (local) or via API (remote).
- **Line number update**: After a FileWriter edit, `sources[locale].line` is updated in the in-memory model to reflect any line shifts from the source file write.

## Capabilities

### New Capabilities
- `edit-persistence`: Persisting edit results (values and metadata) back to the storage backend — data-bank.json in local mode, MongoDB via API in remote mode. Auto-save on every change.

### Modified Capabilities
- (none — existing specs for import and key generation are unaffected)

## Impact

- **C# Desktop app** (`MainWindow.xaml.cs`): `HandleWritebackEdit()` must call persistence logic after FileWriter succeeds; new methods for metadata editing.
- **JS frontend** (`app.js`): Detail panel UI gains editable metadata fields; `saveEdit()` triggers persistence path.
- **API client** (`ApiClient`): May need additional methods or reuse of existing `PUT /api/entries/{key}/locales/{locale}` for remote mode persistence.
- **Data model** (`DataBankOutput`, `LocalizedStringEntry`): In-memory model is the source of truth for serializing back to data-bank.json.
- **File system**: `data-bank.json` is written after every edit in local mode — I/O consideration for rapid editing sessions.
