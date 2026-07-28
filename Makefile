.PHONY: build build-analyzer build-cli build-desktop test clean analyze analyze-test analyze-test-ca restore pack run-desktop publish-desktop ci build-databank build-databank-cli build-databank-desktop run-databank run-databank-desktop test-databank clean-databank restore-databank

# Default target
all: build build-databank

# Restore dependencies
restore:
	dotnet restore src/LocalizationAnalyzers.csproj
	dotnet restore src/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj

# ---------------------------------------------------------------------------------

# Build both analyzer and CLI
build: build-analyzer build-cli

# Build analyzer (netstandard2.0 for NuGet)
build-analyzer:
	dotnet build src/LocalizationAnalyzers.csproj -c Release -f netstandard2.0

# Build CLI tool (net10.0)
build-cli:
	dotnet build src/LocalizationAnalyzers.csproj -c Release -f net10.0

# Run tests
test:
	dotnet test src/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj

# Run analyzers and generate SARIF
analyze: build-cli
	dotnet run --project src/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- src results.sarif

# Clean build artifacts
clean:
	dotnet clean src/LocalizationAnalyzers.csproj -c Release
	dotnet clean src/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj
	rm -f src/results.sarif

# Create NuGet package (analyzer)
pack: build-analyzer
	dotnet pack src/LocalizationAnalyzers.csproj -c Release -f netstandard2.0

# Create dotnet tool package
pack-tool: build-cli
	dotnet pack src/LocalizationAnalyzers.csproj -c Release --no-build -p:TargetFrameworks=net10.0 -p:PackAsDotnetTool=true

pack-tool-2: build-cli
	dotnet pack src/LocalizationAnalyzers.csproj -c Release --no-build -p:TargetFrameworks=net10.0 -p:PackAsDotnetTool=true

# ---------------------------------------------------------------------------------
# Build desktop app (WPF + WebView2)
build-desktop:
	dotnet build src/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release

# Run desktop app
run-desktop: build-desktop
	dotnet run --project src/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release --no-build

# Publish self-contained single-file desktop app
publish-desktop:
	dotnet publish src/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Run analyzers on test codebase (LOC rules only)
analyze-test: build-cli
	dotnet run --project src/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/

# Run analyzers on test codebase with CA rules (LOC + CA globalization rules)
analyze-test-ca: build-cli
	dotnet run --project src/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/ --with-ca-rules

# Full CI pipeline
ci: restore build test analyze

# ---------------------------------------------------------------------------------
# DatabankTool targets (CLI + Desktop + Tests)
# ---------------------------------------------------------------------------------
# Databank CLI usage:
#   make run-databank INPUT_DIR=./l10n-files
#   make run-databank INPUT_DIR=./l10n-files ARGS="--output ./out/data-bank.json --stats"
#   make run-databank INPUT_DIR=./l10n-files ARGS="--format resx --verbose"
#
# INPUT_DIR is required and should point to a folder containing resource files.
# Supported file formats: resx, rc, fhx, ahc, json

restore-databank:
	dotnet restore DatabankTool/DatabankTool.sln

build-databank: build-databank-cli build-databank-desktop

build-databank-cli:
	dotnet build DatabankTool/DataBank.Cli/DataBank.Cli.csproj -c Release

run-databank: build-databank-cli
ifndef INPUT_DIR
	$(error INPUT_DIR is required. Usage: make run-databank INPUT_DIR=./l10n-files)
endif
	dotnet run --project DatabankTool/DataBank.Cli/DataBank.Cli.csproj -c Release --no-build --input-dir $(INPUT_DIR) $(ARGS)

build-databank-desktop:
	dotnet build DatabankTool/DataBank.Desktop/DataBank.Desktop.csproj -c Release

run-databank-desktop: build-databank-desktop
	dotnet run --project DatabankTool/DataBank.Desktop/DataBank.Desktop.csproj -c Release --no-build

test-databank:
	dotnet test DatabankTool/DataBank.Cli.Tests/DataBank.Cli.Tests.csproj

clean-databank:
	dotnet clean DatabankTool/DatabankTool.sln -c Release
	rm -rf DatabankTool/DataBank.Cli/bin DatabankTool/DataBank.Cli/obj
	rm -rf DatabankTool/DataBank.Cli.Tests/bin DatabankTool/DataBank.Cli.Tests/obj
	rm -rf DatabankTool/DataBank.Desktop/bin DatabankTool/DataBank.Desktop/obj
