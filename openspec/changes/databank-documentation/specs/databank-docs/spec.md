## ADDED Requirements

### Requirement: DatabankTool overview documentation
The documentation SHALL provide a top-level overview at `DatabankTool/README.md` describing the DatabankTool directory, its sub-projects, and how they relate to each other.

#### Scenario: Overview readability
- **WHEN** a developer reads the DatabankTool overview
- **THEN** they understand that the directory contains three sub-projects: DataBank.Cli (CLI tool), DataBank.Desktop (WPF+WebView2 frontend), and DataBank.Api (ASP.NET Core Web API).

#### Scenario: Navigation
- **WHEN** a developer reads the top-level README
- **THEN** they find links to each sub-project's README or documentation.

### Requirement: DataBank.Cli documentation overview
The CLI documentation SHALL provide a clear overview of what DataBank does, its purpose in the localization workflow, and its relationship to other tools.

#### Scenario: Overview readability
- **WHEN** a developer reads the CLI overview section
- **THEN** they understand that DataBank is a CLI tool that extracts localization strings from various file formats and outputs a structured JSON data bank for translation context.

### Requirement: Installation and build instructions
The CLI documentation SHALL include step-by-step instructions for building and installing the DataBank CLI tool.

#### Scenario: Build from source
- **WHEN** a developer follows the build instructions
- **THEN** they can successfully build the tool using `dotnet build` or similar commands from the `DatabankTool/DataBank.Cli/` directory.

#### Scenario: Prerequisites
- **WHEN** a developer reads the prerequisites
- **THEN** they know required dependencies (e.g., .NET SDK version).

### Requirement: CLI usage with all flags
The CLI documentation SHALL document all command-line flags and options with descriptions and examples.

#### Scenario: Flag reference
- **WHEN** a user reads the CLI usage section
- **THEN** they find documentation for each flag: `--output`, `--format`, `--resource-h`, `--encoding`, `--locale`, `--stats`, `--coverage`, `--verbose`, `--help`.

#### Scenario: Example commands
- **WHEN** a user looks for usage examples
- **THEN** they see realistic command lines for common tasks.

### Requirement: Supported file formats
The CLI documentation SHALL describe each supported file format (.resx, .rc, .fhx, .ahc) with examples and parsing behavior.

#### Scenario: Format list
- **WHEN** a user reads the formats section
- **THEN** they see a list of supported formats with brief descriptions.

#### Scenario: Format examples
- **WHEN** a user wants to understand a specific format
- **THEN** they find example files and expected output for each format.

### Requirement: Output format schema
The CLI documentation SHALL explain the structure of the output `data-bank.json` file, including all fields and their meanings.

#### Scenario: Schema documentation
- **WHEN** a developer reads the output format section
- **THEN** they understand the JSON schema: `generated`, `entries[]`, and each entry's fields (`key`, `sourceString`, `locale`, `source`, `metadata`).

#### Scenario: Example output
- **WHEN** a user looks at the example output
- **THEN** they see a realistic `data-bank.json` snippet.

### Requirement: Common workflows
The CLI documentation SHALL provide step-by-step workflows for typical use cases (e.g., extracting strings from a project, generating coverage report).

#### Scenario: Workflow steps
- **WHEN** a user follows a workflow
- **THEN** they can complete the task without external help.

### Requirement: Architecture overview
The CLI documentation SHALL describe the internal architecture: parsers, models, and output generation.

#### Scenario: Architecture diagram
- **WHEN** a developer reads the architecture section
- **THEN** they understand the data flow: input files → parsers → `LocalizedStringEntry` list → `DataBankOutput` → JSON.

#### Scenario: Component description
- **WHEN** a developer reads about components
- **THEN** they know the role of each parser (ResxParser, RcParser, FhxParser, AhcParser) and model classes.

### Requirement: Parser details
The CLI documentation SHALL explain how each parser works, including locale detection and format-specific parsing logic.

#### Scenario: Parser specifics
- **WHEN** a developer reads about a specific parser
- **THEN** they understand its input format, extraction logic, and output fields.

#### Scenario: Locale detection
- **WHEN** a developer reads about locale detection
- **THEN** they understand how locale is determined for each format (file extension, content, override).

### Requirement: Coverage analysis
The CLI documentation SHALL explain how coverage analysis works, what metrics are produced, and how to interpret the report.

#### Scenario: Coverage metrics
- **WHEN** a user runs `--coverage`
- **THEN** they understand the summary fields: overall completion percentage, total EN keys, translated keys, missing keys, orphaned keys, per-locale breakdown.

### Requirement: Adding new parsers
The CLI documentation SHALL include a guide for extending DataBank with new file format parsers.

#### Scenario: Extension guide
- **WHEN** a developer wants to add support for a new format
- **THEN** they find step-by-step instructions: create parser class, implement `Parse` method, register in Program.cs.

### Requirement: Desktop app documentation
The documentation SHALL include a README at `DatabankTool/DataBank.Desktop/README.md` describing the WPF+WebView2 desktop frontend.

#### Scenario: Desktop overview
- **WHEN** a developer reads the Desktop README
- **THEN** they understand that DataBank.Desktop is a WPF application hosting a WebView2 browser control that provides a UI for the DataBank tools.

#### Scenario: Desktop build instructions
- **WHEN** a developer follows the Desktop build instructions
- **THEN** they can build the project using `dotnet build` from the `DatabankTool/DataBank.Desktop/` directory with all prerequisites documented.

#### Scenario: Desktop run instructions
- **WHEN** a developer wants to run the Desktop app
- **THEN** they find instructions for `dotnet run` or launching the built executable, including any required runtime dependencies (WebView2 Runtime).

#### Scenario: Desktop architecture
- **WHEN** a developer reads the Desktop architecture section
- **THEN** they understand the WPF host + WebView2 browser control pattern and how IPC works between the UI and backend logic.

#### Scenario: Desktop development setup
- **WHEN** a developer wants to contribute to the Desktop app
- **THEN** they find information about development prerequisites, debugging, and project structure.

### Requirement: API documentation via Swagger
The documentation SHALL note that API documentation is provided via Swagger/OpenAPI within the `DatabankTool/DataBank.Api/` project.

#### Scenario: API docs reference
- **WHEN** a developer reads the top-level DatabankTool README
- **THEN** they find a note that the API project includes Swagger/OpenAPI documentation, accessible at the `/swagger` endpoint when the API is running.

### Requirement: Update PROJECT_CONTEXT.md
The documentation change SHALL update `PROJECT_CONTEXT.md` to reflect the actual implementation status of Tool 2.

#### Scenario: Status update
- **WHEN** a reader views the Current Implementation Status table
- **THEN** Tool 2 status shows "Complete" with accurate description (4 parsers, not SARIF-based).

#### Scenario: Description accuracy
- **WHEN** a reader reads Tool 2 description
- **THEN** it matches the actual implementation: CLI tool that parses resx, rc, fhx, ahc files and outputs data-bank.json.

#### Scenario: Frontend status
- **WHEN** a reader reads the Tool 2 section
- **THEN** they see notes that Desktop (WPF+WebView2) and API (ASP.NET Core) frontends are now available.

### Requirement: Documentation locations
The documentation SHALL be placed at discoverable locations within the DatabankTool directory.

#### Scenario: CLI file existence
- **WHEN** a developer navigates to `DatabankTool/DataBank.Cli/`
- **THEN** they find a README.md file.

#### Scenario: Desktop file existence
- **WHEN** a developer navigates to `DatabankTool/DataBank.Desktop/`
- **THEN** they find a README.md file.

#### Scenario: Top-level file existence
- **WHEN** a developer navigates to `DatabankTool/`
- **THEN** they find a README.md file linking to sub-project docs.
