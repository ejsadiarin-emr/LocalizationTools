## 1. Helper Method

- [x] 1.1 Create `FileHelper.cs` in `DatabankTool/DataBank.Cli/Helpers/` with `HasDntInFilename(string filePath)` method
- [x] 1.2 Implement case-insensitive substring check for "DNT" in filename (not full path)

## 2. Parser Integration

- [x] 2.1 Update `RcParser.Parse` to check filename and set `DoNotTranslate = true` for all entries when DNT detected
- [x] 2.2 Update `FhxParser.ParseLine` to check filename and set `DoNotTranslate = true` (preserve existing context-based detection)
- [x] 2.3 Update `AhcParser` to check filename and set `DoNotTranslate = true` for all entries when DNT detected
- [x] 2.4 Update `ResxParser` to check filename and set `DoNotTranslate = true` for all entries when DNT detected
- [x] 2.5 Update `JsonParser` to check filename and set `DoNotTranslate = true` for all entries when DNT detected

## 3. Testing

- [x] 3.1 Add unit test for `FileHelper.HasDntInFilename` with various filename patterns
- [x] 3.2 Add unit test for RC parser with DNT-marked filename
- [x] 3.3 Add unit test for FHX parser with DNT-marked filename (verify both filename and context detection work)
- [x] 3.4 Add integration test with sample DNT files