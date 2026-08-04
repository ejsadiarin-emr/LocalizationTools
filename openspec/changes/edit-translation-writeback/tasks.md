## 1. Encoding & Line Utilities

- [x] 1.1 Add `DetectLineEnding(string content)` method to `EncodingDetector` that returns `\r\n` or `\n` based on the file content
- [x] 1.2 Add `ReadFileWithMetadata(string filePath)` method to `EncodingDetector` that returns a tuple of `(string content, Encoding encoding, string lineEnding)` — reads file with detected encoding and also detects line ending style

## 2. Format Replacers

- [x] 2.1 Create `IFormatReplacer` interface with `string? ReplaceLine(string line, string oldValue, string newValue)` method — returns null if old value not found
- [x] 2.2 Implement `RcReplacer` — replaces quoted string value in RC lines (STRINGTABLE, DIALOG CAPTION, DIALOG CONTROL), handles `L` prefix and `""` escaping
- [x] 2.3 Implement `FhxReplacer` — replaces value after second tab in tab-delimited FHX lines
- [x] 2.4 Implement `ResxReplacer` — replaces text content of `<value>` XML elements, handles XML entity escaping
- [x] 2.5 Implement `AhcReplacer` — replaces text content of `<LanguageValue>` XML elements
- [x] 2.6 Implement `JsonReplacer` — replaces string value in `"key": "value"` JSON lines, handles JSON escape sequences
- [x] 2.7 Create `FormatReplacerFactory` that returns the correct replacer based on `SourceInfo.Format`

## 3. File Writer

- [x] 3.1 Create `FileWriter` class with `EditEntry(RawLocalizedEntry entry, string newValue)` method
- [x] 3.2 Implement read-modify-write cycle: detect encoding → read file → locate line by `Source.Line` → verify old value → replace using format replacer → write back with same encoding and line endings
- [x] 3.3 Add safety check: abort and report error if old value not found at target line
- [x] 3.4 Add safety check: abort and report error if format replacer returns null (value mismatch)

## 4. CLI Integration

- [x] 4.1 Add `edit` command to CLI that accepts `--key`, `--locale`, `--value`, and `--file` (data bank JSON path) arguments
- [x] 4.2 Implement lookup logic: find entry in data bank by key and locale, extract `Source.File`, `Source.Line`, `Source.Format`, and current value
- [x] 4.3 Wire up `FileWriter.EditEntry()` to the CLI command
- [x] 4.4 Add confirmation output showing what will be changed before writing

## 5. Tests

- [x] 5.1 Write unit tests for `RcReplacer` covering: basic replace, L prefix, escaped quotes, DIALOG CONTROL lines, old value mismatch
- [x] 5.2 Write unit tests for `FhxReplacer` covering: basic replace, old value mismatch
- [x] 5.3 Write unit tests for `ResxReplacer` covering: basic replace, XML entity escaping, old value mismatch
- [x] 5.4 Write unit tests for `AhcReplacer` covering: basic replace, old value mismatch
- [x] 5.5 Write unit tests for `JsonReplacer` covering: basic replace, JSON escape sequences, old value mismatch
- [x] 5.6 Write integration tests for `FileWriter` using sample files: verify round-trip (read → edit → write → re-read produces correct value and preserved encoding)
- [x] 5.7 Write integration tests verifying encoding preservation: UTF-8 file stays UTF-8, UTF-16LE file stays UTF-16LE after edit

## 6. Desktop Integration

- [x] 6.1 Add `ProjectReference` from `DataBank.Desktop` to `DataBank.Cli` to reuse `FileWriter` and models
- [x] 6.2 Add `writebackEdit` WebView2 message action handled in `CoreWebView2_WebMessageReceived` that builds a `RawLocalizedEntry` from source metadata and calls `FileWriter.EditEntry()`
- [x] 6.3 Post the `EditResult` back to the frontend (`window.receiveWritebackResult`) and surface result text in the WPF status bar
- [x] 6.4 Trigger write-back from inline edit in `app.js`; resolve the entry's source file/line/format, skip when no source metadata exists
- [x] 6.5 Add a toast notification element (`index.html` + `styles.css`) to report write-back success/failure in the UI
