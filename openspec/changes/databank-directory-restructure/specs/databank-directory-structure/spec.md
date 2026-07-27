## ADDED Requirements

### Requirement: DatabankTool top-level directory
A `DatabankTool/` directory SHALL exist at the repository root and contain all DataBank-related projects.

#### Scenario: DatabankTool directory exists
- **WHEN** a developer looks at the repository structure
- **THEN** the `DatabankTool/` directory is found at the repository root

### Requirement: DataBank.Cli project location
The DataBank.Cli project SHALL be located at `DatabankTool/DataBank.Cli/` directory.

#### Scenario: DataBank.Cli project exists at new location
- **WHEN** a developer looks at the repository structure
- **THEN** the `DataBank.Cli.csproj` file is found at `DatabankTool/DataBank.Cli/DataBank.Cli.csproj`

#### Scenario: DataBank.Cli source files are organized
- **WHEN** a developer examines the DataBank.Cli project directory
- **THEN** all source files (.cs) are located within `DatabankTool/DataBank.Cli/` and its subdirectories

### Requirement: DataBank.Cli.Tests project location
The DataBank.Cli.Tests project SHALL be located at `DatabankTool/DataBank.Cli.Tests/` directory.

#### Scenario: DataBank.Cli.Tests project exists at new location
- **WHEN** a developer looks at the repository structure
- **THEN** the `DataBank.Cli.Tests.csproj` file is found at `DatabankTool/DataBank.Cli.Tests/DataBank.Cli.Tests.csproj`

#### Scenario: Test source files are organized
- **WHEN** a developer examines the test project directory
- **THEN** all test source files are located within `DatabankTool/DataBank.Cli.Tests/` and its subdirectories

### Requirement: DataBank.Desktop project location
A new DataBank.Desktop project SHALL be created at `DatabankTool/DataBank.Desktop/` as a WPF+WebView2 desktop frontend.

#### Scenario: DataBank.Desktop project exists
- **WHEN** a developer looks at the repository structure
- **THEN** the `DataBank.Desktop.csproj` file is found at `DatabankTool/DataBank.Desktop/DataBank.Desktop.csproj`

#### Scenario: DataBank.Desktop is a WPF application
- **WHEN** a developer examines the `DataBank.Desktop.csproj` file
- **THEN** it targets `net10.0-windows`, enables `<UseWPF>true</UseWPF>`, and references `Microsoft.Web.WebView2`

#### Scenario: DataBank.Desktop contains WebView2 UI
- **WHEN** a developer examines the project structure
- **THEN** a `MainWindow.xaml` with a WebView2 control exists and a `wwwroot/` folder contains static web assets

#### Scenario: DataBank.Desktop calls the API via HTTP
- **WHEN** a developer examines the Desktop application
- **THEN** the WebView2 control loads a web page from `wwwroot/` that makes HTTP calls to the DataBank.Api

### Requirement: DataBank.Api project location
A new DataBank.Api project SHALL be created at `DatabankTool/DataBank.Api/` as an ASP.NET Core Web API (built in a separate change).

#### Scenario: DataBank.Api project exists
- **WHEN** a developer looks at the repository structure
- **THEN** the `DataBank.Api.csproj` file is found at `DatabankTool/DataBank.Api/DataBank.Api.csproj`

### Requirement: Shared helpers reference paths
The DataBank.Cli project SHALL reference shared helpers in `src/Helpers/` using relative paths that correctly resolve from the new location.

#### Scenario: EncodingDetector reference works
- **WHEN** DataBank.Cli project references EncodingDetector.cs
- **THEN** the relative path `../../src/Helpers/EncodingDetector.cs` correctly resolves to the file at `src/Helpers/EncodingDetector.cs`

#### Scenario: CoverageAnalyzer reference works
- **WHEN** DataBank.Cli project references CoverageAnalyzer.cs
- **THEN** the relative path `../../src/Helpers/CoverageAnalyzer.cs` correctly resolves to the file at `src/Helpers/CoverageAnalyzer.cs`

### Requirement: Build configuration maintains compatibility
The build configuration SHALL ensure that all projects continue to build successfully after the directory restructure.

#### Scenario: DataBank.Cli builds successfully
- **WHEN** a developer runs `dotnet build` on the DataBank.Cli project
- **THEN** the build completes without errors

#### Scenario: DataBank.Cli.Tests builds and runs
- **WHEN** a developer runs `dotnet test` on the DataBank.Cli.Tests project
- **THEN** all tests pass successfully

#### Scenario: Solution file includes new paths
- **WHEN** a developer opens the solution file
- **THEN** the solution references the projects at their new locations under DatabankTool/
