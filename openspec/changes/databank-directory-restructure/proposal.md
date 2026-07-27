## Why

The DataBank.Cli tool and its tests are currently located within the `src/` directory alongside the main localization analyzers. This creates confusion about the tool's role and makes the project structure less intuitive. Moving these projects to a dedicated `DatabankTool/` directory at the repository root better reflects their purpose as standalone tools separate from the core analyzer libraries. Additionally, a new WPF+WebView2 desktop frontend and ASP.NET Core Web API are planned, which belong in this same top-level grouping. This reorganization improves code maintainability and developer experience.

## What Changes

- Move `src/DataBank.Cli/` to `DatabankTool/DataBank.Cli/`
- Move `src/DataBank.Cli.Tests/` to `DatabankTool/DataBank.Cli.Tests/`
- Create new `DatabankTool/DataBank.Desktop/` project (WPF+WebView2 desktop frontend, same pattern as `src/LocalizationAnalyzers.Desktop/`)
- Create new `DatabankTool/DataBank.Api/` project (ASP.NET Core Web API, built in a separate change)
- Update relative paths in `DatabankTool/DataBank.Cli/DataBank.Cli.csproj` to reference shared helpers in `src/Helpers/`
- Maintain existing functionality and all test coverage
- No changes to the shared helpers (`src/Helpers/EncodingDetector.cs` and `src/Helpers/CoverageAnalyzer.cs`)

## Capabilities

### New Capabilities
- `databank-directory-structure`: Defines the new directory layout for all DataBank projects under DatabankTool/

### Modified Capabilities
- None (no existing specs to modify)

## Impact

- **Code**: `src/DataBank.Cli/` and `src/DataBank.Cli.Tests/` directories will be moved
- **New Code**: `DatabankTool/DataBank.Desktop/` and `DatabankTool/DataBank.Api/` will be created
- **Project Files**: `DataBank.Cli.csproj` will have updated relative path references to shared helpers
- **Build System**: Project references and build paths will be updated
- **Dependencies**: No external dependencies change for moved projects; Desktop adds WPF+WebView2, Api adds ASP.NET Core
- **Testing**: All existing tests will continue to work in their new location
