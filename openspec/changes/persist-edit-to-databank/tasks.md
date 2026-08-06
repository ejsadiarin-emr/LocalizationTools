## 1. Local Mode Value Persistence

- [x] 1.1 Store `_dataBankPath` (string) and `_dataBankJson` (JsonElement) as instance fields in `MainWindow.xaml.cs`
- [x] 1.2 In `HandleLoadJson()`, save the loaded file path to `_dataBankPath` and the parsed JSON to `_dataBankJson`
- [x] 1.3 Create `PersistEditToLocal(string key, string locale, string newValue, int? newLine)` method in `MainWindow.xaml.cs` that updates `_dataBankJson` (find entry by key, update `values[]`, update `sources[locale].line`) and writes back to `_dataBankPath`
- [x] 1.4 In `HandleWritebackEdit()`, after `FileWriter.EditEntry()` succeeds, call `PersistEditToLocal()` before posting the result back to JS

## 2. Remote Mode Value Persistence

- [x] 2.1 In `HandleWritebackEdit()`, when `_isRemoteMode` is true and FileWriter succeeds, call `PUT /api/entries/{key}/locales/{locale}` via the API client with the new value
- [x] 2.2 Add error handling: if the API call fails, show a warning toast but don't block the write-back result

## 3. Metadata Editing UI

- [x] 3.1 In `app.js`, add editable controls to the detail panel for `comment` (text input), `doNotTranslate` (checkbox), and `formatSpecifiers` (text input)
- [x] 3.2 Wire change/blur events on metadata controls to update the in-memory `entry.metadata` object
- [x] 3.3 On metadata change, send a `persistMetadata` WebView2 message to C# with the entry key and updated metadata fields

## 4. Metadata Persistence

- [x] 4.1 Add `HandlePersistMetadata(JsonElement root)` method in `MainWindow.xaml.cs` that receives metadata updates from JS
- [x] 4.2 In local mode: update `metadata` in `_dataBankJson` for the matching entry and write back to `_dataBankPath`
- [x] 4.3 In remote mode: call `PUT /api/entries/{key}` with the full updated entry to persist metadata to MongoDB
- [x] 4.4 Register the `persistMetadata` message handler in the WebView2 message dispatch switch

## 5. Status Bar Feedback

- [x] 5.1 Update status bar text after data-bank.json auto-save to confirm persistence (e.g., "Saved to data-bank.json")
- [x] 5.2 Show warning if data-bank.json write fails after successful source file write-back
- [x] 5.3 Show warning if remote API call fails after successful source file write-back

## 6. Edge Cases

- [x] 6.1 Handle the case where no file was loaded in local mode (no `_dataBankPath`) — skip data-bank.json persistence gracefully
- [x] 6.2 Handle the case where the entry key is not found in `_dataBankJson` — log and skip persistence
- [x] 6.3 Handle the case where `sources[locale]` doesn't exist in the JSON — skip line number update
