## ADDED Requirements

### Requirement: Edit writes back to source file
The system SHALL write an edited translation value directly to the source file identified by the entry's `Source.File` and `Source.Line` properties.

#### Scenario: Edit RC translation value
- **WHEN** a user edits a translation value for an entry with `Source.Format = "rc"`, `Source.File = "FR/PCInstall.rc"`, `Source.Line = 87`
- **THEN** the system SHALL read `FR/PCInstall.rc` with the detected encoding, replace the value at line 87, and write the file back with the same encoding

#### Scenario: Edit FHX translation value
- **WHEN** a user edits a translation value for an entry with `Source.Format = "fhx"`, `Source.File = "EN/AlarmWords.txt", `Source.Line = 12`
- **THEN** the system SHALL read `EN/AlarmWords.txt` with the detected encoding, replace the value at line 12, and write the file back with the same encoding

### Requirement: Preserve file encoding on write-back
The system SHALL detect the file's encoding using `EncodingDetector.Detect()` before reading and SHALL write the file back using the same encoding.

#### Scenario: UTF-16LE file preserves encoding
- **WHEN** a file has BOM `FF FE` (UTF-16LE)
- **THEN** the system SHALL read and write the file using `Encoding.Unicode` (UTF-16LE)

#### Scenario: UTF-8 file preserves encoding
- **WHEN** a file has BOM `EF BB BF` (UTF-8) or no BOM
- **THEN** the system SHALL read and write the file using `Encoding.UTF8`

#### Scenario: ANSI file with code_page directive preserves encoding
- **WHEN** a file contains `#pragma code_page(1252)` and no BOM
- **THEN** the system SHALL read and write the file using the encoding corresponding to code page 1252

### Requirement: Safety check before overwrite
The system SHALL verify that the old value exists at the target line before replacing. If the old value is not found at the expected line, the system SHALL abort the write and report an error.

#### Scenario: Old value found at target line
- **WHEN** the system reads line 87 and finds the expected old value
- **THEN** the system SHALL replace the value and write the file

#### Scenario: Old value not found at target line
- **WHEN** the system reads line 87 and the expected old value is not present
- **THEN** the system SHALL NOT write the file and SHALL report an error indicating the line may have been modified externally

### Requirement: Preserve line endings
The system SHALL detect the line ending style (`\r\n` vs `\n`) of the file and preserve it on write-back.

#### Scenario: CRLF file preserves line endings
- **WHEN** a file uses `\r\n` line endings
- **THEN** the system SHALL write back with `\r\n` line endings

#### Scenario: LF file preserves line endings
- **WHEN** a file uses `\n` line endings
- **THEN** the system SHALL write back with `\n` line endings

### Requirement: Desktop app writes back to source file
The desktop app SHALL allow editing a translation value inline and write the change back to the source file using the same write-back mechanism as the CLI.

#### Scenario: Inline edit in desktop app writes back to source
- **WHEN** a user edits a locale value inline in the desktop app and the entry has source metadata (file, line, format)
- **THEN** the system SHALL invoke `FileWriter.EditEntry()` with the resolved source file path and SHALL notify the user of the write-back result

#### Scenario: Inline edit with no source metadata is memory-only
- **WHEN** a user edits a locale value inline but the entry has no resolvable source file or line
- **THEN** the system SHALL update only the in-memory value and SHALL notify the user that no source file write-back occurred
