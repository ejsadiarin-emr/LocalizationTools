# DataBank API

REST API for accessing and managing localization data. Backed by MongoDB. The DataBank
CLI extracts `data-bank.json` from resource files; this API imports that data and serves
it to the desktop app and Swagger UI.

## Getting Started

Start MongoDB, then the API:

```bash
# From the repo root (Makefile):
make run-databank-stack          # starts MongoDB (docker compose) + the API

# Raw commands:
docker compose -f DatabankTool/docker-compose.yml up -d mongodb
dotnet run --project DatabankTool/DataBank.Api/DataBank.Api.csproj -c Release
```

The API listens at `http://localhost:5000`. MongoDB runs on `localhost:27017`
(database `databank`).

Swagger UI is available at `http://localhost:5000/swagger` in development mode.

## Importing Data

```bash
make run-databank INPUT_DIR=./l10n-files        # produce data-bank.json
curl -X POST http://localhost:5000/api/import -F "file=@data-bank.json"
# (or use `make import-data`)
```

Import is an upsert: existing entries are replaced, new entries are inserted.

## Endpoints

### Entries (`/api/entries`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/entries` | List entries; filters: `locale`, `format` (`resx`/`rc`/`fhx`/`ahc`/`json`/`grf`), `key` |
| GET | `/api/entries/count` | Entry count (optional `locale` filter) |
| GET | `/api/entries/by-key/{key}` | Get a single entry by key |
| POST | `/api/entries` | Create a new entry (fails if the key already exists; `Id` = `Key`) |
| PUT | `/api/entries/{key}` | Update an entry |
| PUT | `/api/entries/{key}/locales/{locale}` | Update a single locale value - body: `{ "value": "..." }` |
| PATCH | `/api/entries/{key}/values` | Bulk-update locale values |
| DELETE | `/api/entries/{key}` | Delete an entry |

### Import / Export

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/import` | Import a `data-bank.json` file (multipart `file` field, upsert) |
| GET | `/api/databank/export` | Export all data in `data-bank.json` shape (`version`, `entries[...]`, `translationSummary`) |

### Extraction

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/extract` | Trigger extraction from source files |
| GET | `/api/extract/{jobId}` | Check extraction job status |

### Statistics

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/stats` | Get comprehensive statistics |
| GET | `/api/stats/coverage` | Get coverage summary |

### Other

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check (status, entry count, API version) |
| GET | `/api/sessions` | Translation session tracking |
| GET | `/api/metadata` | Dataset metadata |

## Example Usage

```bash
# Get all entries
curl http://localhost:5000/api/entries

# Filter by locale / format / key
curl "http://localhost:5000/api/entries?locale=zh-CN&format=rc"

# Count entries
curl "http://localhost:5000/api/entries/count"

# Get a single entry
curl http://localhost:5000/api/entries/by-key/IDS_WELCOME

# Update one locale value
curl -X PUT http://localhost:5000/api/entries/IDS_WELCOME/locales/zh-CN \
  -H "Content-Type: application/json" -d '{"value": "欢迎"}'

# Import data-bank.json (upsert)
curl -X POST http://localhost:5000/api/import -F "file=@data-bank.json"

# Export all data
curl http://localhost:5000/api/databank/export
```

## Configuration

Edit `DatabankTool/DataBank.Api/appsettings.json`:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "databank"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:4200"]
  }
}
```

## Requirements

- .NET 10 SDK
- MongoDB (start it with `docker compose -f DatabankTool/docker-compose.yml up -d mongodb`, or use an existing instance)