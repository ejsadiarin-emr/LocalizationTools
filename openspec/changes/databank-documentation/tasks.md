## 1. Research and Preparation

- [x] 1.1 Review existing DataBank source code (Program.cs, parsers, models) to gather accurate CLI information.
- [x] 1.2 Examine sample input files (resx, rc, fhx, ahc) to create realistic examples.
- [x] 1.3 Review DataBank.Desktop project structure (App.xaml, MainWindow, WebView2 setup) for Desktop docs.
- [x] 1.4 Review DataBank.Api project structure for API documentation reference.

## 2. Create Top-Level DatabankTool README

- [x] 2.1 Create `DatabankTool/README.md` with overview of the DatabankTool directory.
- [x] 2.2 Describe each sub-project: DataBank.Cli, DataBank.Desktop, DataBank.Api, DataBank.Import.
- [x] 2.3 Add links to each sub-project's README.
- [x] 2.4 Note that API documentation is provided via Swagger/OpenAPI.
- [x] 2.5 Add Quick How to Run section with make commands, docker-compose, MongoDB Compass instructions.
- [x] 2.6 Add architecture diagram showing data flow.
- [x] 2.7 Add API endpoints reference table.
- [x] 2.8 Add CLI reference with all flags.
- [x] 2.9 Add common workflows section.

## 3. Create CLI README Structure

- [ ] 3.1 Create `DatabankTool/DataBank.Cli/README.md` with skeleton sections.
- [ ] 3.2 Add table of contents for easy navigation.

## 4. Write CLI Overview Section

- [ ] 4.1 Write overview of DataBank CLI purpose and role in localization workflow.
- [ ] 4.2 Describe relationship to other tools (Tool 1 analyzer, Tool 3 sync).

## 5. Write CLI Installation and Build Instructions

- [ ] 5.1 Document prerequisites (.NET SDK version, dependencies).
- [ ] 5.2 Provide build commands (`dotnet build`, `dotnet publish`) with correct paths.
- [ ] 5.3 Explain how to run the tool (dotnet run, executable path).

## 6. Write CLI Usage Section

- [ ] 6.1 Document all flags with descriptions and examples.
- [ ] 6.2 Provide example commands for common scenarios.
- [ ] 6.3 Explain input directory behavior and default values.

## 7. Write Supported Formats Section

- [ ] 7.1 Document .resx format (XML resource files, locale detection via filename).
- [ ] 7.2 Document .rc format (Windows resource files, symbol resolution via resource.h).
- [ ] 7.3 Document .fhx format (AlarmWords.txt files, locale detection from content/override).
- [ ] 7.4 Document .ahc format (Alarm history files, encoding detection).
- [ ] 7.5 Document .json format (JSON translation files).
- [ ] 7.6 Document .grf format (DeltaV GRF template files).

## 8. Write CLI Output Format Section

- [ ] 8.1 Document data-bank.json schema (generated, entries[]).
- [ ] 8.2 Document each entry field (key, value, locale, source, metadata).
- [ ] 8.3 Provide realistic example output snippet.

## 9. Write Common Workflows Section

- [ ] 9.1 Document workflow: Extract strings from a project.
- [ ] 9.2 Document workflow: Generate coverage report.
- [ ] 9.3 Document workflow: Filter by format.
- [ ] 9.4 Document workflow: Override encoding/locale.

## 10. Write CLI Architecture Overview Section

- [ ] 10.1 Describe data flow: input files → parsers → LocalizedStringEntry list → DataBankOutput → JSON.
- [ ] 10.2 List components: Program.cs (entry point), parsers, models, helpers.
- [ ] 10.3 Explain output generation and file writing.

## 11. Write Parser Details Section

- [ ] 11.1 Explain ResxParser: XML parsing, key extraction, locale from filename.
- [ ] 11.2 Explain RcParser: resource.h symbol resolution, string table parsing.
- [ ] 11.3 Explain FhxParser: AlarmWords.txt parsing, locale detection from content.
- [ ] 11.4 Explain AhcParser: binary/text parsing, encoding detection.
- [ ] 11.5 Explain JsonParser: JSON translation file parsing.
- [ ] 11.6 Explain GrfParser: GRF template file parsing.

## 12. Write Coverage Analysis Section

- [ ] 12.1 Explain coverage metrics (overall completion, EN keys, translated keys, missing, orphaned).
- [ ] 12.2 Describe per-locale breakdown.
- [ ] 12.3 Show example coverage report output.

## 13. Write CLI Extension Guide Section

- [ ] 13.1 Provide steps to add a new parser: create class, implement Parse method.
- [ ] 13.2 Explain how to register new parser in Program.cs.
- [ ] 13.3 Document model classes and their roles.

## 14. Create Desktop README

- [ ] 14.1 Create `DatabankTool/DataBank.Desktop/README.md` with skeleton sections.
- [ ] 14.2 Write Desktop overview: WPF application hosting WebView2 browser control.
- [ ] 14.3 Document build prerequisites (.NET SDK, WebView2 Runtime).
- [ ] 14.4 Provide build commands (`dotnet build` from `DatabankTool/DataBank.Desktop/`).
- [ ] 14.5 Document how to run the app (`dotnet run`, built executable).
- [ ] 14.6 Describe architecture: WPF host + WebView2 + IPC pattern.
- [ ] 14.7 Document Local and Remote modes with mode persistence.
- [ ] 14.8 Document development setup and debugging tips.
- [ ] 14.9 Describe project structure and key files.

## 15. Update PROJECT_CONTEXT.md

- [ ] 15.1 Change Tool 2 status from "Not started" to "Complete".
- [ ] 15.2 Update Tool 2 description to match actual implementation (6 parsers, not SARIF-based).
- [ ] 15.3 Note the actual output format (richer than originally spec'd).
- [ ] 15.4 Add note that Desktop (WPF+WebView2), API (ASP.NET Core), and Import tool are now available.

## 16. Review and Finalize

- [ ] 16.1 Review all CLI documentation for accuracy against source code.
- [ ] 16.2 Review Desktop documentation for accuracy against project structure.
- [ ] 16.3 Verify top-level README links are correct.
- [ ] 16.4 Test CLI example commands to ensure they work.
- [ ] 16.5 Check for typos, formatting, and consistency across all READMEs.
- [ ] 16.6 Ensure all spec requirements are addressed.
