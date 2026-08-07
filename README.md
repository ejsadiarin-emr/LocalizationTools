# LocalizationTools

Tooling for the DeltaV localization initiative: static analysis of C# for localization problems, extraction of translatable strings into a **Data Bank**, and a desktop UI for browsing/editing translations.

## Table of Contents

- [Repository Layout](#repository-layout)
- [Prerequisites](#prerequisites)
- [Quick Start — LocalizationAnalyzers](#quick-start--localizationanalyzers-static-analysis)
  - [Run the CLI](#run-the-cli-on-a-directory-of-c-files)
  - [Inspect SARIF output](#inspect-the-sarif-output)
  - [Desktop app](#run-the-desktop-app-gui)
- [Quick Start — DatabankTool](#quick-start--databanktool)
  - [Local mode (no server)](#option-a-local-mode--extract-and/browse-data-bankjson-no-server)
  - [Remote mode (MongoDB)](#option-b-remote-mode--api--mongodb-stack)
  - [Desktop app features](#common-things-to-do-in-the-databank-desktop-app)
- [CLI Reference Summaries](#cli-reference-summaries)
- [Testing](#testing)

## Repository Layout

```
├── Makefile                       # shared build/run targets for both tools
├── LocalizationAnalyzer/          # LocalizationAnalyzers (analyzers + CLI + desktop app)
├── DatabankTool/                  # databank-cli, DataBank.Api, DataBank.Desktop, docker-compose.yml
├── l10n-files/                    # sample resource files (EN + Translated) for FHX/RC/GRF/RESX/AHC
├── test-codebase/                 # sample C# code used by the analyzer CLI targets
├── data-bank.json                 # extracted data-bank output (repo-local sample)
└── results.sarif / published_results.sarif   # example analyzer SARIF output
```

This repo contains **two related but separate tools**:

| Tool | What it does | Location |
|------|--------------|----------|
| **LocalizationAnalyzers** | Roslyn analyzers (LOC001–LOC015) that classify string literals as *behavioral* or *display*, plus a CLI and a WPF desktop app. Outputs enriched **SARIF 2.1.0**. | `LocalizationAnalyzer/` ([README](LocalizationAnalyzer/README.md)) |
| **DatabankTool** | Extracts translatable strings from resource files (`.resx`, `.rc`, `.fhx`, `.ahc`, `.json`, `.grf`) into `data-bank.json`, stores them in **MongoDB** via a REST API, and provides a WPF desktop app (Local and Remote modes). | `DatabankTool/` ([README](DatabankTool/README.md)) |


> Both tools share the `Makefile` at the repo root. Raw `dotnet run` equivalents are shown for each target so you can run without `make`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — only needed for Databank **Remote** mode (MongoDB)
- [VS Code](https://code.visualstudio.com/) — optional, for "Open Source File in VS Code" from the Databank desktop app

## Quick Start — LocalizationAnalyzers (static analysis)

### Run the CLI on a directory of C# files

```bash
# make:
make run-la              # runs against LocalizationAnalyzer/ → results.sarif
make run-la-test         # runs against test-codebase/ → stdout
make run-la-test-ca      # same, plus built-in CA globalization rules (CA1303–CA1311)

# raw dotnet run (directory → SARIF file):
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- <directory> [output.sarif] [--with-ca-rules]
```

Example:

```bash
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/ results.sarif
```

- Point it at any **folder** containing `*.cs` files (or a `.csproj` — the directory is used). `bin`, `obj`, `Test`, `TestResults` folders are skipped.
- SARIF goes to the second argument; omit it to print to stdout.
- `--with-ca-rules` also runs Microsoft's built-in globalization analyzers (CA1303–CA1311).

### Inspect the SARIF output

SARIF 2.1.0 files are plain JSON — inspect with any JSON viewer, or use the SARIF extensions for [VS Code](https://marketplace.visualstudio.com/items?itemName=MS-SarifVSCode.sarif-viewer), GitHub Code Scanning, **SonarQube**, or **Azure DevOps** (all consume SARIF natively):

- GitHub: the `.github/workflows/analyze.yml` workflow runs the CLI on `LocalizationAnalyzer/` and uploads `string_classification_results.sarif` to Code Scanning.
- Locally: `results.sarif` / `published_results.sarif` at the repo root are examples of the CLI output.

### Run the desktop app (GUI)

```bash
make run-la-desktop

# raw:
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release
```

Common things to do in the app:

1. Click **Browse Folder** (or type a path) to select a directory containing C# files.
2. Toggle individual **LOC rules** on/off and **Include CA Rules** to add the built-in globalization analyzers.
3. Click **Run Analysis** — a summary panel shows files / lines / diagnostics / duration.
4. Click any row to **expand details**: classification badge, source snippet, string literal, rule description, related rules, and bad/good code examples.
5. Click **Export SARIF** to save the current run to a `.sarif` file for uploading to SonarQube, GitHub Code Scanning, or Azure DevOps.

---

## Quick Start — DatabankTool

### Option A: Local mode — extract and browse `data-bank.json` (no server)

```bash
# 1. Extract strings from a folder of resource files → ./data-bank.json
make run-databank INPUT_DIR=./l10n-files
# or raw:
dotnet run --project DatabankTool/DataBank.Cli/DataBank.Cli.csproj -c Release -- --input-dir ./l10n-files

# with options (output path, stats, format filter, verbose):
make run-databank INPUT_DIR=./l10n-files ARGS="--output ./out/data-bank.json --stats --verbose"

# 2. Browse the result in the desktop app
make run-databank-desktop
# or raw:
dotnet run --project DatabankTool/DataBank.Desktop/DataBank.Desktop.csproj -c Release
```

In the desktop app, stay in **Local** mode (default) and click **Load DataBank JSON** to pick the generated `data-bank.json`.

*Everything is offline - no server required.*


---

### Option B: Remote mode — API + MongoDB stack

```bash
# Start MongoDB and the API server via docker compose:
make run-databank-stack

# or raw equivalent:
docker compose -f DatabankTool/docker-compose.yml up -d mongodb
dotnet run --project DatabankTool/DataBank.Api/DataBank.Api.csproj -c Release
```

This starts:

- **MongoDB** on `localhost:27017` (database `databank`)
- **API** on `http://localhost:5000`
- **Swagger UI** at `http://localhost:5000/swagger`

Then import extracted data and browse it remotely:

```bash
# this outputs "data-bank.json" that you can import in the desktop app
make run-databank INPUT_DIR=./l10n-files

# desktop app → switch to Remote mode (Connect API)
make run-databank-desktop

# In "remote mode", click "Import data to API" and select data-bank.json
# or curl directly to import data-bank.json to MongoDB database via API
curl -X POST http://localhost:5000/api/import -F "file=@data-bank.json"
```

Stop the stack with `make stop-mongo` (Ctrl+C the API first).

> `docker compose` commands use the compose file at `DatabankTool/docker-compose.yml`.
> `make start-mongo` / `make stop-mongo` / `make run-databank-api` / `make import-data`
> are the split-out variants.

### Common things to do in the Databank desktop app

- **Mode switcher (Local / Remote)** — Local loads `data-bank.json` from disk; Remote connects to the API (`http://localhost:5000`, base path configurable for source-file resolution, `~` expands to your home directory). Mode persists across restarts.
- **Dashboard** — total entries, locales, formats, translated/untranslated counts, and a per-locale completion bar.
- **Filter** — multi-select **locales**, **format** (resx/rc/fhx/ahc/json/grf), **status** (Translated / Untranslated / Do Not Translate), and free-text **search** across keys and values.
- **Edit translations in place** — double-click a locale cell, type, Enter to save. The change is **written back to the source file** (needs the entry's source info and base path).
- **Entry detail** — click any row for full details, source file/line, and an **Open Source File** button (opens in VS Code with the correct line).
- **Export JSON** — exports the current filtered view to a timestamped `databank-export-<timestamp>-<locales>.json`; useful for handing subsets to translators.
- **Import Data to API** (Remote only) — upload a `data-bank.json` straight from the app instead of `curl`.
- **GRF Files tab** — separate list of GRF entries (shown apart from the main table).

## CLI Reference Summaries

### LocalizationAnalyzers CLI

```bash
LocalizationAnalyzers <project-path> [output-file] [--with-ca-rules]
```

`<project-path>` is a directory (or `.csproj`) containing C# files. 

Emits SARIF 2.1.0 with `invocations[]` timing, per-file `fileMetrics[]` (size, line count, diagnostics, duration), and per-result `classification`, `sourceSnippet`, `stringLiteral` properties.

### databank-cli

```bash
databank-cli [--input-dir <path>] [--output <path>] [--format <resx|rc|fhx|ahc|json|grf>]
             [--resource-h <path>] [--encoding <enc>] [--locale <locale>]
             [--stats] [--coverage] [--coverage-output <path>] [--flag-untranslated] [--verbose]
databank-cli edit --key <key> --locale <locale> --value <value> --file <data-bank.json> [--dry-run]
```

## Testing

```bash
make test-la          # analyzer unit tests
make test-databank    # databank CLI tests
```

