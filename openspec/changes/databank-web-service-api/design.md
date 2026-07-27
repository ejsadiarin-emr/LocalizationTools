## Context

The DataBank CLI tool currently extracts localization data from various file formats (.resx, .rc, .fhx, .ahc) and outputs a single `data-bank.json` file. While this works for batch processing, teams need programmatic access to this data for integration with CI/CD pipelines, dashboards, and automation workflows.

**Current State:**
- CLI tool at `DatabankTool/DataBank.Cli/` with parsers for 4 file formats
- Shared models: `LocalizedStringEntry`, `SourceInfo`, `EntryMetadata`, `DataBankOutput`, `CoverageReport`
- Output: `data-bank.json` containing all extracted entries
- No web service or API exists

**Constraints:**
- Must reuse existing parsers to avoid code duplication
- Must be separate from CLI tool (independent deployment)
- Must support frontend integration via CORS
- Should work with existing data-bank.json format

**Stakeholders:**
- Development teams needing API access to localization data
- QA teams monitoring coverage metrics
- Automation pipelines triggering extraction
- Frontend dashboards displaying statistics

## Goals / Non-Goals

**Goals:**
- Provide RESTful CRUD operations for localization entries
- Enable filtering and querying by locale, format, and translation status
- Offer statistics and coverage reporting endpoints
- Support triggering extraction from the API
- Document API with Swagger/OpenAPI
- Enable CORS for frontend integration
- Load data from existing data-bank.json files

**Non-Goals:**
- Real-time file watching or automatic extraction
- User authentication/authorization (can be added later)
- Complex query language or GraphQL
- Database persistence (file-based for now)
- Modifying CLI tool behavior
- Supporting multiple data sources simultaneously

## Decisions

**1. Project Structure: Separate ASP.NET Core Web API**
- **Decision**: Create new `DataBank.Api` project in `DatabankTool/` directory
- **Rationale**: Clean separation of concerns, independent deployment, clear API boundaries
- **Alternatives Considered**:
  - Adding API to CLI project: Would mix concerns, harder to deploy separately
  - Shared library approach: More complex for what's needed now

**2. Data Provider: In-Memory File-Based**
- **Decision**: Load entire data-bank.json into memory at startup, provide through service
- **Rationale**: Simple implementation, fast queries, sufficient for typical data sizes
- **Alternatives Considered**:
  - Direct file reads: Too slow for repeated queries
  - SQLite/database: Overkill, adds complexity without clear benefit for this use case
  - Streaming: Not needed for query patterns

**3. Parser Reuse: Project Reference**
- **Decision**: Reference DataBank.Cli project to share parsers and models
- **Rationale**: Avoids code duplication, maintains single source of truth
- **Alternatives Considered**:
  - Copy parsers: Would create maintenance burden, divergence risk
  - Shared library extraction: More refactoring than needed now

**4. API Design: RESTful with Standard Conventions**
- **Decision**: Follow REST conventions with standard HTTP methods and status codes
- **Rationale**: Familiar to developers, works with standard tools
- **Alternatives Considered**:
  - RPC-style: Less intuitive, non-standard
  - GraphQL: More complex, overkill for this use case

**5. Documentation: Swagger/OpenAPI**
- **Decision**: Use Swashbuckle for automatic Swagger generation
- **Rationale**: Industry standard, automatic documentation, testing UI
- **Alternatives Considered**:
  - Manual documentation: Harder to maintain, gets out of sync
  - Postman collections: Less integrated, requires separate tool

## Risks / Trade-offs

**[Risk] Memory Usage with Large Files** → 
- Mitigation: Implement pagination for list endpoints, lazy loading if needed
- Trade-off: Acceptable for typical data sizes, can optimize later

**[Risk] Parser Compatibility Changes** → 
- Mitigation: Pin to specific parser version, add integration tests
- Trade-off: Tight coupling, but avoids duplication

**[Risk] No Authentication** → 
- Mitigation: Document as future enhancement, can add middleware later
- Trade-off: Simplicity now, security can be layered on

**[Risk] Single File Source of Truth** → 
- Mitigation: Validate file exists on startup, clear error messages
- Trade-off: Simplicity vs. flexibility, acceptable for current use

**[Trade-off] In-Memory vs. Database** →
- Chose in-memory for simplicity and speed
- Acceptable for current scale, can migrate later if needed