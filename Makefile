.PHONY: build build-analyzer build-cli test clean analyze restore pack

# Default target
all: build

# Restore dependencies
restore:
	dotnet restore src/LocalizationAnalyzers.csproj
	dotnet restore src/LocalizationAnalyzers.Tests/LocalizationAnalyzers.Tests.csproj

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

# Create NuGet package
pack: build-analyzer
	dotnet pack src/LocalizationAnalyzers.csproj -c Release -f netstandard2.0

# Full CI pipeline
ci: restore build test analyze
