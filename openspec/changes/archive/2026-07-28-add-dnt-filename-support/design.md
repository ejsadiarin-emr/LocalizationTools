## Context

The DataBank CLI parses localization files (RC, FHX, AHC, RESX, JSON) into a unified `data-bank.json` output. Each entry has an `EntryMetadata` with a `DoNotTranslate` boolean property. Currently, `DoNotTranslate` is only set by the FHX parser when the context field contains "do NOT translate".

Files with "DNT" in the filename (e.g., `DVHExecutive-DNT.rc`, `DVHExecutive-DNT.h`) are also intended to be excluded from translation, but this convention is not detected by any parser.

## Goals / Non-Goals

**Goals:**
- Detect files with "DNT" in the filename and mark all their entries as `DoNotTranslate = true`
- Apply detection consistently across all parser formats (RC, FHX, AHC, RESX, JSON)
- Preserve existing FHX "do NOT translate" content-based detection

**Non-Goals:**
- Changing the existing FHX content-based DNT detection
- Adding new CLI flags or configuration options for DNT
- Modifying the output format of `data-bank.json`

## Decisions

### 1. Detection Pattern: Filename contains "DNT" (case-insensitive)

**Decision**: Check if the filename (not full path) contains the substring "DNT" using case-insensitive comparison.

**Alternatives considered**:
- Regex pattern `*DNT*` - Overkill for simple substring match
- Exact match `-DNT.` - Too restrictive, misses variations like `_DNT_` or `DNT-`

**Rationale**: Simple substring match covers the observed patterns (`DVHExecutive-DNT.rc`, `DVHExecutive-DNT.h`) while being flexible enough for future variations.

### 2. Implementation Location: Shared helper method

**Decision**: Create a static helper method `HasDntInFilename(string filePath)` in a shared location that all parsers can call.

**Alternatives considered**:
- Duplicate detection logic in each parser - Violates DRY
- Add to `EntryMetadata` - Metadata is per-entry, not per-file

**Rationale**: Centralized helper ensures consistent behavior and single point of maintenance.

### 3. Application Point: File-level override

**Decision**: Apply `DoNotTranslate = true` to all entries from a DNT-marked file, overriding any per-entry detection.

**Alternatives considered**:
- Merge with per-entry detection (OR logic) - More complex, no benefit since file-level DNT is absolute
- Let per-entry detection take precedence - Contradicts user intent

**Rationale**: If a file is marked DNT, all entries in it should be DNT regardless of content.

## Risks / Trade-offs

- **Risk**: False positives if "DNT" appears in filenames for unrelated reasons → **Mitigation**: Document the convention; users can rename files if needed
- **Risk**: Existing DNT files not currently flagged will change behavior → **Mitigation**: This is the intended fix; no rollback needed