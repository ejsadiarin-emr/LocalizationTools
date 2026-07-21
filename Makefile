.PHONY: build build-analyzer build-cli build-desktop test clean analyze restore pack run-desktop publish-desktop

# Default target
all: build

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

# Full CI pipeline
ci: restore build test analyze
