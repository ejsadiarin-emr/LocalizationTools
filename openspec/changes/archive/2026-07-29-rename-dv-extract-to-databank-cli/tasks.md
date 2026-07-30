## 1. Update Project Documentation

- [x] 1.1 Rename `dv-extract-strings` to `databank-cli` in PROJECT_CONTEXT.md (3 occurrences: L81, L165, L211)
- [x] 1.2 Rename `dv-extract` to `databank-cli` in DatabankTool/README.md (2 occurrences: L66, L121)

## 2. Update Build Configuration

- [x] 2.1 Change `<AssemblyName>dv-extract</AssemblyName>` to `<AssemblyName>databank-cli</AssemblyName>` in DataBank.Cli/DataBank.Cli.csproj (L10)

## 3. Update Source Code Help Text

- [x] 3.1 Rename `dv-extract` to `databank-cli` in DataBank.Cli/Program.cs (5 occurrences: L234, L236, L253, L254, L255)

## 4. Verification

- [x] 4.1 Grep for remaining `dv-extract` references across the codebase
- [x] 4.2 Build the project to verify binary name change (`dotnet build`)
- [x] 4.3 Verify Makefile targets still work (`make build-databank`, `make run-databank`)
