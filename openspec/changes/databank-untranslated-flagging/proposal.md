## Why

The DataBank JSON output currently provides file-level coverage analysis (missing keys, orphaned keys) via `CoverageAnalyzer`, but there is no way to determine the translation status of individual entries. Consumers of `data-bank.json` need to know per-entry whether a string is translated, untranslated, marked DoNotTranslate, or needs review — enabling downstream tools to filter, report, and act on untranslated content without re-parsing source files.

## What Changes

- Add `IsTranslated` (bool) and `TranslationStatus` (enum: Translated, Untranslated, DoNotTranslate, NeedsReview) to the `EntryMetadata` model
- Introduce a `--flag-untranslated` CLI flag that triggers per-entry translation status analysis after parsing
- When enabled, compare each EN-locale entry's key against every target locale's entries to determine status
- Entries with `DoNotTranslate` metadata set to `true` receive `TranslationStatus.DoNotTranslate` automatically
- EN-locale entries themselves are always marked as `Translated`
- Output JSON includes `isTranslated` and `translationStatus` fields on each entry's metadata
- Add a `TranslationSummary` section to `DataBankOutput` with counts by status (Translated, Untranslated, DoNotTranslate, NeedsReview) per locale and overall

## Capabilities

### New Capabilities

- `entry-translation-status`: Per-entry translation status flagging — adding `IsTranslated` and `TranslationStatus` to entry metadata, the comparison logic to determine status, and the CLI flag to enable it

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- **Models**: `EntryMetadata.cs` gains two new properties; `DataBankOutput.cs` gains a summary section
- **CLI**: `Program.cs` gains `--flag-untranslated` flag handling and post-parse analysis invocation
- **New code**: A new service/class (e.g., `TranslationStatusAnalyzer`) to perform the EN-vs-target comparison
- **Output format**: `data-bank.json` schema changes (additive, non-breaking) — existing consumers ignore new fields
- **No external dependencies** added
