## ADDED Requirements

### Requirement: Export endpoint returns DataBankOutput format
The system SHALL provide a `GET /api/databank/export` endpoint that returns a JSON response matching the CLI's `DataBankOutput` structure, including `version`, `generated`, `entries`, and `translationSummary`.

#### Scenario: Export all entries
- **WHEN** a GET request is made to `/api/databank/export`
- **THEN** a JSON response is returned with `version: 2`, `generated` (ISO 8601 timestamp), `entries` (all entries from MongoDB), and `translationSummary` (computed statistics)

#### Scenario: Export response structure
- **WHEN** the export endpoint is called
- **THEN** the response matches this schema:
  ```json
  {
    "version": 2,
    "generated": "2026-07-25T10:00:00Z",
    "entries": [
      {
        "id": "string",
        "key": "string",
        "value": "string",
        "locale": "string",
        "source": {
          "format": "string",
          "file": "string",
          "path": "string",
          "encoding": "string|null"
        },
        "metadata": {
          "comment": "string|null",
          "rcId": "int|null",
          "rcDefine": "string|null",
          "isBehavioral": false,
          "formatSpecifiers": [],
          "doNotTranslate": false,
          "isTranslated": false,
          "translationStatus": "Translated|Untranslated|DoNotTranslate|NeedsReview"
        }
      }
    ],
    "translationSummary": {
      "totalKeys": 0,
      "translatedKeys": 0,
      "untranslatedKeys": 0,
      "doNotTranslateKeys": 0,
      "needsReviewKeys": 0,
      "completionPercentage": 0.0
    }
  }
  ```

### Requirement: Translation summary is computed, not stored
The system SHALL compute `translationSummary` dynamically from MongoDB data using aggregation, not from a stored value.

#### Scenario: Summary reflects current state
- **WHEN** the export endpoint is called after entries have been added or modified
- **THEN** the `translationSummary` reflects the current state of all entries in MongoDB

#### Scenario: Summary counts by status
- **WHEN** the export endpoint is called
- **THEN** `totalKeys` equals total entry count, `translatedKeys` equals entries with `TranslationStatus == "Translated"`, `untranslatedKeys` equals entries with `TranslationStatus == "Untranslated"`, `doNotTranslateKeys` equals entries with `TranslationStatus == "DoNotTranslate"`, `needsReviewKeys` equals entries with `TranslationStatus == "NeedsReview"`, and `completionPercentage` is `(translatedKeys / totalKeys) * 100` rounded to 1 decimal

### Requirement: Metadata endpoint returns version and generated
The system SHALL provide a `GET /api/metadata` endpoint that returns the `DataBankMetadataDocument` from MongoDB, including `version`, `generated`, and `entryCount`.

#### Scenario: Get metadata
- **WHEN** a GET request is made to `/api/metadata`
- **THEN** the metadata document is returned with `version`, `generated` (ISO 8601 timestamp), and `entryCount`

#### Scenario: Metadata not found
- **WHEN** a GET request is made to `/api/metadata` and no metadata exists
- **THEN** a 404 Not Found response is returned
