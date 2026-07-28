## Context

The Databank API currently has two disconnected persistence layers:

1. **MVC Controller layer** → `FileDataBankService` → `data-bank.json` file (in-memory `ConcurrentDictionary`)
2. **Minimal API Endpoint layer** → `MongoDataBankRepository` → MongoDB

Both are registered in DI and mapped to overlapping routes. The extraction service writes to the file layer. MongoDB collections exist but are empty. The goal is to unify on MongoDB as the single source of truth.

**Current state**: `Program.cs` registers both layers. `ExtractionService` calls `_dataBankService.AddEntries()` (file). `MongoDataBankRepository` has full CRUD but nothing calls it for writes.

**Stakeholders**: API consumers (UI apps, other services) who expect a REST API backed by a persistent database.

## Goals / Non-Goals

**Goals:**
- Single persistence layer: MongoDB via `IDataBankRepository`
- Extraction writes to MongoDB, not file
- Full parity with CLI output format via export endpoint
- Translation status and coverage computed from MongoDB aggregations
- Remove all file-based and in-memory stores

**Non-Goals:**
- Changing the CLI tool itself
- Changing the MongoDB document schema (it already mirrors CLI models)
- Adding authentication/authorization
- Migrating existing `data-bank.json` data into MongoDB (user can re-extract)

## Decisions

### Decision 1: Remove MVC Controllers entirely

**Choice**: Delete `EntriesController`, `ExtractController`, `StatsController` and their service interfaces.

**Rationale**: The Minimal API endpoints already cover all CRUD operations on MongoDB. The MVC controllers operate on a file store that should no longer exist. Keeping both creates route collisions and confusion.

**Alternative considered**: Keep MVC controllers and rewire them to MongoDB. Rejected because Minimal API endpoints are already MongoDB-native and more idiomatic for .NET 8+.

### Decision 2: Rewrite ExtractionService to use IDataBankRepository

**Choice**: Modify `ExtractionService` to accept `IDataBankRepository` instead of `IDataBankService`. Parsed entries get inserted into MongoDB directly.

**Rationale**: The extraction flow is the primary write path. It must go to MongoDB.

**Implementation**:
- `ExtractionService` constructor takes `IDataBankRepository` instead of `IDataBankService`
- After parsing, calls `repository.CreateEntryAsync()` for each entry (or batch inserts)
- Updates `DataBankMetadataDocument` with version and generated timestamp
- Extraction endpoint moves to Minimal API (`Endpoints/ExtractionEndpoints.cs`)

### Decision 3: Add export endpoint for CLI parity

**Choice**: `GET /api/databank/export` returns `DataBankOutput`-shaped JSON.

**Rationale**: API consumers need the same structure as `data-bank.json`. The `translationSummary` is computed via MongoDB aggregation (not stored), avoiding staleness.

**Response shape**:
```json
{
  "version": 2,
  "generated": "2026-07-25T10:00:00Z",
  "entries": [...],
  "translationSummary": {
    "totalKeys": 100,
    "translatedKeys": 80,
    "untranslatedKeys": 15,
    "doNotTranslateKeys": 5,
    "needsReviewKeys": 0,
    "completionPercentage": 80.0
  }
}
```

### Decision 4: Use MongoDB aggregation for statistics

**Choice**: Compute statistics and coverage via MongoDB aggregation pipelines rather than loading all entries into memory.

**Rationale**: Scalability. The current `StatisticsService` loads all entries into memory and computes in C#. MongoDB aggregation is more efficient for large datasets.

### Decision 5: Keep TranslationSession feature as-is

**Choice**: The `SessionsEndpoints` and `TranslationSessionDocument` remain unchanged.

**Rationale**: This feature is already MongoDB-native and doesn't conflict with the cleanup.

## Risks / Trade-offs

- **[Data loss on deployment]** → No migration needed. Users re-extract from source files. The `data-bank.json` file is a derived artifact, not primary data.
- **[Route changes break API consumers]** → Minimize by keeping `/api/entries` routes identical. Only `/api/extract` and `/api/stats` change paths.
- **[Extraction performance]** → Batch MongoDB inserts (bulk write) instead of one-by-one. Use `InsertManyAsync`.
- **[Metadata consistency]** → After extraction, update `DataBankMetadataDocument` with entry count and timestamp in same transaction.

## Migration Plan

1. Deploy new API version ( MongoDB must be running)
2. Run extraction via `POST /api/extract` (new endpoint) to populate MongoDB
3. Old `data-bank.json` file is no longer used
4. Rollback: restore old `Program.cs` and re-deploy previous version

## Open Questions

- Should extraction support `.grf` and `.json` formats (matching CLI)? Currently API only supports `.resx`, `.rc`, `.fhx`, `.ahc`.
- Should `--locale` and `--encoding` override parameters be exposed in the extraction API endpoint?
