## Why

The DataBank CLI tool successfully extracts localization data from .resx/.rc/.fhx/.ahc files and outputs data-bank.json, but there's no way to access this data programmatically. Teams need a REST API to integrate DataBank data into their workflows, dashboards, and automation pipelines without manually parsing JSON files. This separates the API service from the CLI tool, enabling independent scaling and deployment.

## What Changes

- **New ASP.NET Core Web API project** at `DatabankTool/DataBank.Api/`
- **CRUD endpoints** for localization entries (GET, POST, PUT, DELETE)
- **Filtering and querying** by locale, format, translation status
- **Statistics and coverage endpoints** for monitoring localization health
- **Extraction trigger endpoint** to run parsers from the API
- **Swagger/OpenAPI documentation** for API discovery
- **CORS configuration** for frontend integration
- **File-based data provider** loading from data-bank.json
- **Shared parser and model references** to avoid code duplication

## Capabilities

### New Capabilities

- `rest-api-endpoints`: CRUD operations for localization entries with filtering and pagination
- `api-documentation`: Swagger/OpenAPI integration for API discovery and testing
- `data-extraction-service`: Endpoint to trigger file parsing and data extraction
- `statistics-reporting`: Coverage summaries and localization statistics endpoints

### Modified Capabilities

None - this is entirely new functionality with no existing spec changes needed.

## Impact

**Affected Code:**
- New project directory: `DatabankTool/DataBank.Api/`
- References to existing parsers in `DatabankTool/DataBank.Cli/Parsers/`
- References to existing models in `DatabankTool/DataBank.Cli/Models/`
- New configuration for data file paths and CORS settings

**APIs:**
- New REST API endpoints (8 total)
- No changes to existing CLI tool functionality

**Dependencies:**
- New ASP.NET Core Web API project dependencies
- Swagger/OpenAPI NuGet packages
- Reference to DataBank.Cli project for shared code

**Systems:**
- Standalone service that can run alongside or separate from CLI
- Reads from same data-bank.json output format
- Can trigger extraction using existing parser logic