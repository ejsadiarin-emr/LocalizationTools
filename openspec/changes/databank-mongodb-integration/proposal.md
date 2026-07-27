## Why

DataBank currently stores all localized string entries in a single `data-bank.json` file. As the localization dataset grows (16K+ entries), this file-based approach becomes a bottleneck: no concurrent access, no query filtering, no indexed lookups, and no support for translation session tracking. A persistent document database enables efficient querying, concurrent team access, and a foundation for the translation workflow features planned next.

## What Changes

- **New `DataBank.Api` ASP.NET Core project** with REST endpoints for CRUD operations on localized string entries
- **MongoDB integration** using the official MongoDB.Driver NuGet package
- **Repository pattern**: `IDataBankRepository` interface with `MongoDataBankRepository` implementation
- **Three MongoDB collections**: `DataBankEntry` (localized strings), `DataBankMetadata` (dataset metadata), `TranslationSession` (translation tracking)
- **Initial data import** from existing `data-bank.json` into MongoDB
- **Connection string configuration** via `appsettings.json`
- **Docker Compose file** for local MongoDB instance
- **Indexes** on Key, Locale, Format, and TranslationStatus fields for query performance

## Capabilities

### New Capabilities

- `mongodb-persistence`: MongoDB connection, configuration, repository pattern, and collection management
- `databank-api`: REST API endpoints for CRUD operations on DataBank entries
- `data-import`: Initial migration tool to import existing data-bank.json into MongoDB
- `translation-sessions`: Translation session tracking and status management

### Modified Capabilities

<!-- No existing specs to modify -->

## Impact

- **New project**: `DatabankTool/DataBank.Api/` — ASP.NET Core Web API
- **New project**: `DatabankTool/DataBank.Import/` — CLI tool for initial data import
- **New file**: `docker-compose.yml` at repo root for local MongoDB
- **Modified**: `appsettings.json` in DataBank.Api for MongoDB connection string
- **Dependencies added**: `MongoDB.Driver`, `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`
- **No breaking changes** to existing DataBank.Cli — it continues to produce `data-bank.json` as before
