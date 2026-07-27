## Why

The DataBank CLI's RC parser generates keys that embed the file path and text value, making EN/Translated coverage matching impossible. This accounts for the 74.4% RC coverage (expected ~100%) and the 67 missing + 121 orphaned keys. Additionally, FHX locale detection produces invalid "translated" locale codes, encoding detection corrupts Unicode characters, and quote parsing has edge cases with escaped quotes.

## What Changes

- **RC parser key redesign**: Remove `relativePath` from dialog control keys. Use dialog name (from DIALOGEX header) instead of file path. Use positional index for IDC_STATIC controls to disambiguate collisions.
- **FHX locale detection**: Add locale detection from file content or require `--locale` for non-EN files. At minimum, document the limitation.
- **Encoding corruption fix**: Parse `#pragma code_page(N)` directives from RC files to detect correct encoding. Add encoding validation warnings.
- **ExtractQuotedString fix**: Replace `LastIndexOf('"')` with a proper state-machine quote parser that handles escaped quotes correctly.
- **GRF & iFix DLL documentation**: Note these binary formats as future work in `src/README.md`.

## Capabilities

### New Capabilities
- `rc-key-redesign`: Redesign RC parser key generation to use dialog names and positional indices instead of file paths and values, enabling correct EN/Translated coverage matching.
- `fhx-locale-detection`: Improve FHX locale detection to produce valid BCP47 locale codes instead of directory names.
- `encoding-detection-fix`: Fix encoding detection to handle RC `#pragma code_page` directives and prevent Unicode character corruption.
- `quote-parsing-fix`: Replace fragile `LastIndexOf('"')` quote extraction with a proper state-machine parser.

### Modified Capabilities

## Impact

- **Files modified**: `RcParser.cs`, `FhxParser.cs`, `EncodingDetector.cs`, `src/README.md`
- **Test files modified**: `RcParserTests.cs`, `FhxParserTests.cs`, `EncodingDetectorTests.cs`
- **Breaking changes**: RC key format changes will break any downstream consumers relying on the old key format (`CAPTION::{path}::{value}`, `{controlType}::{path}::{id}`). The new format will be `CAPTION::{dialogName}`, `{controlType}::{dialogName}::{id}`.
- **Coverage impact**: RC coverage should increase from 74.4% to ~100% after key redesign.
