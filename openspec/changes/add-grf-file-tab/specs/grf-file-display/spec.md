## ADDED Requirements

### Requirement: Tab navigation between DataBank and GRF views

The system SHALL provide tabbed navigation to switch between the DataBank JSON view and the GRF Files view.

#### Scenario: User clicks GRF Files tab

- **WHEN** user clicks the "GRF Files" tab
- **THEN** the GRF file list view is displayed and the DataBank view is hidden

#### Scenario: User clicks DataBank tab

- **WHEN** user clicks the "DataBank" tab
- **THEN** the DataBank JSON view is displayed and the GRF view is hidden

### Requirement: Display GRF filenames from project directory

The system SHALL scan the `l10n-files/GRF/` directory and display all GRF filenames found.

#### Scenario: GRF files exist in EN folder

- **WHEN** GRF files exist in `l10n-files/GRF/EN/`
- **THEN** the system displays each filename with "EN" folder indicator

#### Scenario: GRF files exist in Translated folder

- **WHEN** GRF files exist in `l10n-files/GRF/Translated/`
- **THEN** the system displays each filename with "Translated" folder indicator

#### Scenario: No GRF files found

- **WHEN** no GRF files exist in `l10n-files/GRF/`
- **THEN** the system displays "No GRF files found" message

### Requirement: Backend provides GRF file list via WebMessage

The WPF backend SHALL scan the GRF directory and send the file list to the frontend via WebMessage.

#### Scenario: Frontend requests GRF file list

- **WHEN** the frontend sends a `loadGrfFiles` action message
- **THEN** the backend scans `l10n-files/GRF/` and returns a JSON array of objects with `fileName` and `folder` properties
