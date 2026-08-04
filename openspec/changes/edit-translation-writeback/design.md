## Context

The DatabankTool currently parses localization files (.rc, .fhx, .resx, .ahc, .json) into a data bank for translation management. Parsers are read-only — they extract values but provide no way to write edits back. Translators need to edit translation values and have those changes persist directly to the source files.

Key existing infrastructure:
- `EncodingDetector` — detects file encoding via BOM and `#pragma code_page` directives
- `SourceInfo` — tracks `Format`, `File`, `Path`, and `Line` for each entry
- Format-specific parsers already know the grammar of each file format

The main challenge is that files have different encodings (UTF-8, UTF-16LE, ANSI with various code pages) and format-specific escaping rules. Writing back incorrectly would corrupt files.

## Goals / Non-Goals

**Goals:**
- Allow editing a single translation value and writing it back to the source file
- Preserve file encoding, BOM, line endings, and format structure on write-back
- Reuse existing `EncodingDetector` for encoding detection (detected fresh per write, not stored)
- Use `SourceInfo.Line` for surgical line-level edits (no global search-and-replace)
- Support all text-based formats: RC, FHX, RESX, AHC, JSON

**Non-Goals:**
- GRF binary file write-back (out of scope)
- Bulk search-and-replace across multiple files
- Concurrent edit support (desktop app, single user)
- Encoding normalization (don't change a file's encoding, preserve as-is)
- Storing encoding as metadata in the data bank

## Decisions

### Decision 1: Immediate write-back (no batching)

**Choice:** Write changes to the file immediately when an edit is submitted.

**Alternatives considered:**
- Batched writes (queue edits, apply on "Save") — rejected: adds complexity, no concurrency benefit in desktop app
- Overlay/patch files — rejected: adds indirection, translators don't see changes in source files

**Rationale:** Desktop app, single user, no concurrent writes. Immediate write-back is simpler and gives translators instant feedback.

### Decision 2: Line-number-based surgical edits

**Choice:** Use `SourceInfo.Line` to locate the exact line to edit, then format-aware replace within that line.

**Alternatives considered:**
- Global string replacement in file — rejected: could match wrong occurrences, unsafe
- Re-parse entire file on each edit — rejected: expensive, unnecessary when line numbers are known
- Key-based search near line number — considered as fallback but adds complexity

**Rationale:** Line numbers are already tracked by parsers. With immediate write-back (no concurrent edits), line numbers stay valid. If a line doesn't contain the expected old value, abort (safety check).

### Decision 3: Format-specific replacers

**Choice:** One replacer per format (RcReplacer, FhxReplacer, etc.) that knows how to replace a value within a line.

**Rationale:** Each format has different syntax:
- RC: quoted strings with `L` prefix, `""` escaping
- FHX: tab-delimited, value after 2nd tab
- RESX: XML `<value>` elements
- AHC: XML `<LanguageValue>` elements
- JSON: `"key": "value"` pairs

A small, focused replacer per format is simpler and more reliable than a generic approach.

### Decision 4: Encoding detected fresh per write

**Choice:** Call `EncodingDetector.Detect()` before each write, use the detected encoding for both read and write.

**Alternatives considered:**
- Store encoding in `SourceInfo` — rejected: adds metadata that goes stale, encoding is a file property not an entry property
- Assume UTF-8 for all files — rejected: would corrupt UTF-16LE files (RC translated files are UTF-16LE)

**Rationale:** Encoding is already detected during parsing. Reusing the same detection at write time is one extra function call, not added complexity. Preserves the existing model (no new fields).

### Decision 5: Desktop app writes back directly from code-behind

**Choice:** The WPF/WebView2 desktop app invokes `FileWriter.EditEntry()` directly from the C# code-behind when a translator edits a value inline. A new `writebackEdit` WebView2 message carries the key, locale, old value, new value, and the entry's source metadata (`file`, `line`, `format`) from JavaScript; the code-behind resolves the file path against the loaded `basePath`, constructs a `RawLocalizedEntry`, calls `FileWriter`, and posts the `EditResult` back to JavaScript for a toast.

**Alternatives considered:**
- Write via the API (persist to MongoDB + write-back endpoint) — deferred: user opted to skip this for now; the API only updates MongoDB and has no file-write logic.
- Add write-back to the API as the only path — rejected for now because local-mode desktop writes should not depend on the API being up.

**Rationale:** The desktop app already references `_basePath` and parses source metadata for "Open Source File". Reusing `FileWriter` (shared via a project reference from `DataBank.Desktop` to `DataBank.Cli`) gives identical write-back behavior to the CLI without API dependency. In remote mode the data comes from MongoDB and source files may not exist locally, so write-back is best-effort and fails gracefully with a toast.

## Risks / Trade-offs

- **Line number staleness** → Mitigated by immediate write-back. If a file is externally edited between parse and write, the line number could be wrong. The safety check (verify old value at target line) catches this and aborts rather than corrupting.

- **Format replacer edge cases** → Each format has quirks (RC line continuations, RESX multi-line elements). Initial implementation handles the common cases; edge cases can be added incrementally.

- **Encoding detection failure** → `EncodingDetector` falls back to UTF-8. If a file is actually in a legacy encoding without BOM, the write could corrupt it. Mitigated by the fact that most real files have BOM or `#pragma code_page`. Legacy encoding files are rare in practice.
