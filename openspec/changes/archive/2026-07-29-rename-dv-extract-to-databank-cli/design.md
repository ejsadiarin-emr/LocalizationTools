## Context

The project has a CLI tool built in C# (.NET) located at `DataBank.Cli/`. The tool's binary name is currently `dv-extract` (set via `<AssemblyName>` in the .csproj file), and all user-facing text references `dv-extract` or `dv-extract-strings`. The project branding is "DataBank Tool CLI", creating a naming inconsistency.

Current state:
- Binary name: `dv-extract.exe`
- Help text references: `dv-extract`, `dv-extract-strings`
- Documentation references: `dv-extract-strings` (PROJECT_CONTEXT.md), `dv-extract` (README.md)

## Goals / Non-Goals

**Goals:**
- Rename the compiled binary from `dv-extract` to `databank-cli`
- Update all user-facing help text and usage examples to reference `databank-cli`
- Update documentation to use the new name consistently
- Maintain backward compatibility for Makefile targets (already use `dotnet run`)

**Non-Goals:**
- Renaming the project namespace or folder structure
- Changing any tool functionality or behavior
- Updating any external scripts or CI/CD that may reference `dv-extract` (out of scope)

## Decisions

**1. Binary name: `databank-cli`**

Chosen from options: `databank-cli`, `databank`, `dv-databank`.

Rationale: `databank-cli` is descriptive, follows CLI naming conventions (tool-name + cli suffix), and clearly identifies the tool's purpose. The `dv-` prefix was dropped as it's an artifact of the old naming.

**2. Edit strategy: Find-and-replace with context verification**

Each occurrence will be edited individually with surrounding context to avoid accidental replacements. The replacements are:
- `dv-extract-strings` → `databank-cli` (in documentation)
- `dv-extract` → `databank-cli` (in source code and diagrams)

**3. No Makefile changes required**

The Makefile targets (`run-databank`, `build-databank`) already use `dotnet run` which references `DataBank.Cli.dll` directly, not the binary name. The `<AssemblyName>` change only affects the compiled output filename.

## Risks / Trade-offs

**[Risk] External scripts referencing `dv-extract`** → Mitigation: This is a known breaking change documented in the proposal. Users with direct `dv-extract` invocations will need to update to `databank-cli`.

**[Risk] Accidental partial rename** → Mitigation: All 11 occurrences identified and will be edited with context verification. Post-change grep will confirm no remaining `dv-extract` references.

**[Trade-off] Binary name change vs. symlink** → We chose direct rename over creating symlinks for simplicity. The Makefile already abstracts the binary invocation via `dotnet run`.
