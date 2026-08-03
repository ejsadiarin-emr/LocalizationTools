## Context

The DataBank CLI parses localization files (FHX, RC, RESX, AHC, JSON, GRF) into a unified `data-bank.json` output. Each entry's `SourceInfo` currently records `Format`, `File`, and `Path` — but not the line number where the key appears in the source file. The Desktop app shows a detail panel with source info but has no way to navigate to the source.

The Desktop app is a WPF + WebView2 hybrid. The WebView2 hosts an HTML/JS frontend that communicates with C# via `WebMessageReceived` / `ExecuteScriptAsync`. Currently, the only messages from JS→C# are `loadJson`, `connectApi`, and `retryConnection`.

## Goals / Non-Goals

**Goals:**
- Add `Line` (nullable `int?`) to `SourceInfo` across the full data pipeline (CLI → data-bank.json → API → MongoDB → Desktop)
- Track line numbers in all line-based parsers (FHX, RC) and XML parsers (RESX, AHC) with reasonable accuracy
- For JSON parser, find line numbers via post-processing (text search in raw file)
- GRF parser: no line numbers (not applicable)
- Add "Open Source File" button in Desktop detail panel that opens the file at the given line number using the system's default editor

**Non-Goals:**
- Editing source files from within the Desktop app
- Syntax highlighting or code rendering in the app
- Guaranteeing 100% accuracy for line numbers in RC files with complex line continuations (recording the starting line of the entry is sufficient)
- Version bumping data-bank.json (the field is additive and nullable)

## Decisions

### 1. SourceInfo.Line as nullable int

**Decision**: Add `public int? Line { get; set; }` to `SourceInfo`.

**Rationale**: Nullable because GRF entries have no meaningful line number. Consumers that don't know about `Line` will get `null`, which is safe. No breaking change.

**Alternatives considered**:
- `int` with default 0 — conflates "no line info" with "line 1", ambiguous
- Separate `StartLine`/`EndLine` — over-engineered for the use case; we only need first-match location

### 2. Parser strategies for line tracking

| Parser | Strategy | Notes |
|--------|----------|-------|
| FhxParser | Counter in foreach loop | Increment for each non-empty line, pass to SourceInfo |
| RcParser | Track physical line number in main loop | `NormalizeContent` loses line numbers; track counter alongside the `foreach` over `lines` (post-normalization). Record the line number of the first physical line that started the entry. For line continuations, track the start line before merging. |
| ResxParser | `IXmlLineInfo` on `XElement` | `dataElement is IXmlLineInfo info && info.HasLineInfo()` → `info.LineNumber`. Works because we use `XDocument.Load(filePath)`. |
| AhcParser | Same as Resx | `IXmlLineInfo` on `LanguageValue` elements |
| JsonParser | Post-process text search | After parsing, for each key, search `content` for `"<key>"` and count newlines up to that position. O(n) per entry but acceptable for typical JSON file sizes. |
| GrfParser | null | One entry per file, no meaningful line |

### 3. RcParser line continuation handling

**Decision**: Refactor `NormalizeContent` to return `List<(string line, int startLineNumber)>` so the caller knows which physical line each logical line starts at.

**Rationale**: The current `NormalizeContent` joins continuation lines (lines ending with `\`). We need the starting physical line of each entry. By returning the start line number alongside each normalized line, we preserve this information without changing the continuation-joining behavior.

**Alternative considered**: Track line numbers externally by counting `\n` in the raw content string. More fragile, harder to maintain.

### 4. Desktop "Open Source File" mechanism

**Decision**: Add a new WebView2 message type `openSourceFile` with `{ action: "openSourceFile", filePath: "...", line: 42 }`. The C# handler uses `System.Diagnostics.Process.Start` with the file path. For line number support, use the approach of opening the file in the default editor (most text editors and VS Code will accept a file path; some support `file:line` notation).

**Rationale**: Simple, no dependency on specific editors. VS Code's `code --goto file:line` could be a future enhancement but is out of scope.

**Alternative considered**: Register a custom URI scheme or use VS Code API — over-engineered for initial implementation.

### 5. No data-bank.json version bump

**Decision**: Keep `version: 3`. The `line` field is additive and nullable.

**Rationale**: Existing consumers will deserialize `line` as `null` if they don't have the property, or ignore it. No schema break.

## Risks / Trade-offs

- **[RC line accuracy]** → Line continuations may cause the recorded line to be the start of the multi-line construct, not the exact line of the value text. Mitigation: Acceptable for "go to source" — user lands at the right area.
- **[JSON post-processing perf]** → Searching raw text for each key is O(keys × file_size). Mitigation: JSON files in this project are small (< 1000 entries). Could optimize with a pre-built line index if needed.
- **[Xml line info availability]** → `IXmlLineInfo` returns 0 if the XML was parsed from a string or if line info wasn't collected. Mitigation: We use `XDocument.Load(filePath)` which preserves line info; check `HasLineInfo()` before using.
- **[Default editor limitations]** → Not all editors support opening at a specific line via command line. Mitigation: Open the file without line number as fallback; document that VS Code users can configure `code --goto`.
