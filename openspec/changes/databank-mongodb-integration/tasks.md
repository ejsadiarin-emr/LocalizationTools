## 1. Project Setup

- [ ] 1.1 Create `DatabankTool/DataBank.Api/` ASP.NET Core Web API project with `dotnet new webapi`
- [ ] 1.2 Add NuGet packages: `MongoDB.Driver`, `Swashbuckle.AspNetCore`
- [ ] 1.3 Create `docker-compose.yml` at repo root with MongoDB 6.0 container and volume mount
- [ ] 1.4 Add `appsettings.json` with `MongoDb:ConnectionString` and `MongoDb:DatabaseName` configuration
- [ ] 1.5 Configure Program.cs to read MongoDB config and register services

## 2. Data Models

- [ ] 2.1 Create `Models/DataBankEntryDocument.cs` with MongoDB BSON attributes mapping to DataBankEntry schema
- [ ] 2.2 Create `Models/DataBankMetadataDocument.cs` with MongoDB BSON attributes for metadata collection
- [ ] 2.3 Create `Models/TranslationSessionDocument.cs` with ObjectId `_id`, Status, EntryIds array
- [ ] 2.4 Create `Models/TranslationSessionStatus.cs` enum with values: Pending, InProgress, Completed

## 3. Repository Interface

- [ ] 3.1 Create `Repositories/IDataBankRepository.cs` with async methods: GetAllAsync, GetByIdAsync, GetByKeyAsync, GetByLocaleAsync, CreateAsync, UpdateAsync, DeleteAsync
- [ ] 3.2 Add metadata methods to interface: GetMetadataAsync, UpdateMetadataAsync
- [ ] 3.3 Add session methods to interface: GetAllSessionsAsync, GetSessionByIdAsync, CreateSessionAsync, UpdateSessionStatusAsync, AddEntriesToSessionAsync, DeleteSessionAsync

## 4. MongoDB Repository Implementation

- [ ] 4.1 Create `Repositories/MongoDataBankRepository.cs` implementing IDataBankRepository
- [ ] 4.2 Implement DataBankEntry CRUD methods using IMongoCollection<DataBankEntryDocument>
- [ ] 4.3 Implement GetByKeyAsync and GetByLocaleAsync query methods
- [ ] 4.4 Implement metadata methods using IMongoCollection<DataBankMetadataDocument>
- [ ] 4.5 Implement TranslationSession methods using IMongoCollection<TranslationSessionDocument>
- [ ] 4.6 Add index creation logic: unique index on Key, indexes on Locale, Format, DoNotTranslate, Status
- [ ] 4.7 Register MongoDataBankRepository in DI container in Program.cs

## 5. API Endpoints

- [ ] 5.1 Create `Endpoints/EntriesEndpoints.cs` with GET /api/entries (with locale, format, key query params)
- [ ] 5.2 Add GET /api/entries/{id} endpoint with 404 handling
- [ ] 5.3 Add POST /api/entries endpoint with 409 conflict handling for duplicate keys
- [ ] 5.4 Add PUT /api/entries/{id} endpoint with 404 handling
- [ ] 5.5 Add DELETE /api/entries/{id} endpoint with 204 response
- [ ] 5.6 Add GET /api/entries/count endpoint with optional locale filter
- [ ] 5.7 Create `Endpoints/MetadataEndpoints.cs` with GET /api/metadata
- [ ] 5.8 Create `Endpoints/SessionsEndpoints.cs` with GET /api/sessions (with status filter)
- [ ] 5.9 Add GET /api/sessions/{id} endpoint
- [ ] 5.10 Add POST /api/sessions endpoint with default status="pending" and timestamp initialization
- [ ] 5.11 Add PUT /api/sessions/{id}/status endpoint with transition validation (pending→in-progress→completed)
- [ ] 5.12 Add POST /api/sessions/{id}/entries endpoint for adding entry IDs
- [ ] 5.13 Add DELETE /api/sessions/{id} endpoint
- [ ] 5.14 Map all endpoint groups in Program.cs

## 6. Swagger Configuration

- [ ] 6.1 Configure Swagger/OpenAPI in Program.cs with endpoint descriptions
- [ ] 6.2 Verify Swagger UI accessible at /swagger

## 7. Import Tool

- [ ] 7.1 Create `DatabankTool/DataBank.Import/` console application project
- [ ] 7.2 Add NuGet packages: `MongoDB.Driver`
- [ ] 7.3 Add `appsettings.json` with MongoDB connection configuration
- [ ] 7.4 Implement JSON file reading from --input argument or default path
- [ ] 7.5 Implement MongoDB connection with --connection-string override
- [ ] 7.6 Implement batch insert (1000 entries per batch) with upsert for idempotency
- [ ] 7.7 Implement metadata import (version, timestamp, entry count)
- [ ] 7.8 Add progress reporting during import (e.g., "Importing entries: 1000/5000")
- [ ] 7.9 Add import summary output (total entries, duration, errors)

## 8. Verification

- [ ] 8.1 Start Docker Compose MongoDB and verify container is healthy
- [ ] 8.2 Build and run DataBank.Api, verify Swagger UI loads
- [ ] 8.3 Test GET /api/entries returns empty array on fresh database
- [ ] 8.4 Run DataBank.Import with existing data-bank.json, verify entries are inserted
- [ ] 8.5 Test GET /api/entries returns imported entries
- [ ] 8.6 Test CRUD operations (create, read, update, delete) via API
- [ ] 8.7 Test translation session lifecycle (create, start, add entries, complete)
- [ ] 8.8 Verify indexes exist by querying MongoDB directly
