## ADDED Requirements

### Requirement: RC format replacer replaces quoted string value
The RC replacer SHALL replace the value within a quoted string on a RC resource line, preserving the surrounding syntax (ID, control type, coordinates).

#### Scenario: Replace STRINGTABLE value
- **WHEN** the line is `IDS_WELCOME "Welcome"` and the new value is `"Bonjour"`
- **THEN** the replacer SHALL produce `IDS_WELCOME "Bonjour"`

#### Scenario: Replace value with L prefix
- **WHEN** the line is `L"Welcome"` and the new value is `"Bonjour"`
- **THEN** the replacer SHALL produce `L"Bonjour"` (preserving the L prefix)

#### Scenario: Replace value containing escaped quotes
- **WHEN** the old value contains `""` (doubled quotes)
- **THEN** the replacer SHALL unescape the old value for matching and re-escape the new value for output

#### Scenario: Replace DIALOG CAPTION value
- **WHEN** the line is `CAPTION "Welcome"` and the new value is `"Bienvenue"`
- **THEN** the replacer SHALL produce `CAPTION "Bienvenue"`

#### Scenario: Replace DIALOG CONTROL text value
- **WHEN** the line is `LTEXT "Welcome",IDC_STATIC,1,2,3,4` and the new value is `"Bienvenue"`
- **THEN** the replacer SHALL produce `LTEXT "Bienvenue",IDC_STATIC,1,2,3,4`

### Requirement: FHX format replacer replaces tab-delimited value
The FHX replacer SHALL replace the value portion of a tab-delimited FHX line (everything after the second tab).

#### Scenario: Replace FHX value
- **WHEN** the line is `@Key@\t"context"\tOld Value` and the new value is `"New Value"`
- **THEN** the replacer SHALL produce `@Key@\t"context"\tNew Value`

### Requirement: RESX format replacer replaces XML value element
The RESX replacer SHALL replace the text content of a `<value>` XML element.

#### Scenario: Replace RESX value
- **WHEN** the line is `<value>Old Value</value>` and the new value is `"New Value"`
- **THEN** the replacer SHALL produce `<value>New Value</value>`

#### Scenario: Escape XML entities in new value
- **WHEN** the new value contains `&`, `<`, or `>`
- **THEN** the replacer SHALL escape these as `&amp;`, `&lt;`, `&gt;`

### Requirement: AHC format replacer replaces LanguageValue content
The AHC replacer SHALL replace the text content of a `<LanguageValue>` XML element.

#### Scenario: Replace AHC value
- **WHEN** the line is `<LanguageValue lang="fr">Old Value</LanguageValue>` and the new value is `"New Value"`
- **THEN** the replacer SHALL produce `<LanguageValue lang="fr">New Value</LanguageValue>`

### Requirement: JSON format replacer replaces string value
The JSON replacer SHALL replace the string value in a `"key": "value"` JSON line.

#### Scenario: Replace JSON value
- **WHEN** the line is `"key": "Old Value"` and the new value is `"New Value"`
- **THEN** the replacer SHALL produce `"key": "New Value"`

#### Scenario: Escape JSON special characters
- **WHEN** the new value contains `"`, `\`, or newline characters
- **THEN** the replacer SHALL escape these using JSON escape sequences (`\"`, `\\`, `\n`)

### Requirement: Replacer aborts if old value not found
Each format replacer SHALL verify that the old value exists in the line before replacing. If the old value is not found, the replacer SHALL return the original line unchanged.

#### Scenario: Old value mismatch
- **WHEN** the line is `IDS_KEY "Actual"` but the expected old value is `"Expected"`
- **THEN** the replacer SHALL return the original line unchanged and signal that no replacement was made
