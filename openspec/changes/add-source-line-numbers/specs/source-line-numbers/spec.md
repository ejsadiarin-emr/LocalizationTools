## ADDED Requirements

### Requirement: SourceInfo includes line number
The `SourceInfo` model SHALL include a nullable integer `Line` property representing the line number in the source file where the key's value first appears.

#### Scenario: Line number populated for FHX entries
- **WHEN** the FHX parser processes a line containing a key
- **THEN** the resulting `SourceInfo.Line` SHALL be set to the 1-based line number of that line in the source file

#### Scenario: Line number populated for RC entries
- **WHEN** the RC parser processes a string table entry or dialog control
- **THEN** the resulting `SourceInfo.Line` SHALL be set to the physical line number where the entry starts in the source file

#### Scenario: Line number populated for RESX entries
- **WHEN** the RESX parser processes a `<data>` element
- **THEN** the resulting `SourceInfo.Line` SHALL be set to the XML line number of that element, or null if line info is unavailable

#### Scenario: Line number populated for AHC entries
- **WHEN** the AHC parser processes a `<LanguageValue>` element
- **THEN** the resulting `SourceInfo.Line` SHALL be set to the XML line number of that element, or null if line info is unavailable

#### Scenario: Line number populated for JSON entries
- **WHEN** the JSON parser processes a property
- **THEN** the resulting `SourceInfo.Line` SHALL be set to the line number where that property key appears in the raw file text, or null if not found

#### Scenario: No line number for GRF entries
- **WHEN** the GRF parser creates an entry
- **THEN** the resulting `SourceInfo.Line` SHALL be null

### Requirement: Line number preserved through grouping
The `EntryGrouper` SHALL preserve the `SourceInfo.Line` value when grouping raw entries into `LocalizedStringEntry` objects.

#### Scenario: Line number carried from raw to grouped entry
- **WHEN** a `RawLocalizedEntry` with `Source.Line = 42` is grouped into a `LocalizedStringEntry`
- **THEN** the grouped entry's `Sources[locale].Line` SHALL be 42

### Requirement: Line number in data-bank.json output
The `data-bank.json` output SHALL include the `line` field in each source object.

#### Scenario: Line number serialized in output
- **WHEN** the CLI generates `data-bank.json`
- **THEN** each source object SHALL contain `"line": <number>` or `"line": null`

### Requirement: Line number in API responses
The API export and entries endpoints SHALL include the `line` field in source objects.

#### Scenario: Export endpoint includes line
- **WHEN** a client calls `GET /api/databank/export`
- **THEN** each entry's source objects SHALL include a `line` property

#### Scenario: Entries endpoint includes line
- **WHEN** a client calls `GET /api/entries`
- **THEN** each entry's source objects SHALL include a `line` property

### Requirement: Line number in MongoDB documents
The `SourceInfoDocument` model SHALL include a nullable integer `Line` property persisted in MongoDB.

#### Scenario: Line stored in MongoDB
- **WHEN** a `DataBankEntryDocument` with `Sources["en"].Line = 42` is saved to MongoDB
- **THEN** the document SHALL store `"Line": 42` in the Sources.en subdocument

#### Scenario: Missing line deserializes as null
- **WHEN** a MongoDB document exists without a `Line` field in a source subdocument
- **THEN** deserialization SHALL produce `Line = null`

### Requirement: Line number in import tool
The `DataBank.Import` tool SHALL read and write the `Line` field when importing `data-bank.json`.

#### Scenario: Import preserves line numbers
- **WHEN** a `data-bank.json` with `"line": 42` in a source is imported
- **THEN** the MongoDB document SHALL contain `"Line": 42`
