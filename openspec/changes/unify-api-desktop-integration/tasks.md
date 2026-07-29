## 1. API Import Endpoint

- [x] 1.1 Add `POST /api/import` endpoint to `ExtractionEndpoints.cs` that accepts file upload
- [x] 1.2 Implement file upload handling using `IFormFile` and stream reading
- [x] 1.3 Deserialize JSON using `DataBank.Cli.Models.DataBankOutput` (shared model)
- [x] 1.4 Add `ReplaceOrInsertManyAsync` method to `IDataBankRepository` with upsert semantics
- [x] 1.5 Implement upsert logic using `ReplaceOneModel` with `IsUpsert = true`
- [x] 1.6 Update metadata document after successful import
- [x] 1.7 Add request validation (file not null, valid JSON, required fields)
- [x] 1.8 Return appropriate response models (success/error)

## 2. API Health Check Endpoint

- [x] 2.1 Add `GET /api/health` endpoint to `ExtractionEndpoints.cs`
- [x] 2.2 Implement health check that tests MongoDB connectivity
- [x] 2.3 Return `{ status: "healthy", entryCount: N, version: 2 }` on success
- [x] 2.4 Return `503 Service Unavailable` with error message on failure

## 3. Desktop Dual-Mode Architecture

- [x] 3.1 Create `ApiClient.cs` service class in Desktop project
- [x] 3.2 Implement `CheckHealthAsync()` method that calls `GET /api/health`
- [x] 3.3 Implement `FetchEntriesAsync()` method that calls `GET /api/entries`
- [x] 3.4 Implement `ImportJsonAsync(string filePath)` method that calls `POST /api/import`
- [x] 3.5 Add mode setting persistence using `ApplicationSettings` or JSON config
- [x] 3.6 Add mode toggle UI element in MainWindow.xaml (RadioButton or ToggleSwitch)
- [x] 3.7 Implement mode switching logic in C# code-behind
- [x] 3.8 Implement Local mode: file dialog → read JSON → parse → post to WebView2
- [x] 3.9 Implement Remote mode: health check → fetch entries → post to WebView2
- [x] 3.10 Add error handling for API connectivity failures in Remote mode
- [x] 3.11 Add "Retry" and "Switch to Local Mode" buttons in error state

## 4. Import Tool Deprecation

- [x] 4.1 Add `[Obsolete("Use POST /api/import instead")]` attribute to `DataBank.Import` project
- [x] 4.2 Update README or documentation to note deprecation
- [x] 4.3 Keep Import tool functional for backward compatibility

## 5. Testing and Verification

- [x] 5.1 Test API import with valid data-bank.json file
- [x] 5.2 Test API import with invalid JSON (should return 400)
- [ ] 5.3 Test API import with empty entries array (should return success)
- [x] 5.4 Test API import idempotency (re-import same file)
- [x] 5.5 Test health check endpoint (healthy and unhealthy states)
- [ ] 5.6 Test Desktop Local mode: load JSON file and display entries
- [ ] 5.7 Test Desktop Remote mode: connect to API and fetch entries
- [ ] 5.8 Test mode switching between Local and Remote
- [x] 5.9 Test mode persistence across application restarts
- [ ] 5.10 Test error handling when API is unreachable in Remote mode
- [ ] 5.11 Verify both modes display same UI features (filtering, search, pagination)
