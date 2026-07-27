## 1. Create JsonParser

- [x] 1.1 Create `DataBank.Cli/Parsers/JsonParser.cs` with static `Parse(string filePath, string? rootDir)` method returning `List<LocalizedStringEntry>`
- [x] 1.2 Implement locale detection from filename: extract locale from `translate.<locale>.json` pattern
- [x] 1.3 Implement flat JSON parsing using `System.Text.Json.JsonDocument`: iterate top-level properties, skip non-string values, produce `LocalizedStringEntry` with `Format = "json"` and `Id = "json::{relativePath}::{key}"`

## 2. Integrate into CLI

- [x] 2.1 Add JSON file discovery block in `Program.cs` (search for `translate.*.json` files, call `JsonParser.Parse()`)
- [x] 2.2 Add `"json"` to the `--format` flag handling in `Program.cs`

## 3. Coverage Support

- [x] 3.1 Add `.json` to the `IsSupportedFormat` method in `CoverageAnalyzer.cs`

## 4. Verify

- [x] 4.1 Run `dotnet build` to confirm no compilation errors
- [x] 4.2 Run `dotnet test` to confirm existing tests still pass
