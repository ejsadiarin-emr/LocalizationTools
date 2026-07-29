# DatabankTool

A localization data extraction and management pipeline for DeltaV. Extracts translatable strings from resource files (.resx, .rc, .fhx, .ahc, .json, .grf), stores them in MongoDB, and provides a desktop and web interface for browsing and managing translations.

## Quick How to Run

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for MongoDB)
- [MongoDB Compass](https://www.mongodb.com/compass) (optional, for visual DB inspection)

### Option 1: Full Stack (MongoDB + API)

```bash
# Start MongoDB container and API server
make run-databank-stack
```

This starts:
- **MongoDB** on `localhost:27017`
- **API** on `http://localhost:5000`
- **Swagger UI** at `http://localhost:5000/swagger`

To stop:
```bash
# Stop the API (Ctrl+C), then stop MongoDB
make stop-mongo
```

### Option 2: Desktop App

```bash
# Build and run the Desktop app
make run-databank-desktop
```

The app starts in **Local mode** by default. Switch to **Remote mode** in the toolbar to connect to a running API.

### Option 3: CLI Extraction Only

```bash
# Extract strings from a directory of resource files
make run-databank INPUT_DIR=./l10n-files

# With options
make run-databank INPUT_DIR=./l10n-files ARGS="--output ./out/data-bank.json --stats --verbose"
```

### Connecting to MongoDB Compass

1. Open MongoDB Compass
2. Enter connection string: `mongodb://localhost:27017`
3. Click **Connect**
4. Select the `databank` database

Collections:
- `DataBankEntry` — localized string entries
- `DataBankMetadata` — dataset version and counts
- `TranslationSession` — translation session tracking

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  Resource   │────▶│  dv-extract  │────▶│ data-bank   │
│  Files      │     │  (CLI)       │     │ .json       │
└─────────────┘     └──────────────┘     └──────┬──────┘
                                                │
                          ┌─────────────────────┤
                          ▼                     ▼
                   ┌─────────────┐     ┌──────────────┐
                   │   Import    │     │     API      │
                   │  (deprecated│     │  /api/import │
                   └──────┬──────┘     └──────┬───────┘
                          │                   │
                          └─────────┬─────────┘
                                    ▼
                              ┌───────────┐
                              │  MongoDB  │
                              └─────┬─────┘
                                    │
                          ┌─────────┴─────────┐
                          ▼                   ▼
                   ┌─────────────┐     ┌──────────────┐
                   │   Desktop   │     │  Swagger UI  │
                   │  (WPF +     │     │  /swagger    │
                   │  WebView2)  │     │              │
                   └─────────────┘     └──────────────┘
```

## Sub-Projects

| Project | Description | Run |
|---------|-------------|-----|
| **DataBank.Cli** | CLI tool that extracts strings from resource files | `make run-databank INPUT_DIR=...` |
| **DataBank.Api** | ASP.NET Core Web API with MongoDB backend | `make run-databank-api` |
| **DataBank.Desktop** | WPF + WebView2 desktop frontend | `make run-databank-desktop` |
| **DataBank.Import** | **Deprecated** — JSON-to-MongoDB importer | Use `POST /api/import` instead |

## Makefile Commands

| Command | Description |
|---------|-------------|
| `make build-databank` | Build CLI and Desktop |
| `make build-databank-cli` | Build CLI only |
| `make build-databank-desktop` | Build Desktop only |
| `make run-databank INPUT_DIR=<path>` | Run CLI extraction |
| `make run-databank-desktop` | Run Desktop app |
| `make run-databank-api` | Run API server |
| `make run-databank-stack` | Start MongoDB + API |
| `make start-mongo` | Start MongoDB container |
| `make stop-mongo` | Stop MongoDB container |
| `make test-databank` | Run CLI tests |
| `make clean-databank` | Clean build artifacts |
| `make restore-databank` | Restore NuGet packages |

## CLI Reference

```
dv-extract --input-dir <path> [options]

Options:
  --input-dir <path>      Input directory to scan (default: current dir)
  --output, -o <path>     Output file path (default: ./data-bank.json)
  --format, -f <format>   Filter: resx, rc, fhx, ahc, json, grf
  --resource-h <path>     Path to resource.h for .rc symbol resolution
  --encoding <enc>        Override file encoding (e.g., windows-1252)
  --locale <locale>       Override locale for FHX Translated files
  --stats, -s             Print summary statistics
  --coverage              Generate coverage analysis report
  --verbose, -v           Print per-file parsing progress
  --flag-untranslated     Flag entries with translation status analysis
  --help, -h              Show help
```

## Supported File Formats

| Format | Extension | Description |
|--------|-----------|-------------|
| **resx** | `.resx` | .NET XML resource files. Locale detected from filename (e.g., `Messages.fr.resx`). |
| **rc** | `.rc` | Windows C/C++ resource files. Requires `resource.h` for symbol resolution. |
| **fhx** | `.fhx`, `.txt` | DeltaV FHX alarm files. Locale from file path or content detection. |
| **ahc** | `.ahc` | DeltaV alarm history files. Auto-detects encoding. |
| **json** | `.json` | JSON translation files (e.g., `translate.en.json`). |
| **grf** | `.grf` | DeltaV GRF template files. |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check with entry count |
| `POST` | `/api/import` | Import `data-bank.json` (multipart upload) |
| `GET` | `/api/entries` | List entries (filter: `?locale=`, `?format=`, `?key=`) |
| `GET` | `/api/entries/{id}` | Get entry by ID |
| `POST` | `/api/entries` | Create entry |
| `PUT` | `/api/entries/{id}` | Update entry |
| `DELETE` | `/api/entries/{id}` | Delete entry |
| `GET` | `/api/metadata` | Dataset metadata |
| `GET` | `/api/stats` | Localization statistics |
| `GET` | `/api/stats/coverage` | Coverage summary |
| `POST` | `/api/extract` | Trigger extraction from source files |
| `GET` | `/api/extract/{jobId}` | Check extraction job status |
| `GET` | `/api/databank/export` | Export full dataset as JSON |
| `GET` | `/api/sessions` | List translation sessions |
| `POST` | `/api/sessions` | Create translation session |

Full interactive API docs available at `http://localhost:5000/swagger` when the API is running.

## Desktop App Modes

### Local Mode
- Load a `data-bank.json` file from disk via file dialog
- No server required — works completely offline

### Remote Mode
- Connects to the API at `http://localhost:5000`
- Fetches entries from MongoDB
- Shows connection status with Retry / Switch to Local fallback buttons
- Mode preference persists across app restarts

## Common Workflows

### Extract and browse locally (no server)

```bash
make run-databank INPUT_DIR=./l10n-files
make run-databank-desktop
# In Desktop: Load DataBank JSON → select data-bank.json
```

### Full pipeline with API

```bash
make run-databank-stack
# In another terminal:
make run-databank INPUT_DIR=./l10n-files
# Import into API:
curl -X POST http://localhost:5000/api/import -F "file=@data-bank.json"
# Browse in Desktop (Remote mode) or Swagger UI
```

### Re-import updated data

```bash
# Re-extract
make run-databank INPUT_DIR=./l10n-files
# Re-import (upserts — existing entries are updated)
curl -X POST http://localhost:5000/api/import -F "file=@data-bank.json"
```
