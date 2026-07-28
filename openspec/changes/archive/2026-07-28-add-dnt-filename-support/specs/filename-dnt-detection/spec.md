## ADDED Requirements

### Requirement: Detect DNT marker in filename

The system SHALL detect files with "DNT" in the filename (case-insensitive) and mark all entries from those files as DoNotTranslate.

#### Scenario: File with DNT in filename
- **WHEN** a file is parsed and its filename contains "DNT" (case-insensitive)
- **THEN** all entries from that file SHALL have `Metadata.DoNotTranslate = true`

#### Scenario: File without DNT in filename
- **WHEN** a file is parsed and its filename does not contain "DNT"
- **THEN** entries SHALL use existing detection logic (FHX context-based or default false)

#### Scenario: DNT detection is case-insensitive
- **WHEN** a file is named `file-dnt.rc`, `file-DNT.rc`, or `file_Dnt.rc`
- **THEN** all entries SHALL have `Metadata.DoNotTranslate = true`

### Requirement: Preserve existing FHX DNT detection

The system SHALL continue to detect "do NOT translate" in FHX context fields as DoNotTranslate, in addition to filename-based detection.

#### Scenario: FHX entry with do NOT translate context
- **WHEN** an FHX file is parsed and a line has context containing "do NOT translate"
- **THEN** that entry SHALL have `Metadata.DoNotTranslate = true` regardless of filename

#### Scenario: FHX file with DNT filename
- **WHEN** an FHX file with "DNT" in the filename is parsed
- **THEN** all entries SHALL have `Metadata.DoNotTranslate = true` (file-level override)