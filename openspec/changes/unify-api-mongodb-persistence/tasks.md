## 1. Remove MVC Controller Layer

- [x] 1.1 Delete `Controllers/EntriesController.cs`
- [x] 1.2 Delete `Controllers/ExtractController.cs`
- [x] 1.3 Delete `Controllers/StatsController.cs`
- [x] 1.4 Delete `Services/IDataBankService.cs`
- [x] 1.5 Delete `Services/FileDataBankService.cs`
- [x] 1.6 Delete `Services/IExtractionService.cs`
- [x] 1.7 Delete `Services/ExtractionService.cs`
- [x] 1.8 Delete `Services/IStatisticsService.cs`
- [x] 1.9 Delete `Services/StatisticsService.cs`

## 2. Update DI Registration in Program.cs

- [x] 2.1 Remove `builder.Services.AddControllers()` and `app.MapControllers()` from `Program.cs`
- [x] 2.2 Remove singleton registrations for `IDataBankService`, `IExtractionService`, `IStatisticsService`
- [x] 2.3 Keep `IDataBankRepository` → `MongoDataBankRepository` registration

## 3. Create Extraction Minimal API Endpoint

- [x] 3.1 Create `Endpoints/ExtractionEndpoints.cs` with `POST /api/extract` and `GET /api/extract/{jobId}`
- [x] 3.2 Rewrite extraction logic to use `IDataBankRepository` instead of `IDataBankService`
- [x] 3.3 Add batch insert support (`InsertManyAsync`) to `MongoDataBankRepository`
- [x] 3.4 After extraction, update `DataBankMetadataDocument` with entry count and timestamp
- [x] 3.5 Map extraction endpoints in `Program.cs`

## 4. Create Statistics Minimal API Endpoints

- [x] 4.1 Create `Endpoints/StatsEndpoints.cs` with `GET /api/stats` and `GET /api/stats/coverage`
- [x] 4.2 Implement MongoDB aggregation pipeline for statistics (total entries, unique keys, by-locale, by-format, translation status breakdown)
- [x] 4.3 Implement MongoDB aggregation pipeline for coverage (per-locale completion percentages)
- [x] 4.4 Map stats endpoints in `Program.cs`

## 5. Create Export Endpoint

- [x] 5.1 Create `Endpoints/ExportEndpoints.cs` with `GET /api/databank/export`
- [x] 5.2 Implement `DataBankOutput` response: `version`, `generated`, `entries`, `translationSummary`
- [x] 5.3 Compute `translationSummary` from MongoDB aggregation (translated, untranslated, doNotTranslate, needsReview counts)
- [x] 5.4 Map export endpoint in `Program.cs`

## 6. Enhance MongoDataBankRepository

- [x] 6.1 Add `InsertManyAsync` method for batch entry insertion
- [x] 6.2 Add aggregation method for statistics computation
- [x] 6.3 Add aggregation method for coverage computation
- [x] 6.4 Add method to get all entries as `List<DataBankEntryDocument>` (for export)

## 7. Verify and Test

- [x] 7.1 Verify all endpoints are registered and no route collisions exist
- [x] 7.2 Verify MongoDB indexes are created on startup
- [x] 7.3 Test CRUD operations against MongoDB
- [x] 7.4 Test extraction flow populates MongoDB
- [x] 7.5 Test export endpoint returns correct `DataBankOutput` structure
- [x] 7.6 Run `dotnet build` to verify no compilation errors
