.PHONY: build build-analyzer build-cli build-desktop test clean analyze analyze-test analyze-test-ca restore pack run-desktop publish-desktop ci build-databank build-databank-cli build-databank-desktop run-databank run-databank-desktop test-databank clean-databank restore-databank start-mongo stop-mongo run-databank-api run-databank-stack build-la build-la-analyzer build-la-cli test-la clean-la pack-la pack-la-tool build-la-desktop run-la-desktop publish-la-desktop run-la-test run-la-test-ca ci-la restore-la

# Default target
all: build-la build-databank

# ---------------------------------------------------------------------------------
# LocalizationAnalyzer targets
# ---------------------------------------------------------------------------------

# Restore dependencies
restore-la:
	dotnet restore LocalizationAnalyzer/LocalizationAnalyzers.csproj
	dotnet restore LocalizationAnalyzer/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj

# Build both analyzer and CLI
build-la: build-la-analyzer build-la-cli

# Build analyzer (netstandard2.0 for NuGet)
build-la-analyzer:
	dotnet build LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release -f netstandard2.0

# Build CLI tool (net10.0)
build-la-cli:
	dotnet build LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release -f net10.0

# Run tests
test-la:
	dotnet test LocalizationAnalyzer/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj

# Run analyzers and generate SARIF
run-la: build-la-cli
	dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- src results.sarif

# Clean build artifacts
clean-la:
	dotnet clean LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release
	dotnet clean LocalizationAnalyzer/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj
	rm -f LocalizationAnalyzer/results.sarif

# Create NuGet package (analyzer)
pack-la: build-la-analyzer
	dotnet pack LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release -f netstandard2.0

# Create dotnet tool package
pack-la-tool: build-la-cli
	dotnet pack LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release --no-build -p:TargetFrameworks=net10.0 -p:PackAsDotnetTool=true

pack-la-tool-2: build-la-cli
	dotnet pack LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release --no-build -p:TargetFrameworks=net10.0 -p:PackAsDotnetTool=true

# Build desktop app (WPF + WebView2)
build-la-desktop:
	dotnet build LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release

# Run desktop app
run-la-desktop: build-la-desktop
	dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release --no-build

# Publish self-contained single-file desktop app
publish-la-desktop:
	dotnet publish LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Run analyzers on test codebase (LOC rules only)
run-la-test: build-la-cli
	dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/

# Run analyzers on test codebase with CA rules (LOC + CA globalization rules)
run-la-test-ca: build-la-cli
	dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/ --with-ca-rules

# Full CI pipeline
ci-la: restore-la build-la test-la run-la

# ---------------------------------------------------------------------------------
# Backward-compat aliases (deprecated — prefer la- prefixed targets)
# ---------------------------------------------------------------------------------
restore: restore-la
build: build-la
build-analyzer: build-la-analyzer
build-cli: build-la-cli
test: test-la
analyze: run-la
clean: clean-la
pack: pack-la
pack-tool: pack-la-tool
pack-tool-2: pack-la-tool-2
build-desktop: build-la-desktop
run-desktop: run-la-desktop
publish-desktop: publish-la-desktop
analyze-test: run-la-test
analyze-test-ca: run-la-test-ca
ci: ci-la

# ---------------------------------------------------------------------------------
# DatabankTool targets (CLI + Desktop + Tests)
# ---------------------------------------------------------------------------------
# Databank CLI usage:
#   make run-databank INPUT_DIR=./l10n-files
#   make run-databank INPUT_DIR=./l10n-files ARGS="--output ./out/data-bank.json --stats"
#   make run-databank INPUT_DIR=./l10n-files ARGS="--format resx --verbose"
#
# INPUT_DIR is required and should point to a folder containing resource files.
# Supported file formats: resx, rc, fhx, ahc, json, grf

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

# ---------------------------------------------------------------------------------
# Databank API + MongoDB targets (via docker-compose)
# ---------------------------------------------------------------------------------
COMPOSE_FILE := DatabankTool/docker-compose.yml
MONGO_PORT := 27017
API_PORT := 5000

# Start MongoDB via docker-compose
start-mongo:
	@echo "Starting MongoDB container..."
	docker compose -f $(COMPOSE_FILE) up -d mongodb
	@echo "MongoDB started on port $(MONGO_PORT)"

# Stop MongoDB via docker-compose
stop-mongo:
	@echo "Stopping MongoDB container..."
	docker compose -f $(COMPOSE_FILE) down
	@echo "MongoDB stopped"

# Build and run the API server
run-databank-api: build-databank-cli
	@echo "Starting DataBank API on port $(API_PORT)..."
	@echo "Swagger UI: http://localhost:$(API_PORT)/swagger"
	dotnet run --project DatabankTool/DataBank.Api/DataBank.Api.csproj -c Release

# Start both MongoDB and API (full stack)
run-databank-stack: start-mongo
	@echo ""
	@echo "=== DataBank Stack ==="
	@echo "MongoDB:  localhost:$(MONGO_PORT)"
	@echo "API:      http://localhost:$(API_PORT)"
	@echo "Swagger:  http://localhost:$(API_PORT)/swagger"
	@echo ""
	@echo "After import with: make import-data"
	@echo "Press Ctrl+C to stop, then run 'make stop-mongo' to cleanup"
	@echo ""
	$(MAKE) run-databank-api

# Import data-bank.json into MongoDB via API (run in separate terminal after stack starts)
import-data:
	@echo "Importing data-bank.json into MongoDB..."
	curl -X POST http://localhost:$(API_PORT)/api/import -F "file=@data-bank.json"
	@echo ""
