## Why

Translators need to edit translation values and have those changes persist directly back to the source files (.rc, .fhx, .resx, .ahc, .json). Currently, the parsers are read-only — they extract values into a data bank but provide no way to write edits back. This forces translators to manually locate and edit files, risking format corruption and encoding mismatches.

## What Changes

- Add a **file writer** capability that can surgically edit a single translation value in a source file, preserving the file's encoding, format structure, and line endings.
- Add **format-specific replacers** for each supported file format (RC, FHX, RESX, AHC, JSON) that know how to replace a value within a line while preserving surrounding syntax.
- Add an **edit command** to the CLI that takes a key, locale, and new value, looks up the source file and line number, and performs the write-back.
- Add **desktop app write-back**: the WPF/WebView2 desktop app triggers the same file write-back directly from the C# code-behind when a translator edits a value inline, using the source file metadata already present in the loaded data bank.
- Encoding detection (`EncodingDetector`) is reused for both read and write — detected fresh each time, never stored as metadata.

## Capabilities

### New Capabilities
- `translation-writeback`: The ability to edit a translation value and persist the change directly to the correct source file, preserving encoding and format structure.
- `format-replacers`: Format-specific line editing logic for RC, FHX, RESX, AHC, and JSON that replaces a value within a line while preserving surrounding syntax and escaping rules.

### Modified Capabilities
- `ahc-key-generation`: No changes — existing spec is unaffected.

## Impact

- **Code**: New files in `DatabankTool/DataBank.Cli/` — a writer module and format replacers. No changes to existing parsers.
- **CLI**: New command or flag for edit/write-back operation.
- **Desktop**: `DataBank.Desktop` gains a project reference to `DataBank.Cli` and handles a new `writebackEdit` WebView2 message in the C# code-behind.
- **API**: New endpoint for updating a translation value (if API exposure is desired).
- **Dependencies**: None new — reuses existing `EncodingDetector` and `SourceInfo.Line` from current models.
- **Risk**: Low — write-back is a new capability, no existing behavior changes.
