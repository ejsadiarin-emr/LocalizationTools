## 1. Project Setup

- [x] 1.1 Create `DatabankTool/DataBank.Api/` ASP.NET Core Web API project with `dotnet new webapi`
- [x] 1.2 Add NuGet packages: `MongoDB.Driver`, `Swashbuckle.AspNetCore`
- [x] 1.3 Create `docker-compose.yml` at repo root with MongoDB 6.0 container and volume mount
- [x] 1.4 Add `appsettings.json` with `MongoDb:ConnectionString` and `MongoDb:DatabaseName` configuration
- [x] 1.5 Configure Program.cs to read MongoDB config and register services

## 2. Data Models

- [x] 2.1 Create `Models/DataBankEntryDocument.cs` with MongoDB BSON attributes mapping to DataBankEntry schema
- [x] 2.2 Create `Models/DataBankMetadataDocument.cs` with MongoDB BSON attributes for metadata collection
- [x] 2.3 Create `Models/TranslationSessionDocument.cs` with ObjectId `_id`, Status, EntryIds array
- [x] 2.4 Create `Models/TranslationSessionStatus.cs` enum with values: Pending, InProgress, Completed

## 3. Repository Interface

- [x] 3.1 Create `Repositories/IDataBankRepository.cs` with async methods: GetAllAsync, GetByIdAsync, GetByKeyAsync, GetByLocaleAsync, CreateAsync, UpdateAsync, DeleteAsync
- [x] 3.2 Add metadata methods to interface: GetMetadataAsync, UpdateMetadataAsync
- [x] 3.3 Add session methods to interface: GetAllSessionsAsync, GetSessionByIdAsync, CreateSessionAsync, UpdateSessionStatusAsync, AddEntriesToSessionAsync, DeleteSessionAsync

## 4. MongoDB Repository Implementation

- [x] 4.1 Create `Repositories/MongoDataBankRepository.cs` implementing IDataBankRepository
- [x] 4.2 Implement DataBankEntry CRUD methods using IMongoCollection<DataBankEntryDocument>
- [x] 4.3 Implement GetByKeyAsync and GetByLocaleAsync query methods
- [x] 4.4 Implement metadata methods using IMongoCollection<DataBankMetadataDocument>
- [x] 4.5 Implement TranslationSession methods using IMongoCollection<TranslationSessionDocument>
- [x] 4.6 Add index creation logic: unique index on Key, indexes on Locale, Format, DoNotTranslate, Status
- [x] 4.7 Register MongoDataBankRepository in DI container in Program.cs

## 5. API Endpoints

- [x] 5.1 Create `Endpoints/EntriesEndpoints.cs` with GET /api/entries (with locale, format, key query params)
- [x] 5.2 Add GET /api/entries/{id} endpoint with 404 handling
- [x] 5.3 Add POST /api/entries endpoint with 409 conflict handling for duplicate keys
- [x] 5.4 Add PUT /api/entries/{id} endpoint with 404 handling
- [x] 5.5 Add DELETE /api/entries/{id} endpoint with 204 response
- [x] 5.6 Add GET /api/entries/count endpoint with optional locale filter
- [x] 5.7 Create `Endpoints/MetadataEndpoints.cs` with GET /api/metadata
- [x] 5.8 Create `Endpoints/SessionsEndpoints.cs` with GET /api/sessions (with status filter)
- [x] 5.9 Add GET /api/sessions/{id} endpoint
- [x] 5.10 Add POST /api/sessions endpoint with default status="pending" and timestamp initialization
- [x] 5.11 Add PUT /api/sessions/{id}/status endpoint with transition validation (pending→in-progress→completed)
- [x] 5.12 Add POST /api/sessions/{id}/entries endpoint for adding entry IDs
- [x] 5.13 Add DELETE /api/sessions/{id} endpoint
- [x] 5.14 Map all endpoint groups in Program.cs

## 6. Swagger Configuration

- [x] 6.1 Configure Swagger/OpenAPI in Program.cs with endpoint descriptions
- [x] 6.2 Verify Swagger UI accessible at /swagger

## 7. Import Tool

- [x] 7.1 Create `DatabankTool/DataBank.Import/` console application project
- [x] 7.2 Add NuGet packages: `MongoDB.Driver`
- [x] 7.3 Add `appsettings.json` with MongoDB connection configuration
- [x] 7.4 Implement JSON file reading from --input argument or default path
- [x] 7.5 Implement MongoDB connection with --connection-string override
- [x] 7.6 Implement batch insert (1000 entries per batch) with upsert for idempotency
- [x] 7.7 Implement metadata import (version, timestamp, entry count)
- [x] 7.8 Add progress reporting during import (e.g., "Importing entries: 1000/5000")
- [x] 7.9 Add import summary output (total entries, duration, errors)

## 8. Verification

- [x] 8.1 Start Docker Compose MongoDB and verify container is healthy
- [x] 8.2 Build and run DataBank.Api, verify Swagger UI loads
- [x] 8.3 Test GET /api/entries returns empty array on fresh database
- [x] 8.4 Run DataBank.Import with existing data-bank.json, verify entries are inserted
- [x] 8.5 Test GET /api/entries returns imported entries
- [x] 8.6 Test CRUD operations (create, read, update, delete) via API
- [x] 8.7 Test translation session lifecycle (create, start, add entries, complete)
- [x] 8.8 Verify indexes exist by querying MongoDB directly
