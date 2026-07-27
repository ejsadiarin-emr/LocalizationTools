## Context

DataBank.Cli is a command-line tool that parses localization resource files (RESX, RC, FHX, AHC) and outputs a unified `data-bank.json` file containing ~16K localized string entries. Each entry has an ID, key, value, locale, source info (format, file path, encoding), and metadata (comments, format specifiers, behavioral flags).

The current file-based storage has no query capabilities, no concurrent access support, and no mechanism for tracking translation sessions. This change introduces MongoDB as a persistent document database with a new ASP.NET Core Web API layer.

**Stakeholders**: Localization engineers who need to query, filter, and manage translation entries across multiple locales and formats.

## Goals / Non-Goals

**Goals:**
- Provide a REST API for CRUD operations on DataBank entries
- Store entries in MongoDB with proper indexes for query performance
- Support translation session tracking (status, assigned translator, timestamps)
- Enable initial data import from existing data-bank.json
- Provide local development environment via Docker Compose

**Non-Goals:**
- Modifying DataBank.Cli behavior (it continues to produce JSON)
- User authentication/authorization (future work)
- Real-time synchronization between CLI and API
- Migration of the CLI tool to use MongoDB directly
- Production deployment configuration (local dev only for now)

## Decisions

### 1. MongoDB over PostgreSQL/SQLite

**Decision**: Use MongoDB as the document database.

**Rationale**: DataBank entries are naturally document-shaped (nested source info, metadata objects). MongoDB's flexible schema accommodates varying entry structures across different file formats without JOIN tables. The official C# driver is mature and well-supported.

**Alternatives considered**:
- PostgreSQL with JSONB: More complex setup, requires relational modeling for document data
- SQLite: No concurrent access, limited query capabilities for nested documents

### 2. Repository Pattern with Interface

**Decision**: Introduce `IDataBankRepository` interface with `MongoDataBankRepository` implementation.

**Rationale**: Decouples API controllers from MongoDB specifics. Enables future swapping of storage backend (e.g., for testing with in-memory store, or switching to a different database). Follows existing codebase conventions for clean separation.

### 3. Three Separate Collections

**Decision**: Use three MongoDB collections: `DataBankEntry`, `DataBankMetadata`, `TranslationSession`.

**Rationale**: Entries are the primary query target (by key, locale, format). Metadata is dataset-level (version, generation timestamp) and accessed infrequently. Translation sessions are a separate concern with different lifecycle and query patterns. Separating them avoids document bloat and enables collection-specific indexes.

### 4. ASP.NET Core Minimal APIs

**Decision**: Use Minimal APIs rather than Controller-based APIs.

**Rationale**: New project with straightforward CRUD endpoints. Minimal APIs reduce boilerplate, align with modern .NET practices, and are sufficient for the scope of this change. Can migrate to controllers if routing becomes complex.

### 5. MongoDB Docker Compose for Local Dev

**Decision**: Provide a docker-compose.yml with MongoDB container.

**Rationale**: Eliminates local MongoDB installation requirement. Ensures consistent version across developers. Uses volume mount for data persistence across restarts.

### 6. Import as Separate CLI Tool

**Decision**: Create `DataBank.Import` as a standalone console app rather than an API endpoint.

**Rationale**: One-time migration operation. Keeps the API focused on runtime operations. Simpler error handling and logging for bulk import. Can be run independently without starting the API server.

## Risks / Trade-offs

- **[Risk] Data drift between CLI JSON and MongoDB** → Mitigation: Import is one-time; CLI output remains the source of truth for parsing. Future sync mechanism can be added if needed.
- **[Risk] MongoDB version compatibility** → Mitigation: Pin MongoDB version in docker-compose.yml (6.0+). Use only stable driver features.
- **[Risk] Large import performance** → Mitigation: Batch inserts (1000 entries per batch). Progress logging. Idempotent upsert on duplicate keys.
- **[Trade-off] No auth** → Acceptable for local dev. Production deployment will require auth layer.
- **[Trade-off] Separate Import tool** → Slightly more complex setup but cleaner API surface and better separation of concerns.
