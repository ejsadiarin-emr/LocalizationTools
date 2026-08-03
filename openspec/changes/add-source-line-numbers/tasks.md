## 1. Data Model

- [x] 1.1 Add `int? Line` property to `SourceInfo` in `DataBank.Cli\Models\SourceInfo.cs`
- [x] 1.2 Add `int? Line` property to `SourceInfoDocument` in `DataBank.Api\Models\DataBankEntryDocument.cs`
- [x] 1.3 Add `int? Line` property to `SourceInfoInput` in `DataBank.Import\Program.cs`

## 2. Parser Updates

- [x] 2.1 Update FhxParser to track line numbers (add counter in foreach loop, set `Source.Line`)
- [x] 2.2 Refactor RcParser `NormalizeContent` to return `List<(string line, int startLineNumber)>` and track line numbers in the main parse loop
- [x] 2.3 Update ResxParser to use `IXmlLineInfo` on `XElement` to populate `Source.Line`
- [x] 2.4 Update AhcParser to use `IXmlLineInfo` on `LanguageValue` elements to populate `Source.Line`
- [x] 2.5 Update JsonParser to post-process raw file text and find line numbers for each key
- [x] 2.6 Verify GrfParser leaves `Source.Line` as null (no changes needed, just confirm)

## 3. Data Pipeline

- [x] 3.1 Verify `EntryGrouper` preserves `Source.Line` when grouping (should work automatically since it copies the `SourceInfo` object)
- [x] 3.2 Verify `DataBankOutput` serialization includes `line` field in JSON output (System.Text.TextJson should serialize it by default)
- [x] 3.3 Update API `ExportEndpoints` to include `line` in the export response source objects

## 4. Desktop "Go to Source"

- [x] 4.1 Add `openSourceFile` case to `CoreWebView2_WebMessageReceived` in `MainWindow.xaml.cs`
- [x] 4.2 Implement file open logic using `Process.Start` with `UseShellExecute = true`
- [x] 4.3 Update `app.js` detail panel to render line numbers in the Sources section
- [x] 4.4 Add "Open Source File" button in `app.js` detail panel for each source with a file path
- [x] 4.5 Add CSS styling for the "Open Source File" button and line number display

## 5. Testing

- [x] 5.1 Update existing parser tests to verify `Source.Line` is populated correctly
- [x] 5.2 Add test cases for line numbers in FhxParser, RcParser, ResxParser, AhcParser, JsonParser
- [x] 5.3 Test API export endpoint returns `line` field in source objects
- [x] 5.4 Test Desktop detail panel displays line numbers and "Open Source File" button

## 6. Re-extraction

- [x] 6.1 Re-parse all l10n-files with updated parsers to generate data-bank.json with line numbers
- [x] 6.2 Verify data-bank.json output contains line numbers in source objects
