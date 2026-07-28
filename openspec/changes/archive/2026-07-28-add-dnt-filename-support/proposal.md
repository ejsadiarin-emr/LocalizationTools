## Why

The DataBank CLI currently detects "Do Not Translate" entries only from FHX file content (context field containing "do NOT translate"). However, files with "DNT" in their filename (e.g., `DVHExecutive-DNT.rc`, `DVHExecutive-DNT.h`) are also intended to be excluded from translation but are not currently recognized. This means entries from DNT-marked files are incorrectly flagged as needing translation.

## What Changes

- Add filename-based DNT detection: files with "DNT" in the filename (case-insensitive) will have all their entries marked as `DoNotTranslate = true`
- This applies to all parser formats (RC, FHX, AHC, RESX, JSON) that process files
- The detection will check the filename component of the file path for the pattern `*DNT*`

## Capabilities

### New Capabilities
- `filename-dnt-detection`: Detect "Do Not Translate" files by checking if the filename contains "DNT" marker

### Modified Capabilities
- None

## Impact

- Affected parsers: RcParser, FhxParser, AhcParser, ResxParser, JsonParser
- EntryMetadata.DoNotTranslate will be set for all entries in DNT-marked files
- TranslationStatus analysis will correctly classify these entries as DoNotTranslate
- No breaking changes - existing behavior preserved, new detection added