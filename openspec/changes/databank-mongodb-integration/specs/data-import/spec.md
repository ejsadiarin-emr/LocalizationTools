## ADDED Requirements

### Requirement: Import CLI tool existence
The system SHALL provide a `DataBank.Import` console application project that reads `data-bank.json` and inserts entries into MongoDB.

#### Scenario: Import tool compiles
- **WHEN** the DataBank.Import project is built
- **THEN** it produces a runnable executable

### Requirement: Read data-bank.json
The import tool SHALL read and deserialize `data-bank.json` from the current working directory or a specified path.

#### Scenario: Default path
- **WHEN** the import tool runs without arguments
- **THEN** it reads `data-bank.json` from the current directory

#### Scenario: Custom path
- **WHEN** the import tool runs with `--input /path/to/file.json`
- **THEN** it reads from the specified path

#### Scenario: File not found
- **WHEN** the import tool runs and the input file does not exist
- **THEN** it displays an error message and exits with non-zero code

### Requirement: Connect to MongoDB
The import tool SHALL read the MongoDB connection string from `appsettings.json` in its project directory, or accept it via `--connection-string` argument.

#### Scenario: Connection from config
- **WHEN** no `--connection-string` argument is provided
- **THEN** the tool reads connection string from appsettings.json

#### Scenario: Connection from argument
- **WHEN** `--connection-string mongodb://host:27017` is provided
- **THEN** the tool uses the specified connection string

### Requirement: Batch insert entries
The import tool SHALL insert entries into MongoDB in batches of 1000 documents to optimize bulk insert performance.

#### Scenario: Import entries
- **WHEN** the import tool reads 5000 entries from data-bank.json
- **THEN** it performs 5 batch inserts of 1000 entries each

### Requirement: Idempotent import
The import tool SHALL use upsert operations so that re-running the import does not create duplicate entries. Existing entries with the same Key SHALL be updated.

#### Scenario: Re-run import
- **WHEN** the import tool runs a second time with the same data-bank.json
- **THEN** no duplicate entries are created and existing entries are updated

### Requirement: Import metadata
The import tool SHALL insert a DataBankMetadata document containing the version, generation timestamp, and total entry count from the JSON file.

#### Scenario: Metadata is imported
- **WHEN** the import tool completes
- **THEN** a DataBankMetadata document exists with the correct version, timestamp, and entry count

### Requirement: Progress reporting
The import tool SHALL display progress during import showing the number of entries processed and total.

#### Scenario: Progress output
- **WHEN** the import tool is processing entries
- **THEN** it displays lines like `Importing entries: 1000/5000`

### Requirement: Import summary
The import tool SHALL display a summary after completion showing total entries imported, duration, and any errors.

#### Scenario: Successful import summary
- **WHEN** the import completes without errors
- **THEN** it displays `Import complete: 5000 entries in 12.3s`
