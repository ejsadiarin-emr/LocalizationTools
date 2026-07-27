## Why

The `dv-extract` CLI tool parses RESX, RC, FHX, and AHC localization files but has no support for flat JSON translation files (`translate.en.json`, `translate.zh.json`). These JSON files follow a simple `{ "key": "value" }` structure and are used alongside the other formats in the `l10n-files/` directory. Without a JSON parser, the tool cannot extract strings from these files into the unified `data-bank.json` output, creating a gap in coverage.

## What Changes

- Add a new `JsonParser` that parses flat key-value JSON translation files into `LocalizedStringEntry` objects
- Extend `Program.cs` to discover and parse `*.json` files matching the `translate.*.json` naming convention
- Add a `"json"` format option to the `--format` flag
- Add `"json"` to the supported format list in `CoverageAnalyzer`

## Capabilities

### New Capabilities
- `json-parser`: Parse flat `{ "key": "value" }` JSON translation files, detect locale from filename (`translate.en.json` → `en`), and produce `LocalizedStringEntry` objects consistent with other parsers

### Modified Capabilities
- (none)

## Impact

- **Files to modify**: `DataBank.Cli/Program.cs` (file discovery loop), `DataBank.Cli/Helpers/CoverageAnalyzer.cs` (supported format list)
- **Files to create**: `DataBank.Cli/Parsers/JsonParser.cs`
- **Dependencies**: None — uses `System.Text.Json` which is built into .NET 8
- **No breaking changes**: Purely additive; existing parsers unaffected
