## Why

Users need to quickly navigate back to the exact location in a source file where a localization key appears. Currently, `SourceInfo` records the file path but not the line number, making it impossible to jump directly to the relevant line. This is needed for a "go to source" feature in the Desktop app that opens the source file at the exact line number.

## What Changes

- Add `Line` property (nullable `int?`) to the `SourceInfo` model in `DataBank.Cli`
- Update all parsers to capture line numbers during parsing:
  - **FhxParser**: Trivial — add a line counter in the existing `foreach` loop
  - **RcParser**: Track physical line numbers; handle line continuations (backslash-joined lines) by recording the starting line of each entry
  - **ResxParser**: Use `IXmlLineInfo` on `XElement` to get XML line numbers
  - **AhcParser**: Same `IXmlLineInfo` approach as Resx
  - **JsonParser**: Post-process — after parsing, search raw file text for each key to find its line number
  - **GrfParser**: No line numbers (one entry per file, not line-based)
- Propagate `Line` through `EntryGrouper`, `DataBankOutput`, and the data-bank.json output
- Add `Line` to `SourceInfoDocument` (MongoDB model), `SourceInfoInput` (Import tool), and API export response
- Add "Open Source File" button in the Desktop detail panel that sends a message to C# to open the file at the given line number
- Display line numbers in the Desktop detail panel source section

## Capabilities

### New Capabilities
- `source-line-numbers`: Line number tracking in SourceInfo across all parsers, data models, API, and MongoDB storage
- `go-to-source`: Desktop app feature to open source files at a specific line number, with a new WebView2→C# message protocol for file opening

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Data model**: `SourceInfo` gains a new nullable property — non-breaking for consumers that ignore unknown fields
- **Parsers**: FhxParser, RcParser, ResxParser, AhcParser, JsonParser all need updates
- **Output**: data-bank.json entries will include `"line": 42` in source objects (additive, no version bump needed)
- **API**: Export endpoint and entries endpoint responses will include `line` in source objects
- **MongoDB**: `SourceInfoDocument` gains `Line` field; existing documents without it will deserialize as `null`
- **Import tool**: `SourceInfoInput` gains `Line` field
- **Desktop**: New "Open Source File" button in detail panel; new C# handler using `System.Diagnostics.Process.Start`
- **Desktop frontend**: `app.js` detail panel renders line numbers and sends `openSourceFile` message to C#
