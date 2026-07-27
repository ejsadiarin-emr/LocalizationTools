## ADDED Requirements

### Requirement: EntryMetadata includes translation status fields
The `EntryMetadata` model SHALL include an `IsTranslated` boolean property and a `TranslationStatus` enum property. The `TranslationStatus` enum SHALL have four values: `Translated`, `Untranslated`, `DoNotTranslate`, `NeedsReview`.

#### Scenario: New fields are present on deserialized entry
- **WHEN** a `LocalizedStringEntry` is deserialized from `data-bank.json`
- **THEN** its `Metadata` object SHALL contain `isTranslated` (boolean) and `translationStatus` (string enum value)

#### Scenario: Default values when flag-untranslated is not used
- **WHEN** the `--flag-untranslated` CLI flag is not provided
- **THEN** `IsTranslated` SHALL default to `false` and `TranslationStatus` SHALL default to `Untranslated` for all entries

### Requirement: CLI flag --flag-untranslated
The CLI SHALL accept a `--flag-untranslated` boolean flag. When provided, the system SHALL perform per-entry translation status analysis after parsing all entries.

#### Scenario: Flag is provided
- **WHEN** the user runs `dv-extract --flag-untranslated <dir>`
- **THEN** the system SHALL analyze each entry's translation status and populate `IsTranslated` and `TranslationStatus` on every entry's metadata

#### Scenario: Flag is not provided
- **WHEN** the user runs `dv-extract <dir>` without `--flag-untranslated`
- **THEN** the system SHALL NOT perform translation status analysis and metadata fields retain defaults

### Requirement: EN-locale entries are always Translated
Entries whose `Locale` property equals `en` (case-insensitive) SHALL always have `IsTranslated = true` and `TranslationStatus = Translated` when the flag is active.

#### Scenario: EN entry receives Translated status
- **WHEN** translation status analysis runs and an entry has `Locale = "en"`
- **THEN** `IsTranslated` SHALL be `true` and `TranslationStatus` SHALL be `Translated`

### Requirement: DoNotTranslate entries receive DoNotTranslate status
Entries whose `Metadata.DoNotTranslate` is `true` SHALL have `IsTranslated = false` and `TranslationStatus = DoNotTranslate` when the flag is active, regardless of whether a matching key exists in the EN locale.

#### Scenario: Entry marked DoNotTranslate
- **WHEN** translation status analysis runs and an entry has `Metadata.DoNotTranslate = true`
- **THEN** `IsTranslated` SHALL be `false` and `TranslationStatus` SHALL be `DoNotTranslate`

### Requirement: Target locale entries matched against EN keys
For each non-EN locale, the system SHALL compare the entry's `Key` against the set of keys present in EN-locale entries. If a matching EN key exists, the entry SHALL be marked `Translated`. If no matching EN key exists, the entry SHALL be marked `NeedsReview`.

#### Scenario: Target locale entry has matching EN key
- **WHEN** translation status analysis runs and a non-EN entry's `Key` matches an EN entry's `Key`
- **THEN** `IsTranslated` SHALL be `true` and `TranslationStatus` SHALL be `Translated`

#### Scenario: Target locale entry has no matching EN key
- **WHEN** translation status analysis runs and a non-EN entry's `Key` does not match any EN entry's `Key`
- **THEN** `IsTranslated` SHALL be `false` and `TranslationStatus` SHALL be `NeedsReview`

### Requirement: EN entries missing from target locale are Untranslated
For each EN-locale key, the system SHALL check whether a matching key exists in each target locale. If a target locale lacks a matching entry, a synthetic indication of `Untranslated` status is recorded in the summary (not as a new entry).

#### Scenario: Summary counts untranslated EN keys per target locale
- **WHEN** translation status analysis completes
- **THEN** the summary SHALL include, for each target locale, the count of EN keys that have no matching entry in that locale, categorized as `Untranslated`

### Requirement: DataBankOutput includes TranslationSummary
The `DataBankOutput` model SHALL include an optional `TranslationSummary` property. When `--flag-untranslated` is active, this summary SHALL contain overall counts and per-locale counts for each `TranslationStatus` value.

#### Scenario: Summary is populated when flag is active
- **WHEN** `--flag-untranslated` is used and analysis completes
- **THEN** `DataBankOutput.TranslationSummary` SHALL be populated with `Overall` counts (Translated, Untranslated, DoNotTranslate, NeedsReview) and a `ByLocale` list with per-locale breakdowns

#### Scenario: Summary is null when flag is inactive
- **WHEN** `--flag-untranslated` is not used
- **THEN** `DataBankOutput.TranslationSummary` SHALL be `null`

### Requirement: Output JSON includes translation status fields
When translation status analysis is active, the serialized `data-bank.json` SHALL include `isTranslated` and `translationStatus` properties on each entry's `metadata` object, and a `translationSummary` property at the top level.

#### Scenario: JSON output contains new fields
- **WHEN** `--flag-untranslated` is used and `data-bank.json` is written
- **THEN** each entry's `metadata` SHALL contain `isTranslated` (boolean) and `translationStatus` (string), and the root object SHALL contain `translationSummary` with overall and per-locale counts
