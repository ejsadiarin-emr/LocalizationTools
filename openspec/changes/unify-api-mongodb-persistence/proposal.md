## Why

The Databank API has two parallel, disconnected data layers: an MVC controller layer backed by an in-memory `ConcurrentDictionary` + JSON file (`FileDataBankService`), and a Minimal API endpoint layer backed by MongoDB (`MongoDataBankRepository`). Both are registered in DI and mapped to overlapping routes (5 collisions on `/api/entries`). The extraction service writes to the file layer, not MongoDB, so MongoDB collections remain empty. This means the API cannot fulfill its core purpose — serving localization data from a persistent database.

## What Changes

- **Remove** the MVC Controller layer (`EntriesController`, `ExtractController`, `StatsController`) and the `FileDataBankService` / `IExtractionService` / `IStatisticsService` interfaces and implementations that operate on the JSON file
- **Rewire** the extraction flow to write parsed entries directly to MongoDB via `IDataBankRepository`
- **Add** a `GET /api/databank/export` endpoint that returns a `DataBankOutput`-shaped JSON (matching CLI output format: `version`, `generated`, `entries`, `translationSummary`)
- **Add** translation status computation via MongoDB aggregation (replacing the in-memory `TranslationStatusAnalyzer` call)
- **Add** coverage/statistics endpoints backed by MongoDB aggregation pipelines
- **BREAKING**: Remove `POST /api/extract` (old controller route) — replaced by new extraction endpoint backed by MongoDB
- **BREAKING**: Remove `GET /api/stats` and `GET /api/stats/coverage` (old controller routes) — replaced by MongoDB-backed equivalents

## Capabilities

### New Capabilities
- `mongodb-persistence`: All CRUD operations, extraction, and data storage go through MongoDB. No file-based or in-memory stores.
- `data-export`: A `GET /api/databank/export` endpoint that returns the full `DataBankOutput` JSON structure (matching CLI output) with computed `translationSummary`.

### Modified Capabilities

_(none — no existing specs)_

## Impact

- **Files removed**: `Controllers/EntriesController.cs`, `Controllers/ExtractController.cs`, `Controllers/StatsController.cs`, `Services/FileDataBankService.cs`, `Services/IDataBankService.cs`, `Services/ExtractionService.cs`, `Services/IExtractionService.cs`, `Services/StatisticsService.cs`, `Services/IStatisticsService.cs`
- **Files modified**: `Program.cs` (DI cleanup), `Repositories/MongoDataBankRepository.cs` (add extraction + aggregation methods), `Endpoints/EntriesEndpoints.cs` (add export + stats endpoints)
- **Files added**: `Endpoints/ExportEndpoint.cs`, `Endpoints/StatsEndpoints.cs`
- **API routes**: All `/api/entries` routes remain (now MongoDB-backed). `/api/stats/*` routes change implementation. New `/api/databank/export` route added.
- **Dependencies**: No new NuGet packages. MongoDB driver already in use.
