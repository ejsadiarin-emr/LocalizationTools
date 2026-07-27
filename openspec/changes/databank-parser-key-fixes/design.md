## Context

The DataBank CLI parses localization files (RESX, RC, FHX, AHC) and generates a unified JSON output. The RC parser currently produces keys that embed the file path and text value, making EN/Translated coverage matching impossible. The CoverageAnalyzer compares keys using exact string matching (`CoverageAnalyzer.cs:47-48`), so any key difference between EN and Translated files results in false "missing" or "orphaned" keys.

Current key formats:
- CAPTION: `CAPTION::{relativePath}::{value}` (e.g., `CAPTION::RC\EN\PCInstall.rc::About DeltaV`)
- Dialog controls: `{controlType}::{relativePath}::{defineName}` (e.g., `LTEXT::RC\EN\PCInstall.rc::IDC_STATIC`)
- STRINGTABLE: `{defineName}` (e.g., `IDS_PRIMARY_OR_SECONDARY_APP_STATION`) — already works correctly

The FHX parser detects locale from parent directory name, producing invalid "translated" locale for non-EN files. The encoding detector doesn't handle RC `#pragma code_page` directives, causing Unicode corruption. The `ExtractQuotedString` method uses `LastIndexOf('"')` which fails with escaped quotes.

## Goals / Non-Goals

**Goals:**
- Redesign RC parser keys to be path-independent and value-independent, enabling correct EN/Translated coverage matching
- Fix FHX locale detection to produce valid BCP47 locale codes
- Fix encoding detection to handle RC `#pragma code_page` directives
- Fix `ExtractQuotedString` to handle escaped quotes correctly
- Document GRF & iFix DLL as future work in README

**Non-Goals:**
- Implementing GRF (OLE) or iFix DLL (.NET assembly) parsers — these are binary formats requiring specialized libraries
- Changing the CoverageAnalyzer's matching logic — the fix is in key generation, not comparison
- Modifying the RESX or AHC parsers — they already work correctly

## Decisions

### Decision 1: RC Key Format — Use Dialog Name Instead of File Path

**Choice**: Use dialog name from DIALOGEX header (e.g., `IDD_ABOUTBOX`) instead of `relativePath`.

**New key formats**:
- CAPTION: `CAPTION::{dialogName}` (e.g., `CAPTION::IDD_ABOUTBOX`)
- Dialog controls: `{controlType}::{dialogName}::{defineName}` (e.g., `LTEXT::IDD_ABOUTBOX::IDC_STATIC`)
- STRINGTABLE: `{defineName}` (unchanged — already works)

**Alternatives considered**:
- Positional keys (e.g., `CAPTION::0`): Rejected because positional indices are fragile and hard to debug.
- Hash-based keys: Rejected because they're opaque and make manual inspection impossible.

**Rationale**: Dialog names are stable identifiers that exist in both EN and Translated RC files. They're human-readable and don't depend on file path or text value.

### Decision 2: IDC_STATIC Disambiguation — Use Positional Index

**Choice**: For controls with `IDC_STATIC` (or any repeated ID in the same dialog), append a positional index: `LTEXT::IDD_ABOUTBOX::3`.

**Alternatives considered**:
- Skip IDC_STATIC controls: Rejected because they contain localizable text (e.g., "Version 5.2", "Select Workstation Type").
- Use text value in key: Rejected because this reintroduces the value-dependency problem.

**Rationale**: Positional indices are deterministic (same dialog structure produces same keys) and don't depend on file path or text value.

### Decision 3: FHX Locale — Detect from Content or Require --locale

**Choice**: Try to detect locale from the file content (e.g., look for language-specific patterns), fall back to `--locale` override. If neither is available, log a warning and use "unknown".

**Alternatives considered**:
- Parse locale from sibling files: Rejected because the directory structure isn't standardized.
- Hardcode locale mapping: Rejected because it's fragile and doesn't scale.

**Rationale**: FHX files don't contain explicit locale metadata. The most reliable approach is to require `--locale` for non-EN files, with content-based detection as a best-effort fallback.

### Decision 4: Encoding — Parse #pragma code_page

**Choice**: Add a method to `EncodingDetector` that parses `#pragma code_page(N)` from RC files and maps the codepage number to a .NET encoding name.

**Alternatives considered**:
- Always use UTF-8: Rejected because RC files may use Windows-1252 or other codepages.
- Use BOM-only detection: Rejected because many RC files lack BOM.

**Rationale**: `#pragma code_page` is the standard way RC files declare encoding. It's reliable and widely used.

### Decision 5: Quote Parsing — State Machine

**Choice**: Replace `LastIndexOf('"')` with a state machine that tracks quote boundaries and handles `""` escape sequences.

**Alternatives considered**:
- Regex-based parsing: Rejected because regex is overkill for this simple case and harder to maintain.
- Use `IndexOf` with offset: Rejected because it doesn't handle escaped quotes.

**Rationale**: A state machine is simple, efficient, and handles all edge cases correctly.

## Risks / Trade-offs

- **Breaking change**: RC key format changes will break downstream consumers relying on old keys. Mitigation: This is a CLI tool, not a library. The key format is an internal implementation detail.
- **Test coverage**: Existing tests use the old key format. Mitigation: Update all affected tests to expect new key format.
- **FHX locale detection**: Content-based detection may not work for all files. Mitigation: Log warnings and require `--locale` as fallback.
- **Encoding detection**: `#pragma code_page` may not be present in all RC files. Mitigation: Fall back to BOM detection, then UTF-8.
