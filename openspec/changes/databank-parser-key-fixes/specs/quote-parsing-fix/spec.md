## ADDED Requirements

### Requirement: ExtractQuotedString handles escaped quotes
The RC parser SHALL use a state-machine quote parser that correctly handles escaped quotes.

#### Scenario: Simple quoted string
- **WHEN** the input is `CAPTION "About DeltaV"`
- **THEN** the extracted string SHALL be `About DeltaV`

#### Scenario: String with escaped quotes
- **WHEN** the input is `LTEXT "Say ""hello""",IDC_STATIC,10,10,100,14`
- **THEN** the extracted string SHALL be `Say "hello"`

#### Scenario: String with embedded quotes
- **WHEN** the input is `CAPTION "File ""Open"" Dialog"`
- **THEN** the extracted string SHALL be `File "Open" Dialog`

#### Scenario: Empty string
- **WHEN** the input is `LTEXT "",IDC_STATIC,10,10,100,14`
- **THEN** the extracted string SHALL be empty
- **AND** the entry SHALL be skipped

#### Scenario: No closing quote
- **WHEN** the input is `CAPTION "About DeltaV`
- **THEN** the extracted string SHALL be null
- **AND** no entry SHALL be created
