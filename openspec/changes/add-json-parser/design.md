## Context

The `dv-extract` CLI tool (`DataBank.Cli`) extracts localized strings from multiple file formats (RESX, RC, FHX, AHC) into a unified JSON output. The `l10n-files/` directory also contains flat JSON translation files (`translate.en.json`, `translate.zh.json`) with a simple `{ "key": "value" }` structure. These files are not currently parsed, creating a gap in localization coverage analysis.

Existing parsers follow a consistent pattern: a static class with a `Parse(filePath, ...) → List<LocalizedStringEntry>` method. The JSON parser should follow this same convention.

## Goals / Non-Goals

**Goals:**
- Parse flat `{ "key": "value" }` JSON translation files
- Detect locale from filename pattern `translate.<locale>.json`
- Produce `LocalizedStringEntry` objects consistent with other parsers
- Integrate into `Program.cs` discovery loop and `CoverageAnalyzer`

**Non-Goals:**
- Parsing nested JSON structures (only flat key-value)
- Parsing JSON arrays or mixed-type values
- Supporting arbitrary JSON schemas beyond the `translate.*.json` convention
- Handling JSON5 or JSONC (comments, trailing commas)

## Decisions

### 1. File discovery pattern: `translate.*.json`
**Decision**: Match files named `translate.<locale>.json` in any subdirectory.
**Rationale**: The sample files follow this exact naming convention. It's specific enough to avoid accidentally parsing unrelated JSON files (e.g., `package.json`, `tsconfig.json`). The glob pattern `**/translate.*.json` is used in `Program.cs`.
**Alternative considered**: Match all `*.json` — rejected because it would pick up non-translation JSON files.

### 2. Locale detection: filename-based
**Decision**: Extract locale from the part between `translate.` and `.json` in the filename.
**Rationale**: Consistent with how `ResxParser` detects locale from filename suffixes. The content itself is just key-value pairs with no language metadata.
**Example**: `translate.zh.json` → locale `zh`, `translate.en.json` → locale `en`.

### 3. Library: `System.Text.Json` (built-in)
**Decision**: Use `System.Text.Json.JsonDocument` for parsing.
**Rationale**: Already used in `Program.cs` for output serialization. No additional NuGet dependency needed. `JsonDocument` provides a lightweight DOM approach suitable for flat key-value parsing.
**Alternative considered**: `System.Text.Json.Serialization.JsonSerializer` deserialization into a `Dictionary<string, string>` — also viable but `JsonDocument` gives more control over error handling for malformed files.

### 4. Value handling: strings only
**Decision**: Skip non-string values (numbers, booleans, arrays, objects) with a warning.
**Rationale**: The `LocalizedStringEntry.Value` is typed as `string`. Non-string values in translation files are likely mistakes or edge cases not relevant to localization.

### 5. `DoNotTranslate` detection: none for JSON
**Decision**: JSON entries default to `DoNotTranslate = false`.
**Rationale**: Unlike FHX which has an explicit "do NOT translate" context field, flat JSON files have no such metadata. All entries are assumed translatable.

## Risks / Trade-offs

- **[Risk] Accidentally parsing non-translation JSON files** → Mitigated by requiring `translate.*.json` naming pattern.
- **[Risk] Malformed JSON files** → Mitigated by try/catch with warning (matches existing parser pattern).
- **[Trade-off] No nested JSON support** → Accepted because the actual l10n files are flat. Can be extended later if needed.
