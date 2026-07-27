## Why

The DataBank tools are fully implemented but lack documentation. The CLI tool, desktop app, and web API have no README or developer-facing docs. The PROJECT_CONTEXT.md incorrectly lists Tool 2 as "Not started" even though it's complete. Comprehensive documentation is needed to explain the tools' capabilities, usage, architecture, and to correct the project status.

## What Changes

- Create `DatabankTool/README.md` with an overview of the entire DatabankTool directory and its sub-projects.
- Create `DatabankTool/DataBank.Cli/README.md` with full CLI documentation:
  - Overview of what DataBank does
  - Installation and build instructions
  - CLI usage with all flags
  - Supported formats with examples
  - Output format (data-bank.json schema)
  - Common workflows
  - Architecture overview (parsers, models, output)
  - How parsers work, locale detection, coverage analysis
  - How to add new parsers
- Create `DatabankTool/DataBank.Desktop/README.md` with desktop app documentation:
  - Overview of the WPF + WebView2 desktop frontend
  - Build instructions and prerequisites
  - How to run the app
  - Architecture (WPF host, WebView2 browser control, IPC with CLI/API)
  - Development setup and debugging
- API documentation is provided via Swagger/OpenAPI within the `DatabankTool/DataBank.Api/` project (built in a separate change).
- Update `PROJECT_CONTEXT.md`:
  - Change Tool 2 status from "Not started" to "Complete"
  - Update Tool 2 description to match actual implementation (4 parsers, not SARIF-based)
  - Note the actual output format (richer than originally spec'd)
  - Add note that Desktop and API frontends are now available

## Capabilities

### New Capabilities
- `databank-docs`: Comprehensive documentation for all DataBank sub-projects: CLI, Desktop, and API, including usage, architecture, and extension guides.

### Modified Capabilities
- (none)

## Impact

- Affected files: `DatabankTool/README.md` (new), `DatabankTool/DataBank.Cli/README.md` (new), `DatabankTool/DataBank.Desktop/README.md` (new), `PROJECT_CONTEXT.md` (updated)
- No code changes; documentation only.
- Improves developer onboarding and tool adoption across all DataBank sub-projects.
- Corrects project status tracking.
