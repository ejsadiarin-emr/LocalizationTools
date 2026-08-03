## 1. Fix GenerateKey

- [x] 1.1 Update `GenerateKey` in `AhcParser.cs` to walk the full ancestor chain, skipping elements without `Name` attributes, and returning the first ancestor with a `Name` attribute
- [x] 1.2 Add Gem ancestor detection: if the walk reaches a `<Gem>` element before finding a named key, return a skip sentinel (e.g., `null`)

## 2. Handle Top-Level Skips

- [x] 2.1 In `GenerateKey`, if the walk reaches `<ContextualDisplay>` (root) without finding a `Name` attribute on a child element, return the element's `LocalName` only for `<Title>` and `<Description>` (top-level elements)
- [x] 2.2 Ensure `<Tag>` and `<Context1Description>` at root level produce no entries (their ancestor chain hits root without a usable named element)

## 3. Record Empty-Value Keys

- [x] 3.1 After parsing `LanguageValue` elements, scan `<ContainedElements>` for `<Text Name="...">` and `<CheckBox Name="...">` elements that have no `LanguageValue` descendants
- [x] 3.2 For each such element, emit one `RawLocalizedEntry` per locale with empty value, `IsTranslated = false`, and `Comment = "no language value provided"`

## 4. Update Tests

- [x] 4.1 Update `AhcParserTests.cs` to assert correct key names (`txtLimits`, `txtAlarms`, etc.) instead of `"Value"`
- [x] 4.2 Update entry count assertion (expected raw count with empty-value keys included)
- [x] 4.3 Add test for empty-value key recording (e.g., `txtEnab` has empty entries with correct metadata)
- [x] 4.4 Add test verifying Gem descendants are not parsed

## 5. Verify

- [x] 5.1 Run `dotnet test --filter "AhcParserTests"` and confirm all tests pass
- [x] 5.2 Run `dotnet run --project DatabankTool/DataBank.Cli -- --input-dir ./l10n-files --format ahc` and verify data-bank.json has correct keys
