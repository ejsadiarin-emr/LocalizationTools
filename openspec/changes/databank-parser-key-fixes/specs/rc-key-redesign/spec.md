## ADDED Requirements

### Requirement: CAPTION keys use dialog name
The RC parser SHALL generate CAPTION keys using the dialog name from the DIALOGEX header, not the file path or text value.

#### Scenario: CAPTION on same line as DIALOGEX
- **WHEN** an RC file contains `IDD_ABOUTBOX DIALOGEX 0, 0, 295, 55` with `CAPTION "About DeltaV"`
- **THEN** the generated key SHALL be `CAPTION::IDD_ABOUTBOX`
- **AND** the value SHALL be `About DeltaV`

#### Scenario: CAPTION on separate line
- **WHEN** an RC file contains a DIALOGEX block with `CAPTION "Main Dialog"` on a separate line
- **THEN** the generated key SHALL be `CAPTION::{dialogName}`
- **AND** the value SHALL be `Main Dialog`

#### Scenario: EN and Translated CAPTIONs match
- **WHEN** EN file contains `CAPTION "About DeltaV"` in `IDD_ABOUTBOX`
- **AND** Translated file contains `CAPTION "关于 DeltaV"` in `IDD_ABOUTBOX`
- **THEN** both entries SHALL have the same key `CAPTION::IDD_ABOUTBOX`

### Requirement: Dialog control keys use dialog name
The RC parser SHALL generate dialog control keys using the dialog name, not the file path.

#### Scenario: LTEXT control with unique ID
- **WHEN** an RC file contains `LTEXT "Version 5.2",IDC_VERSION,40,14,159,8` in `IDD_ABOUTBOX`
- **THEN** the generated key SHALL be `LTEXT::IDD_ABOUTBOX::IDC_VERSION`

#### Scenario: PUSHBUTTON control
- **WHEN** an RC file contains `PUSHBUTTON "OK",IDOK,238,34,32,14` in `IDD_ABOUTBOX`
- **THEN** the generated key SHALL be `PUSHBUTTON::IDD_ABOUTBOX::IDOK`

#### Scenario: CONTROL element
- **WHEN** an RC file contains `CONTROL "My Checkbox",IDC_CHECK,"Button",BS_AUTOCHECKBOX,10,10,100,14` in `IDD_TEST`
- **THEN** the generated key SHALL be `CONTROL::IDD_TEST::IDC_CHECK`

### Requirement: IDC_STATIC controls use positional index
The RC parser SHALL disambiguate multiple IDC_STATIC controls within the same dialog using a positional index.

#### Scenario: Multiple IDC_STATIC controls
- **WHEN** a dialog contains:
  - `LTEXT "First",IDC_STATIC,10,10,100,14`
  - `LTEXT "Second",IDC_STATIC,10,30,100,14`
  - `LTEXT "Third",IDC_STATIC,10,50,100,14`
- **THEN** the generated keys SHALL be:
  - `LTEXT::IDD_TEST::IDC_STATIC::0`
  - `LTEXT::IDD_TEST::IDC_STATIC::1`
  - `LTEXT::IDD_TEST::IDC_STATIC::2`

#### Scenario: Mixed IDC_STATIC and unique IDs
- **WHEN** a dialog contains:
  - `LTEXT "Static Text",IDC_STATIC,10,10,100,14`
  - `PUSHBUTTON "OK",IDOK,238,34,32,14`
- **THEN** the LTEXT key SHALL be `LTEXT::IDD_TEST::IDC_STATIC::0`
- **AND** the PUSHBUTTON key SHALL be `PUSHBUTTON::IDD_TEST::IDOK`

### Requirement: STRINGTABLE keys remain unchanged
The RC parser SHALL continue to use `{defineName}` as the key for STRINGTABLE entries.

#### Scenario: STRINGTABLE entry with symbolic ID
- **WHEN** an RC file contains `IDS_WELCOME "Welcome"` in a STRINGTABLE block
- **THEN** the generated key SHALL be `IDS_WELCOME`

#### Scenario: STRINGTABLE entry with numeric ID
- **WHEN** an RC file contains `100 "Welcome"` in a STRINGTABLE block
- **THEN** the generated key SHALL be `100`
