## ADDED Requirements

### Requirement: Entry structure with multi-locale values
The system SHALL store each unique key as a single entry containing an array of locale-value pairs. Each entry SHALL have a `values` array where each element contains a `locale` (string) and `value` (string).

#### Scenario: Single entry per key
- **WHEN** a key "@CRITICAL@" exists with translations in English and Chinese
- **THEN** the data bank contains one entry with `key: "@CRITICAL@"` and `values: [{locale: "en", value: "CRITICAL"}, {locale: "zh-CN", value: "危急"}]`

#### Scenario: Missing locale value
- **WHEN** a key has English and Chinese translations but no Russian translation
- **THEN** the entry's `values` array contains `{locale: "ru", value: ""}` with an empty string

#### Scenario: Empty values array
- **WHEN** a key exists but has no translations in any locale
- **THEN** the entry's `values` array is empty or contains only empty-string values

### Requirement: Locale-keyed sources
The system SHALL store source information per locale in a `sources` dictionary keyed by locale code. Each source entry SHALL contain `format`, `file`, and `path` fields.

#### Scenario: Multiple source files
- **WHEN** a key's English value comes from `FHX\EN\AlarmWords.txt` and Chinese from `FHX\Translated\Chinese\AlarmWords.txt`
- **THEN** `sources` contains `{ "en": { "format": "fhx", "file": "...", "path": "..." }, "zh-CN": { "format": "fhx", "file": "...", "path": "..." } }`

#### Scenario: Single source file
- **WHEN** a key exists in only one locale file
- **THEN** `sources` contains only that locale's entry

### Requirement: Key-based unique ID
The system SHALL use the translation key as the unique identifier for each entry. The `id` field SHALL equal the `key` field value.

#### Scenario: Unique key identification
- **WHEN** two keys have the same name in different source files
- **THEN** the system recognizes them as the same entry (keys are globally unique across formats)

### Requirement: Simplified metadata
The system SHALL store per-key metadata with only the following fields: `comment` (string|null), `formatSpecifiers` (string[]), `doNotTranslate` (boolean), `isTranslated` (boolean). The system SHALL NOT store `rcId`, `rcDefine`, `isBehavioral`, or `translationStatus` as separate fields.

#### Scenario: Deriving translation status
- **WHEN** an entry has `doNotTranslate: false` and `isTranslated: true`
- **THEN** the effective translation status is "Translated"

#### Scenario: Do-not-translate entry
- **WHEN** an entry has `doNotTranslate: true`
- **THEN** the effective translation status is "Do Not Translate" regardless of `isTranslated`

#### Scenario: Untranslated entry
- **WHEN** an entry has `doNotTranslate: false` and `isTranslated: false`
- **THEN** the effective translation status is "Untranslated"

### Requirement: Removed fields
The system SHALL NOT include `source.encoding`, `metadata.rcId`, `metadata.rcDefine`, `metadata.isBehavioral`, or `metadata.translationStatus` in the data bank schema.

#### Scenario: Schema compliance
- **WHEN** a data bank entry is serialized to JSON
- **THEN** the output does not contain any of the removed fields

### Requirement: Schema version
The system SHALL identify the data bank format version as 3 in the `version` field of the root JSON object.

#### Scenario: Version标识
- **WHEN** a data bank JSON file is generated
- **THEN** the root object contains `"version": 3`
