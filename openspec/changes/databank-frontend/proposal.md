## Why

The DataBank CLI tool extracts localization data and the API provides programmatic access, but there's no visual interface for teams to monitor translation coverage, identify issues, and manage localization entries. Teams need a desktop application to quickly assess project health, filter entries, and locate untranslated or problematic strings without writing code or manually inspecting JSON files.

A WPF + WebView2 desktop application follows the established pattern in `src/LocalizationAnalyzers.Desktop/` — a thin WPF shell hosting a web frontend that communicates with the DataBank API. This provides native desktop distribution (single .exe) while leveraging web technologies for the UI layer.

## What Changes

- **New WPF + WebView2 desktop application** at `DatabankTool/DataBank.Desktop/`
- **WPF shell** with WebView2 control loading local web assets from `wwwroot/`
- **Web frontend** (vanilla JS or React) in `wwwroot/` served by WebView2
- **Dashboard view** with translation coverage pie chart and per-locale progress bars
- **Table view** displaying entries with columns: Key, Source (EN), Translation, Locale, Format, Status
- **Filtering** by locale, format (resx/rc/fhx/ahc), and translation status (client-side)
- **Search functionality** by key or source string (client-side)
- **Similar strings detection** using Fuse.js fuzzy matching (client-side)
- **Entry detail view** showing full metadata
- **Color-coded status indicators**: green=translated, red=untranslated, yellow=needs review, gray=do not translate
- **REST API integration** with DataBank API endpoints
- **API base URL configurable** via settings or WPF code-behind

## Capabilities

### New Capabilities

- `dashboard-views`: Translation coverage overview with Chart.js pie chart and per-locale progress bars
- `entry-table`: Searchable, filterable table displaying localization entries with status indicators
- `entry-detail`: Detailed view for individual entries showing all metadata
- `string-similarity`: Fuzzy matching detection for similar source strings using Fuse.js
- `api-integration`: Web frontend client for DataBank REST API with error handling and state management

### Modified Capabilities

None - this is entirely new functionality with no existing spec changes needed.

## Impact

**Affected Code:**
- New project directory: `DatabankTool/DataBank.Desktop/`
- WPF project with WebView2 control (mirrors `src/LocalizationAnalyzers.Desktop/` pattern)
- `wwwroot/` containing static web assets (HTML, CSS, JS) copied to output
- Integration with DataBank API endpoints from `databank-web-service-api` change

**APIs:**
- Consumes REST API endpoints defined in `databank-web-service-api` change
- No API changes required

**Dependencies:**
- WPF, .NET 10.0-windows, Microsoft.Web.WebView2 NuGet package
- Chart.js for dashboard visualizations (in web layer)
- Fuse.js for similar strings detection (in web layer)
- Optional: React 18+ if using React in wwwroot (otherwise vanilla JS)

**Systems:**
- Standalone desktop application distributed as .exe
- Connects to DataBank API service (separate deployment or local)
- Web frontend loaded locally via WebView2 (no IIS/Kestrel needed)
- No backend code changes required
