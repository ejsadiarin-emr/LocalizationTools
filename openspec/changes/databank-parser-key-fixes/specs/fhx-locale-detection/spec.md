## ADDED Requirements

### Requirement: FHX locale detection produces valid BCP47 codes
The FHX parser SHALL detect locale from file content or require `--locale` override for non-EN files.

#### Scenario: EN directory
- **WHEN** an FHX file is in a directory named `EN`
- **THEN** the detected locale SHALL be `en`

#### Scenario: --locale override
- **WHEN** the `--locale zh-Hans` flag is provided
- **THEN** all FHX entries SHALL have locale `zh-Hans`

#### Scenario: Non-EN directory without override
- **WHEN** an FHX file is in a directory named `Translated`
- **AND** no `--locale` override is provided
- **THEN** the parser SHALL log a warning: `Warning: FHX file in non-EN directory without --locale override. Detected locale: "translated". Use --locale to specify the actual locale.`
- **AND** the detected locale SHALL be `unknown`

#### Scenario: Content-based locale detection (best-effort)
- **WHEN** an FHX file contains language-specific patterns (e.g., Chinese characters, Cyrillic text)
- **AND** no `--locale` override is provided
- **THEN** the parser SHALL attempt to detect the locale from content
- **AND** log a warning if detection is uncertain
