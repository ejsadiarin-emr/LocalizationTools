## Context

The DataBank CLI (`dv-extract`) parses localization files (resx, rc, fhx, ahc) into a unified `data-bank.json` output. Currently, each `LocalizedStringEntry` has an `EntryMetadata` with comment, RC info, format specifiers, and `DoNotTranslate` — but no indication of whether the string is actually translated. The existing `CoverageAnalyzer` provides file-level coverage (missing/orphaned keys per file pair) but does not annotate individual entries.

Consumers of `data-bank.json` need per-entry translation status to power dashboards, filter untranslated strings, and drive automation without re-parsing source files.

## Goals / Non-Goals

**Goals:**
- Add `IsTranslated` (bool) and `TranslationStatus` (enum) to `EntryMetadata`
- Add `--flag-untranslated` CLI flag to opt into per-entry analysis
- Compare EN keys against each target locale to determine status
- Add `TranslationSummary` to `DataBankOutput` with counts by status
- Keep output format additive and non-breaking

**Non-Goals:**
- Changing existing `CoverageAnalyzer` behavior or output
- Detecting fuzzy/partial translations (only exact key match)
- Modifying the `DoNotTranslate` detection logic
- Supporting multiple source locales (EN is the only reference)
- Performance optimization for very large datasets (current approach is sufficient for typical localization projects)

## Decisions

### Decision: Analysis runs as post-parse step, not during parsing

**Choice**: Perform translation status analysis in a separate pass after all entries are parsed, invoked from `Program.Main` when `--flag-untranslated` is set.

**Rationale**: Parsing is format-specific and locale-unaware (each parser produces entries independently). Coupling translation analysis to parsing would require each parser to know about other locales. A post-parse step operates on the complete entry list, making EN-key comparison straightforward.

**Alternative considered**: Annotate entries during parsing — rejected because parsers don't have access to entries from other files/locales at parse time.

### Decision: New TranslationStatusAnalyzer class

**Choice**: Create `DatabankTool/DataBank.Cli/TranslationStatusAnalyzer.cs` with a static `Analyze(List<LocalizedStringEntry> entries)` method returning a `TranslationSummary`.

**Rationale**: Keeps translation logic isolated and testable. Follows the existing pattern of `CoverageAnalyzer` (static class, takes entries, returns report). The analyzer mutates `EntryMetadata` in-place on the entry list and returns summary data.

**Alternative considered**: Adding logic directly to `Program.Main` — rejected for testability and separation of concerns.

### Decision: DoNotTranslate takes precedence over key matching

**Choice**: If `Metadata.DoNotTranslate` is `true`, the entry receives `TranslationStatus.DoNotTranslate` regardless of whether a matching EN key exists.

**Rationale**: `DoNotTranslate` is an explicit author intent. An entry marked DNT should never appear as "translated" or "needs review" — it's intentionally excluded from translation. Checking DNT first avoids misleading status.

### Decision: Untranslated is summary-only, not per-entry

**Choice**: An EN key with no matching entry in a target locale contributes to the `Untranslated` count in the summary but does not create a synthetic entry.

**Rationale**: Creating phantom entries would inflate the entry list and confuse consumers. The summary already captures "how many EN keys are missing from locale X." Individual entries in the target locale that exist are either `Translated` (key matches EN) or `NeedsReview` (key doesn't match EN — possibly orphaned or from a different source).

### Decision: Enum serialized as string

**Choice**: Serialize `TranslationStatus` as its string name (`"translated"`, `"untranslated"`, etc.) using `JsonStringEnumConverter`.

**Rationale**: Matches existing camelCase JSON convention. String values are self-documenting in the output file. The `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` already applied to serialization handles casing.

### Decision: TranslationSummary is nullable

**Choice**: `DataBankOutput.TranslationSummary` is `null` when `--flag-untranslated` is not used, and populated when it is.

**Rationale**: Keeps the output identical to current behavior when the flag is off. Consumers that don't use the feature see no change. Avoids adding empty summary objects to every output.

## Risks / Trade-offs

- **[Risk] Large entry sets slow down O(n*m) comparison** → Mitigation: Build a `HashSet<string>` of EN keys for O(1) lookup. Total complexity is O(n) per target locale where n is total entries. Acceptable for typical localization project sizes (< 100K entries).

- **[Risk] Locale detection may be imperfect** → Mitigation: Use existing locale extraction from file paths/parsers (already handled). The analyzer trusts the `Locale` property set during parsing.

- **[Risk] NeedsReview may conflate orphaned keys with legitimate entries** → Mitigation: This is intentional — an entry in a target locale whose key doesn't match any EN key is genuinely suspicious and worth flagging. The existing `CoverageReport.OrphanedKeys` provides complementary file-level detail.

- **[Trade-off] In-place mutation of EntryMetadata** → Accepted: Analyzer modifies entries directly. This is consistent with how other post-processing works in the codebase and avoids copying large lists.
