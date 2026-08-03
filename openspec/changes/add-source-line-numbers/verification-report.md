# Verification Report: add-source-line-numbers

## Summary

| Dimension    | Status |
|--------------|--------|
| Completeness | 23/23 tasks complete, 16/16 requirements implemented |
| Correctness  | 16/16 requirements covered with implementation evidence |
| Coherence    | Design decisions followed, code patterns consistent |

## Requirement Verification

### source-line-numbers spec

| # | Requirement | Verdict | Location |
|---|-------------|---------|----------|
| 1 | SourceInfo includes `int? Line` | ✅ PASS | `SourceInfo.cs:8` |
| 2 | FHX line counter | ✅ PASS | `FhxParser.cs:29-88` |
| 3 | RC line tracking | ✅ PASS | `RcParser.cs:34-461` |
| 4 | RESX IXmlLineInfo | ✅ PASS | `ResxParser.cs:25,68-85` |
| 5 | AHC IXmlLineInfo | ✅ PASS | `AhcParser.cs:23,49` |
| 6 | JSON line finding | ✅ PASS | `JsonParser.cs:69-91` |
| 7 | GRF leaves Line null | ✅ PASS | `GrfParser.cs:23-28` |
| 8 | Line preserved through grouping | ✅ PASS | `EntryGrouper.cs:50` |
| 9 | Line in data-bank.json output | ✅ PASS | `SourceInfo.Line` serializes via System.Text.Json |
| 10 | Line in API responses | ✅ PASS | `ExportEndpoints.cs:37` |
| 11 | Line in MongoDB documents | ✅ PASS | `DataBankEntryDocument.cs:45-46` |
| 12 | Line in import tool | ✅ PASS | `Program.cs:93,181` |

### go-to-source spec

| # | Requirement | Verdict | Location |
|---|-------------|---------|----------|
| 13 | Desktop displays line numbers | ✅ PASS | `app.js:575-577` |
| 14 | "Open Source File" button | ✅ PASS | `app.js:579,596-606` |
| 15 | Opens file at line number | ✅ PASS | `MainWindow.xaml.cs:78-145` (VS Code `code -g file:line`, fallback to shell open) |
| 16 | WebView2 message protocol | ✅ PASS | `app.js:600-604`, `MainWindow.xaml.cs:66-67` |

## Issues

### CRITICAL
None.

### WARNING
None.

### SUGGESTION
1. ~~**Future enhancement**: The `line` property is read and displayed in the status bar, but `Process.Start` with `UseShellExecute = true` doesn't pass the line number to the editor. For VS Code users, a future enhancement could detect VS Code and use `code -g <file>:<line>`.~~ **RESOLVED**: VS Code detection now implemented. `TryOpenWithVsCode` checks for `code` via local install path or `where code`, and uses `code -g file:line` when available. Falls back to shell open for other editors.

## Build & Test
- Build: 0 errors
- Tests: 178/178 passing
- Re-extraction: 609 entries generated with line numbers in all applicable formats

## Final Assessment
All checks passed. Ready for archive.
