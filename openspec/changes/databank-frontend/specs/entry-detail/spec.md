## ADDED Requirements

### Requirement: Entry detail view shows full metadata
The system SHALL display detailed information for a selected localization entry in the web frontend.

#### Scenario: Entry detail opens
- **WHEN** user clicks on a table row
- **THEN** web frontend displays entry detail view with all metadata fields

#### Scenario: Detail view shows all fields
- **WHEN** entry detail view loads
- **THEN** system displays: Key, Source (EN), Translation, Locale, Format, Status, and any additional metadata from the entry object

### Requirement: Entry detail view includes navigation
The system SHALL allow users to navigate between entries from the detail view.

#### Scenario: Next entry navigation
- **WHEN** user clicks "Next" button in detail view
- **THEN** system displays next entry in the filtered/sorted list

#### Scenario: Previous entry navigation
- **WHEN** user clicks "Previous" button in detail view
- **THEN** system displays previous entry in the filtered/sorted list

#### Scenario: Back to table navigation
- **WHEN** user clicks "Back to Table" button
- **THEN** system returns to table view preserving filter/search/sort state

### Requirement: Entry detail view shows status with color coding
The system SHALL display entry status with appropriate color coding.

#### Scenario: Status indicator displays
- **WHEN** entry detail view loads
- **THEN** system displays status with color: green=translated, red=untranslated, yellow=needs review, gray=do not translate

### Requirement: Entry detail view shows similar strings
The system SHALL display similar strings detected via Fuse.js fuzzy matching.

#### Scenario: Similar strings section displays
- **WHEN** entry detail view loads
- **THEN** web frontend runs Fuse.js fuzzy matching against all loaded entries
- **AND** displays section showing similar source strings with similarity percentage

#### Scenario: Similar strings are clickable
- **WHEN** user clicks on a similar string entry in the detail view
- **THEN** system navigates to that entry's detail view

#### Scenario: Similar strings excluded self
- **WHEN** similar strings are detected
- **THEN** the current entry is excluded from results
