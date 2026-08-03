## Why

The AHC parser's `GenerateKey` method returns incorrect keys for nested elements. It stops walking the XML ancestor tree at the first element without a `Name` attribute, instead of continuing upward to find the actual named element. This causes `<Text Name="txtLimits">` entries to get key `"Value"` instead of `"txtLimits"`, and Gem element children to get key `"GsLocalizedString"` instead of being skipped entirely.

## What Changes

- Fix `GenerateKey` in `AhcParser.cs` to walk the full ancestor chain and return the first ancestor with a `Name` attribute
- Skip `<Gem>` element descendants during parsing (no valid key structure)
- Skip top-level `<Tag>` and `<Context1Description>` `GsLocalizedString` wrappers (not part of the element key pattern)
- Record keys for `<Text>` and `<CheckBox>` elements even when they have no `LanguageValue` children (empty values), flagged as untranslated with comment "no language value provided"
- Update existing unit tests to match corrected behavior

## Capabilities

### New Capabilities
- `ahc-key-generation`: Correct key derivation from AHC XML element hierarchy

### Modified Capabilities
<!-- None - no existing specs -->

## Impact

- **Code**: `AhcParser.cs` (`GenerateKey` method and `Parse` method)
- **Tests**: `AhcParserTests.cs` (update assertions for corrected keys and entry counts)
- **Output**: `data-bank.json` will produce different keys for AHC entries (correct behavior, but a data change)
