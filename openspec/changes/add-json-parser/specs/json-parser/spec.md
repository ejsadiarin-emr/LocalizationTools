## ADDED Requirements

### Requirement: Parse flat JSON translation files
The system SHALL parse flat `{ "key": "value" }` JSON files into `LocalizedStringEntry` objects. Each top-level key becomes an entry with the key as `Key` and the string value as `Value`.

#### Scenario: Parse a valid flat JSON file
- **WHEN** a file containing `{ "Greeting": "Hello", "Farewell": "Goodbye" }` is parsed
- **THEN** two `LocalizedStringEntry` objects are produced with keys `"Greeting"` and `"Farewell"` and their respective values

#### Scenario: Skip non-string values
- **WHEN** a JSON file contains `{ "Count": 42, "Enabled": true, "Label": "OK" }`
- **THEN** only the `"Label"` entry is produced; `"Count"` and `"Enabled"` are skipped

#### Scenario: Handle malformed JSON
- **WHEN** a JSON file contains invalid JSON syntax (e.g., trailing comma, missing brace)
- **THEN** the parser writes a warning to stderr and returns an empty list for that file

### Requirement: Detect locale from filename
The system SHALL detect the locale from the filename pattern `translate.<locale>.json`. The `<locale>` segment is used as the BCP47 locale code.

#### Scenario: Locale extracted from filename
- **WHEN** the file `translate.zh.json` is parsed
- **THEN** all entries produced have `Locale` set to `"zh"`

#### Scenario: Base English file
- **WHEN** the file `translate.en.json` is parsed
- **THEN** all entries produced have `Locale` set to `"en"`

#### Scenario: Compound locale in filename
- **WHEN** the file `translate.zh-Hans.json` is parsed
- **THEN** all entries produced have `Locale` set to `"zh-Hans"`

### Requirement: Produce entries consistent with other parsers
The system SHALL produce `LocalizedStringEntry` objects with the same structure as other parsers.

#### Scenario: Entry fields populated correctly
- **WHEN** a JSON entry is parsed from `translate.fr.json` in relative path `l10n-files/translate.fr.json`
- **THEN** the entry has `Id = "json::l10n-files/translate.fr.json::<key>"`, `Format = "json"`, `File = "l10n-files/translate.fr.json"`, and `Locale = "fr"`

### Requirement: Integrate into CLI discovery loop
The system SHALL discover and parse `translate.*.json` files when the `--format` flag is unset or set to `"json"`.

#### Scenario: Default format discovers JSON files
- **WHEN** `dv-extract` is run without `--format` on a directory containing `translate.en.json`
- **THEN** JSON entries are included in the output

#### Scenario: Explicit JSON format filter
- **WHEN** `dv-extract --format json` is run
- **THEN** only JSON files are parsed

### Requirement: Coverage analyzer recognizes JSON format
The system SHALL treat `.json` files as a supported format in `CoverageAnalyzer`.

#### Scenario: JSON files included in coverage analysis
- **WHEN** coverage analysis is run on a directory containing EN/Translated JSON file pairs
- **THEN** those files are included in the coverage report
