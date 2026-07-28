## 1. Create FileDetector Helper

- [x] 1.1 Create `DatabankTool/DataBank.Cli/Helpers/FileDetector.cs` with a `DetectFormat` method that returns a format string (e.g., "resx", "rc", "fhx", "ahc", "json", "grf", or null)
- [x] 1.2 Implement extension-based detection: `.resx` → "resx", `.rc` → "rc", `.fhx` → "fhx", `.ahc` → "ahc", `.json` → "json", `.grf` → "grf"
- [x] 1.3 Implement directory-name detection: if parent directory name is "Fhx" (case-insensitive) → "fhx"
- [x] 1.4 Implement content-based fallback: read first line, check for `@Key@\t` pattern → "fhx"
- [x] 1.5 Add `DiscoverFiles` method that returns all supported files with their detected formats using `SearchOption.AllDirectories`

## 2. Update Program.cs File Discovery

- [x] 2.1 Replace FHX file discovery block (lines 113-123) to use `FileDetector.DiscoverFiles` filtered for "fhx" format
- [x] 2.2 Replace JSON file discovery block (lines 135-143) to use `FileDetector.DiscoverFiles` filtered for "json" format
- [x] 2.3 Replace RESX file discovery block (lines 93-101) to use `FileDetector.DiscoverFiles` filtered for "resx" format
- [x] 2.4 Replace RC file discovery block (lines 103-111) to use `FileDetector.DiscoverFiles` filtered for "rc" format
- [x] 2.5 Replace AHC file discovery block (lines 125-133) to use `FileDetector.DiscoverFiles` filtered for "ahc" format
- [x] 2.6 Verify verbose logging still prints relative paths for each discovered file

## 3. Update CoverageAnalyzer

- [x] 3.1 Update `IsSupportedFormat` in `CoverageAnalyzer.cs` to use `FileDetector.DetectFormat` instead of hardcoded extension list
- [x] 3.2 Ensure `.fhx` and `.grf` files are now recognized as supported formats

## 4. Add Unit Tests

- [x] 4.1 Create `DatabankTool/DataBank.Cli.Tests/FileDetectorTests.cs` with test cases for extension-based detection
- [x] 4.2 Add test cases for directory-name FHX detection (both `.fhx` and `.txt` in `Fhx` dir)
- [x] 4.3 Add test cases for content-based fallback detection
- [x] 4.4 Add test case for unsupported file type returning null
- [x] 4.5 Run existing tests (`FhxParserTests`, `CoverageAnalyzerTests`, `IntegrationTests`) and verify they pass

## 5. Verify No Regressions

- [x] 5.1 Build the solution and confirm no compile errors
- [x] 5.2 Run full test suite and confirm all tests pass
- [x] 5.3 Manually verify that existing `AlarmWords.txt` files in `Fhx` directories are still detected correctly
