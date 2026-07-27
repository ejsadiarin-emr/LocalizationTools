## Context

The DataBank.Cli tool is currently located at `src/DataBank.Cli/` alongside the main localization analyzer libraries. This creates confusion about the tool's role and makes the project structure less intuitive. The tool is a standalone CLI application that extracts data from localization files, distinct from the core analyzer libraries. Additionally, a new WPF+WebView2 desktop frontend and ASP.NET Core Web API are planned for the DataBank ecosystem and should be co-located.

The current structure has DataBank.Cli at `src/DataBank.Cli/` with references to shared helpers at `src/Helpers/` using relative paths `../Helpers/`. The test project is at `src/DataBank.Cli.Tests/` with a project reference to `../DataBank.Cli/DataBank.Cli.csproj`.

**Stakeholders:** Developers working on localization analysis tools who need to understand the project structure and maintain the codebase.

## Goals / Non-Goals

**Goals:**
- Improve project organization by moving DataBank.Cli to a dedicated `DatabankTool/` directory
- Provide a home for new DataBank projects: Desktop frontend and Web API
- Maintain all existing functionality and test coverage
- Update project references to work correctly from new locations
- Ensure the build system continues to work without changes to build scripts

**Non-Goals:**
- Modifying the shared helper files (`src/Helpers/EncodingDetector.cs` and `src/Helpers/CoverageAnalyzer.cs`)
- Changing the functionality or API of the DataBank.Cli tool
- Restructuring other projects in the `src/` directory
- Building the DataBank.Api (tracked in a separate change)

## Decisions

### Decision 1: Use `DatabankTool/` as the new parent directory

**Choice:** Move projects to `DatabankTool/DataBank.Cli/` and `DatabankTool/DataBank.Cli.Tests/`, and create new projects at `DatabankTool/DataBank.Desktop/` and `DatabankTool/DataBank.Api/`

**Alternatives considered:**
1. Keep in `src/` but rename to `src/DataBank.Cli.Extracted/` - Doesn't address the organizational confusion
2. Move to root as `DataBank.Cli/` - Too flat, doesn't group related tools
3. Create `tools/` directory - Less descriptive than `DatabankTool/`

**Rationale:** `DatabankTool/` clearly communicates the purpose of the projects within and provides a logical grouping for all DataBank-related tools including CLI, Desktop, and API.

### Decision 2: Update relative paths in csproj files

**Choice:** Update `DataBank.Cli.csproj` to use `../../src/Helpers/` paths and keep `DataBank.Cli.Tests.csproj` referencing `../DataBank.Cli/`

**Alternatives considered:**
1. Use absolute paths - Not portable across machines
2. Move helpers to new location - Changes shared code, violates non-goals
3. Use MSBuild properties - Adds complexity without benefit

**Rationale:** Relative paths maintain portability and clearly show the relationship between projects. The test project reference remains simple since both projects move together.

### Decision 3: No changes to build scripts or CI/CD

**Choice:** Maintain existing build commands and CI/CD pipelines

**Alternatives considered:**
1. Update build scripts to reflect new paths - Unnecessary since dotnet CLI handles project references
2. Add new build targets for the restructured projects - Adds complexity without benefit

**Rationale:** The .NET build system handles project references automatically. As long as csproj files are updated correctly, existing build commands will continue to work.

### Decision 4: Desktop follows existing WPF+WebView2 pattern

**Choice:** Model `DataBank.Desktop` after the existing `LocalizationAnalyzers.Desktop` project structure

**Alternatives considered:**
1. Use a different UI framework (MAUI, Avalonia) - Adds unfamiliar technology, team knows WPF+WebView2
2. Build a pure WPF app without WebView2 - Limits web-based UI flexibility

**Rationale:** The existing Desktop project provides a proven pattern. WebView2 loads a local web page from `wwwroot/` that calls the DataBank.Api via HTTP, keeping UI and backend cleanly separated.

## Risks / Trade-offs

**Risk:** Developers may have hardcoded paths in scripts or documentation → **Mitigation:** Update any documentation referencing old paths, communicate the change clearly.

**Risk:** Build may fail if relative paths are incorrect → **Mitigation:** Test build after each csproj update, verify all references resolve correctly.

**Risk:** IDE may not automatically update project references → **Mitigation:** Developers may need to reload projects in Visual Studio/Rider after the move.

**Risk:** Desktop project requires WebView2 Runtime installed on user machines → **Mitigation:** WebView2 Evergreen Runtime is automatically installed on Windows 10/11 via Windows Update.

**Trade-off:** Slightly deeper directory structure for DataBank projects → **Benefit:** Clearer organization and separation of concerns.

## Migration Plan

1. Create `DatabankTool/` directory at repository root
2. Move `src/DataBank.Cli/` to `DatabankTool/DataBank.Cli/`
3. Move `src/DataBank.Cli.Tests/` to `DatabankTool/DataBank.Cli.Tests/`
4. Create `DatabankTool/DataBank.Desktop/` project (WPF+WebView2, same pattern as `src/LocalizationAnalyzers.Desktop/`)
5. Create `DatabankTool/DataBank.Api/` project (ASP.NET Core Web API, in separate change)
6. Update `DatabankTool/DataBank.Cli/DataBank.Cli.csproj` helper references
7. Verify build succeeds with `dotnet build`
8. Verify tests pass with `dotnet test`
9. Update any documentation referencing old paths
