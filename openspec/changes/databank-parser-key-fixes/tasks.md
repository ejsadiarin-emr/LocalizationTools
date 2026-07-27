## 1. RC Parser Key Redesign

- [x] 1.1 Track dialog context in RcParser state machine — add `currentDialogName` variable that captures the dialog name from DIALOGEX header line
- [x] 1.2 Update CreateDialogEntry to use dialog name instead of value in key: `CAPTION::{dialogName}`
- [x] 1.3 Update ParseDialogControl to use dialog name instead of relativePath in key: `{controlType}::{dialogName}::{defineName}`
- [x] 1.4 Implement IDC_STATIC positional index — track control count per dialog, append index for IDC_STATIC controls
- [x] 1.5 Update CONTROL pattern key generation to use dialog name and positional index
- [x] 1.6 Verify STRINGTABLE keys remain unchanged (already path-independent)

## 2. FHX Locale Detection Fix

- [x] 2.1 Update FhxParser.DetectLocale to return "unknown" for non-EN directories without --locale override
- [x] 2.2 Add warning message when locale detection falls back to "unknown"
- [x] 2.3 Add best-effort content-based locale detection (check for CJK characters, Cyrillic ranges)
- [x] 2.4 Update FhxParserTests to expect "unknown" locale for Translated directory

## 3. Encoding Detection Fix

- [x] 3.1 Add method to EncodingDetector to parse `#pragma code_page(N)` from file content
- [x] 3.2 Map codepage numbers to .NET encoding names (1252→windows-1252, 936→gb2312, etc.)
- [x] 3.3 Update EncodingDetector.Detect to check for #pragma code_page before BOM detection
- [x] 3.4 Add encoding validation warning when replacement characters are detected
- [x] 3.5 Update EncodingDetectorTests with #pragma code_page test cases

## 4. Quote Parsing Fix

- [x] 4.1 Implement state-machine quote parser in RcParser.ExtractQuotedString
- [x] 4.2 Handle `""` escape sequences correctly (unescaped to `"`)
- [x] 4.3 Handle strings with embedded quotes
- [x] 4.4 Return null for unclosed quotes
- [x] 4.5 Update RcParserTests with escaped quote test cases

## 5. Test Updates

- [x] 5.1 Update RcParserTests to expect new key format (dialog name instead of path)
- [x] 5.2 Add test for EN/Translated CAPTION key matching
- [x] 5.3 Add test for IDC_STATIC positional index disambiguation
- [x] 5.4 Add test for FHX locale "unknown" fallback
- [x] 5.5 Add test for encoding detection with #pragma code_page
- [x] 5.6 Add test for escaped quote parsing
- [x] 5.7 Run all tests and verify they pass

## 6. Documentation

- [x] 6.1 Update src/README.md to note GRF (OLE) and iFix DLL (.NET assembly) as future work
- [x] 6.2 Document the new RC key format in README or code comments
- [x] 6.3 Document FHX locale detection behavior and --locale requirement
