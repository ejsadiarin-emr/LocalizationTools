## Why

The Databank CLI tool's file detection logic is hardcoded and incomplete. FHX files are detected by filename (`AlarmWords.txt`) rather than format, JSON files use a rigid `translate.*.json` pattern, and the coverage analyzer doesn't recognize `.fhx` or `.grf` extensions. This makes the tool brittle — it fails on real-world folder structures where FHX data lives in both `.fhx` and `.txt` files across differently named directories. The detection logic is also scattered across multiple files (Program.cs, CoverageAnalyzer.cs, JsonParser.cs, MainWindow.xaml.cs), making maintenance error-prone.

## What Changes

- **Centralized file detection**: Create a `FileDetector` helper that encapsulates all file type detection logic in one place, replacing scattered `Directory.GetFiles` + hardcoded patterns.
- **FHX detection by directory name**: Detect FHX format by checking if the file resides in a directory named `Fhx` (case-insensitive), supporting both `.fhx` and `.txt` extensions.
- **Content-based fallback**: When extension and directory name are ambiguous (e.g., `.txt` files), peek at file content to distinguish FHX (`@Key@\t` format) from plain text.
- **JSON pattern flexibility**: Support `translate.*.json` and other JSON naming conventions without hardcoding the prefix.
- **Coverage analyzer fixes**: Add `.fhx` and `.grf` to `IsSupportedFormat` so translated files are properly paired with EN source files.
- **Desktop app GRF support**: Update hardcoded `l10n-files/GRF` path in MainWindow.xaml.cs to use dynamic detection.

## Capabilities

### New Capabilities
- `file-detection`: Centralized file type detection using extension, directory name, and content-based fallback. Replaces scattered detection logic across Program.cs, CoverageAnalyzer.cs, and JsonParser.cs.

### Modified Capabilities

(No existing specs to modify — this is the first spec.)

## Impact

- **Files modified**: `Program.cs`, `CoverageAnalyzer.cs`, `JsonParser.cs`, `MainWindow.xaml.cs`, `FileHelper.cs`
- **New files**: `FileDetector.cs` (or similar centralized helper)
- **Tests**: Existing `FhxParserTests.cs`, `CoverageAnalyzerTests.cs`, `IntegrationTests.cs` — must continue passing. New tests for FileDetector.
- **Breaking changes**: None — all existing file types continue to work. Detection becomes more robust, not different.
