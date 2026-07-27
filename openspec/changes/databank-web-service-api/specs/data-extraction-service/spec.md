## ADDED Requirements

### Requirement: Trigger extraction endpoint
The system SHALL provide a POST endpoint at `/api/extract` that triggers file parsing and data extraction.

#### Scenario: Trigger extraction
- **WHEN** client sends POST request to `/api/extract`
- **THEN** system triggers extraction process and returns HTTP 202 Accepted with job ID

#### Scenario: Extraction with parameters
- **WHEN** client sends POST request to `/api/extract` with source directory and file patterns
- **THEN** system uses provided parameters for extraction and returns HTTP 202 Accepted

### Requirement: Extraction job status
The system SHALL track extraction job status and provide status endpoint.

#### Scenario: Get job status
- **WHEN** client sends GET request to `/api/extract/{jobId}`
- **THEN** system returns job status (running, completed, failed) with progress information

#### Scenario: Job not found
- **WHEN** client sends GET request to `/api/extract/{jobId}` where job does not exist
- **THEN** system returns HTTP 404 Not Found

### Requirement: Extraction job completion
The system SHALL update data provider when extraction completes.

#### Scenario: Successful extraction
- **WHEN** extraction job completes successfully
- **THEN** system updates in-memory data with new entries and returns completion summary

#### Scenario: Extraction failure
- **WHEN** extraction job fails
- **THEN** system returns error details and preserves existing data unchanged

### Requirement: Parser integration
The system SHALL reuse existing parsers from DataBank.Cli for extraction.

#### Scenario: File format support
- **WHEN** extraction processes files
- **THEN** system correctly parses .resx, .rc, .fhx, and .ahc files using existing parser implementations

#### Scenario: Parser error handling
- **WHEN** parser encounters invalid file format
- **THEN** system logs error, skips invalid file, and continues processing other files