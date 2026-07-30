## Why

The CLI tool currently uses the name `dv-extract` / `dv-extract-strings`, which is inconsistent with the project's branding as "DataBank Tool CLI". Renaming to `databank-cli` aligns the binary name with the project identity and improves discoverability.

## What Changes

- Rename the compiled binary from `dv-extract.exe` to `databank-cli.exe` (via `<AssemblyName>` in .csproj)
- Update all user-facing help text and usage examples in `Program.cs` to reference `databank-cli` instead of `dv-extract`
- Update documentation (`PROJECT_CONTEXT.md`, `DatabankTool/README.md`) to use the new name consistently

**BREAKING**: The executable name changes from `dv-extract.exe` to `databank-cli.exe`. Any scripts or workflows invoking `dv-extract` directly will need updating.

## Capabilities

### New Capabilities

None — this is a naming/branding change, not a feature addition.

### Modified Capabilities

None — no spec-level behavior changes.

## Impact

- **Files modified**: 4 files across documentation and source code
  - `PROJECT_CONTEXT.md` (3 occurrences)
  - `DataBank.Cli/DataBank.Cli.csproj` (1 occurrence — AssemblyName)
  - `DataBank.Cli/Program.cs` (5 occurrences — help text)
  - `DatabankTool/README.md` (2 occurrences)
- **Binary output**: `dv-extract.exe` → `databank-cli.exe`
- **Makefile**: No changes needed — targets use `dotnet run` and reference `DataBank.Cli.dll` directly
- **Breaking change**: Direct invocations of `dv-extract` must be updated to `databank-cli`
