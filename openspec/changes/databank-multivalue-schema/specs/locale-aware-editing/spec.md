## ADDED Requirements

### Requirement: Edit individual locale value
The system SHALL allow editing the value for a specific locale within a grouped entry without affecting other locale values. The API SHALL accept a locale-specific update request.

#### Scenario: Update single locale value
- **WHEN** a user updates the Chinese value for key "@CRITICAL@" from "危急" to "紧急"
- **THEN** only the zh-CN entry in the `values` array is updated; the English value remains unchanged

#### Scenario: Add new locale value
- **WHEN** a user adds a Russian translation for a key that previously had no Russian value
- **THEN** a new `{locale: "ru", value: "..."}` element is appended to the `values` array

### Requirement: API endpoint for locale value update
The API SHALL provide an endpoint to update a specific locale value within an entry. The endpoint SHALL accept the key, locale, and new value.

#### Scenario: PUT /api/entries/{key}/locales/{locale}
- **WHEN** the client sends `PUT /api/entries/@CRITICAL@/locales/zh-CN` with `{"value": "紧急"}`
- **THEN** the API updates only the zh-CN value in the entry and returns the updated entry

#### Scenario: Locale not found
- **WHEN** the client sends a PUT request for a locale that doesn't exist in the entry
- **THEN** the API adds the new locale value to the `values` array

### Requirement: Bulk locale value update
The API SHALL support updating multiple locale values for a single key in one request.

#### Scenario: Batch update
- **WHEN** the client sends a PATCH request with `{"values": [{"locale": "en", "value": "CRITICAL"}, {"locale": "zh-CN", "value": "紧急"}]}`
- **THEN** all specified locale values are updated atomically

### Requirement: Metadata editing
The system SHALL allow updating per-key metadata (comment, doNotTranslate, isTranslated, formatSpecifiers) via the API.

#### Scenario: Toggle doNotTranslate
- **WHEN** a user sets `doNotTranslate: true` for a key
- **THEN** the key's metadata is updated and the effective status becomes "Do Not Translate"

#### Scenario: Update comment
- **WHEN** a user adds a comment to a key's metadata
- **THEN** the `comment` field is updated in the entry

### Requirement: Entry creation with multi-locale values
The API SHALL support creating a new entry with multiple locale values in a single request.

#### Scenario: Create grouped entry
- **WHEN** the client sends `POST /api/entries` with a key and multiple locale values
- **THEN** a new entry is created with all locale values in the `values` array

### Requirement: Frontend inline editing
The frontend SHALL allow inline editing of locale values in the grouped table view. Each locale cell SHALL be editable independently.

#### Scenario: Click to edit locale value
- **WHEN** the user clicks on a locale value cell in the table
- **THEN** the cell becomes an editable input field with the current value

#### Scenario: Save edited value
- **WHEN** the user finishes editing and presses Enter or clicks away
- **THEN** the new value is sent to the API and the cell returns to display mode with the updated value

#### Scenario: Cancel edit
- **WHEN** the user presses Escape while editing
- **THEN** the edit is cancelled and the cell reverts to the original value
