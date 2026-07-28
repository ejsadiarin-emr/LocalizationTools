## Context

The Databank CLI tool processes localization files in multiple formats (RESX, RC, FHX, AHC, JSON). File detection logic is scattered across four files with hardcoded patterns:

- `Program.cs:113-123` — FHX detected by exact filename `AlarmWords.txt`
- `Program.cs:137` — JSON detected by `translate.*.json` glob
- `CoverageAnalyzer.cs:144` — `IsSupportedFormat` missing `.fhx` and `.grf`
- `MainWindow.xaml.cs:112` — GRF path hardcoded to `l10n-files/GRF`

Real-world data uses both `.fhx` and `.txt` extensions for FHX format. The reliable signal is the directory name `Fhx`, not the file extension.

## Goals / Non-Goals

**Goals:**
- Centralize all file type detection into a single `FileDetector` class
- Detect FHX by directory name (`Fhx`) supporting both `.fhx` and `.txt` extensions
- Add content-based fallback for ambiguous files (e.g., `.txt` that might be FHX or plain text)
- Fix coverage analyzer to recognize `.fhx` and `.grf` formats
- Make JSON detection flexible (not tied to `translate.` prefix)
- Keep all existing parsers working unchanged

**Non-Goals:**
- Adding a GRF parser (no parser exists; desktop app just lists files)
- Changing parser internals (FhxParser, JsonParser, etc. stay as-is)
- Restructuring the project layout

## Decisions

### 1. FileDetector as a static helper in `Helpers/`

**Decision**: Create `FileDetector.cs` in `DataBank.Cli/Helpers/` with static methods for file discovery.

**Why static helper over service/DI**: The existing codebase uses static parsers and helpers (e.g., `FileHelper.HasDntInFilename`). A static class matches the established pattern and avoids adding DI complexity to a CLI tool.

**Alternatives considered**:
- Extension methods on `string` — rejected because detection involves directory context, not just the file path
- Instance service with DI — over-engineered for a CLI tool with no existing DI container

### 2. Detection priority: extension → directory name → content

**Decision**: Three-tier detection with early exit:

```
1. Extension match (.fhx → FHX, .ahc → AHC, .rc → RC, .resx → RESX)
2. Directory name match (parent dir is "Fhx" → FHX regardless of extension)
3. Content peek (first line matches @Key@\t pattern → FHX)
```

**Why this order**: Extension is cheapest. Directory name is the reliable real-world signal for FHX. Content peek is expensive (requires reading the file) so it's the fallback.

### 3. Content peek reads first line only

**Decision**: Read only the first line for content-based detection. FHX files start with `@Key@\t"context"\tValue` on line 1.

**Why first line only**: Minimizes I/O. The format signature is unambiguous on line 1.

### 4. CoverageAnalyzer uses FileDetector for format checks

**Decision**: Replace the hardcoded `IsSupportedFormat` extension check with a call to `FileDetector.DetectFormat()` which returns a known format string or null.

**Why**: Single source of truth. When new formats are added, only FileDetector needs updating.

### 5. JSON locale detection stays in JsonParser

**Decision**: The `DetectLocale` method in JsonParser.cs stays where it is. FileDetector handles discovery (which files to parse), not locale extraction (which is parse-specific).

**Why**: Locale detection is intrinsic to the JSON format's semantics, not a cross-cutting concern.

## Risks / Trade-offs

- **Performance**: Content peek adds a file read per ambiguous file. Mitigated by only doing it for files that don't match by extension or directory. In practice, most files will match early.

- **False positives on content detection**: A `.txt` file starting with `@Key@` that isn't FHX could be misidentified. Mitigation: The `@Key@\t` tab-separated pattern is highly specific to FHX format. Risk is negligible.

- **Desktop app GRF path**: The hardcoded `l10n-files/GRF` path in MainWindow.xaml.cs is specific to the desktop app's deployment structure. Changing it to use FileDetector could break the desktop app's expected layout. Mitigation: Leave the desktop app's GRF path as-is; it's a different context (WPF app, not CLI). Only fix the CLI's detection logic.

- **Existing tests**: Tests reference `AlarmWords.txt` paths. Mitigation: Tests should still pass because FileDetector will find those files via directory name or content. Verify after implementation.
