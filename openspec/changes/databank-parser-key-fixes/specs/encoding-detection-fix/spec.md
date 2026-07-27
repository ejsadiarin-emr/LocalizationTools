## ADDED Requirements

### Requirement: Encoding detection parses #pragma code_page
The encoding detector SHALL parse `#pragma code_page(N)` directives from RC files to detect the correct encoding.

#### Scenario: RC file with #pragma code_page(1252)
- **WHEN** an RC file contains `#pragma code_page(1252)`
- **THEN** the detected encoding SHALL be `windows-1252`

#### Scenario: RC file with #pragma code_page(936)
- **WHEN** an RC file contains `#pragma code_page(936)`
- **THEN** the detected encoding SHALL be `gb2312`

#### Scenario: RC file without #pragma code_page
- **WHEN** an RC file does not contain `#pragma code_page`
- **THEN** the detector SHALL fall back to BOM detection
- **AND** if no BOM, default to UTF-8

#### Scenario: Encoding validation warning
- **WHEN** the detected encoding produces replacement characters (U+FFFD)
- **THEN** the parser SHALL log a warning: `Warning: Encoding mismatch detected in {filePath}. Consider using --encoding override.`
