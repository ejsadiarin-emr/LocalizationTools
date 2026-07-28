## ADDED Requirements

### Requirement: Detect file format by extension
The system SHALL detect file format using file extension as the primary signal.
- `.resx` → RESX format
- `.rc` → RC format
- `.fhx` → FHX format
- `.ahc` → AHC format
- `.json` → JSON format
- `.grf` → GRF format

#### Scenario: FHX file with .fhx extension
- **WHEN** a file has the `.fhx` extension
- **THEN** the system SHALL classify it as FHX format

#### Scenario: RESX file by extension
- **WHEN** a file has the `.resx` extension
- **THEN** the system SHALL classify it as RESX format

#### Scenario: RC file by extension
- **WHEN** a file has the `.rc` extension
- **THEN** the system SHALL classify it as RC format

#### Scenario: AHC file by extension
- **WHEN** a file has the `.ahc` extension
- **THEN** the system SHALL classify it as AHC format

#### Scenario: GRF file by extension
- **WHEN** a file has the `.grf` extension
- **THEN** the system SHALL classify it as GRF format

### Requirement: Detect FHX format by directory name
The system SHALL detect FHX format when a file resides in a directory whose name is `Fhx` (case-insensitive), regardless of the file extension. This supports both `.fhx` and `.txt` files in FHX directories.

#### Scenario: TXT file in Fhx directory
- **WHEN** a file has the `.txt` extension AND resides in a directory named `Fhx`
- **THEN** the system SHALL classify it as FHX format

#### Scenario: FHX file in Fhx directory
- **WHEN** a file has the `.fhx` extension AND resides in a directory named `Fhx`
- **THEN** the system SHALL classify it as FHX format

#### Scenario: Case-insensitive directory match
- **WHEN** a file resides in a directory named `fhx` (lowercase)
- **THEN** the system SHALL classify it as FHX format

### Requirement: Content-based fallback for ambiguous files
The system SHALL use content-based detection as a fallback when a file cannot be classified by extension or directory name. The system SHALL read the first line of the file and check for the FHX signature pattern: `@Key@` followed by a tab character.

#### Scenario: TXT file with FHX content
- **WHEN** a file has the `.txt` extension AND does NOT reside in an `Fhx` directory AND the first line starts with `@Key@` followed by a tab
- **THEN** the system SHALL classify it as FHX format

#### Scenario: TXT file without FHX content
- **WHEN** a file has the `.txt` extension AND does NOT reside in an `Fhx` directory AND the first line does NOT start with `@Key@`
- **THEN** the system SHALL NOT classify it as FHX format

### Requirement: Discovery returns all matching files
The system SHALL discover all files matching any supported format within a directory tree, using `SearchOption.AllDirectories`. Each file SHALL be associated with its detected format.

#### Scenario: Mixed formats in directory tree
- **WHEN** a directory contains `.resx`, `.rc`, `.txt` (in Fhx dir), and `.ahc` files
- **THEN** the system SHALL return all files with their correct format classifications

#### Scenario: Empty directory
- **WHEN** a directory contains no supported files
- **THEN** the system SHALL return an empty collection

### Requirement: Coverage analyzer recognizes all supported formats
The system SHALL recognize `.fhx` and `.grf` as supported formats in coverage analysis, in addition to the existing `.rc`, `.resx`, `.txt`, `.ahc`, and `.json` formats.

#### Scenario: FHX file in coverage analysis
- **WHEN** the coverage analyzer encounters a file with the `.fhx` extension
- **THEN** the system SHALL treat it as a supported format

#### Scenario: GRF file in coverage analysis
- **WHEN** the coverage analyzer encounters a file with the `.grf` extension
- **THEN** the system SHALL treat it as a supported format

### Requirement: Existing parsers remain unchanged
The system SHALL continue to invoke existing parsers (ResxParser, RcParser, FhxParser, AhcParser, JsonParser) with the same arguments and behavior. File detection changes SHALL NOT alter parser input contracts.

#### Scenario: FHX parser receives same input
- **WHEN** an FHX file is detected and passed to FhxParser.Parse
- **THEN** FhxParser SHALL receive the same file path, locale, encoding, and root directory arguments as before

#### Scenario: JSON parser receives same input
- **WHEN** a JSON file is detected and passed to JsonParser.Parse
- **THEN** JsonParser SHALL receive the same file path and root directory arguments as before
