## Context

The Desktop app (WPF + WebView2) has an `ApiClient` with an `ImportJsonAsync(string filePath)` method that already calls `POST /api/import` with multipart upload and parses the response (entry count or error). This method is currently unused. The toolbar in `MainWindow.xaml` already toggles button visibility based on Local vs Remote mode via `ModeRadio_Checked`.

## Goals / Non-Goals

**Goals:**
- Provide an "Import Data to API" button visible only in Remote mode.
- Allow selecting a local `data-bank.json`, uploading it to `POST /api/import` (upsert), and showing result feedback.
- Refresh the entry list from the API after a successful import.

**Non-Goals:**
- No changes to the API import endpoint (already supports upsert).
- No changes to Local mode behavior.
- No drag-and-drop or batch import.

## Decisions

- **Reuse existing `ApiClient.ImportJsonAsync`** rather than adding new client code. It already handles multipart upload, response parsing, and error extraction.
- **Add a plain WPF `Button`** next to the other toolbar buttons, initially `Visibility="Collapsed"`, shown only when `RemoteModeRadio` is checked (mirrors existing `ConnectApiBtn` pattern).
- **Disable the button while importing** to prevent duplicate concurrent uploads, and show "Importing..." in the status bar.
- **Reload entries after success** by calling `LoadEntriesFromApi()` so the UI reflects the upserted data.
- Use the existing `Microsoft.Win32.OpenFileDialog` pattern (same as `HandleLoadJson`) with a `.json` filter.

## Risks / Trade-offs

- [Large file uploads block the UI thread] → Import runs `async` and only the file read/upload is awaited; button disabled during import.
- [API unreachable during import] → `ImportJsonAsync` already returns `(false, 0, error)` instead of throwing; surface the error in the status bar.
- [Import success but reload fails] → Reload catch already sets an error status and shows Retry; import result is still reported first.
