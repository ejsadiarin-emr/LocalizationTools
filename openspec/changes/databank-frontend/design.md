## Context

The DataBank system has a CLI tool for extracting localization data and a REST API for programmatic access. Teams need a visual interface to monitor translation coverage, identify issues, and manage entries without writing code. Following the established pattern in `src/LocalizationAnalyzers.Desktop/`, we will build a WPF + WebView2 desktop application that hosts a web frontend which calls the DataBank API.

**Current State:**
- CLI tool extracts data from .resx/.rc/.fhx/.ahc files to data-bank.json
- REST API provides CRUD operations and statistics (from databank-web-service-api change)
- `src/LocalizationAnalyzers.Desktop/` demonstrates the WPF + WebView2 pattern successfully
- No DataBank visual interface exists

**Reference Pattern (`src/LocalizationAnalyzers.Desktop/`):**
- `.csproj`: `<UseWPF>true</UseWPF>`, `net10.0-windows`, `Microsoft.Web.WebView2` package
- `MainWindow.xaml`: `<wv2:WebView2 x:Name="WebView" />` in a WPF window
- `MainWindow.xaml.cs`: Initializes WebView2, navigates to `wwwroot/index.html`, handles `WebMessageReceived`
- `wwwroot/`: Static HTML/CSS/JS loaded by WebView2 via `file:///` protocol
- `app.js`: Uses `window.chrome.webview.postMessage()` to communicate with C# code-behind
- C# code-behind does heavy lifting (file I/O, analysis), posts results back to web layer

**Constraints:**
- Must follow the same WPF + WebView2 pattern as the reference Desktop app
- Must integrate with existing DataBank API endpoints
- Web frontend in `wwwroot/` calls API directly via `fetch()` (no C# proxy needed)
- All filtering, search, and fuzzy matching happen client-side in the web layer
- Charts rendered in web layer using Chart.js (canvas-based)

**Stakeholders:**
- Development teams monitoring translation progress
- QA teams verifying translation quality
- Project managers tracking localization health
- Localization specialists managing entries

## Goals / Non-Goals

**Goals:**
- WPF desktop app at `DatabankTool/DataBank.Desktop/` with WebView2
- Web frontend in `wwwroot/` with HTML/CSS/JS (vanilla or React)
- Dashboard with translation coverage visualization (Chart.js pie chart, progress bars)
- Browse and filter localization entries in a table view
- Search by key or source string (client-side)
- Detect similar strings via Fuse.js fuzzy matching (client-side)
- Detailed entry metadata view
- Color coding for translation status
- Integrate with DataBank API
- Configurable API base URL

**Non-Goals:**
- User authentication/authorization (can add later)
- Editing translations directly in the UI (view-only initially)
- Real-time updates or WebSocket connections
- Mobile-native application
- Server-side rendering or SSR
- File upload or extraction triggering (use API directly)

## Decisions

**1. Project Structure: WPF + WebView2 Desktop App**
- **Decision**: Create new `DatabankTool/DataBank.Desktop/` following the existing `src/LocalizationAnalyzers.Desktop/` pattern
- **Rationale**: Consistent architecture across desktop tools, proven pattern, native desktop distribution
- **File structure:**
  ```
  DatabankTool/DataBank.Desktop/
  ├── DataBank.Desktop.csproj
  ├── App.xaml / App.xaml.cs
  ├── MainWindow.xaml / MainWindow.xaml.cs
  └── wwwroot/
      ├── index.html
      ├── styles.css
      ├── app.js
      └── (optional React build output)
  ```
- **Alternatives Considered**:
  - Standalone React/Vite app: No native desktop integration, requires separate hosting
  - WinForms + WebView2: WPF is the established pattern in this repo
  - Electron: Heavier runtime, not aligned with existing .NET ecosystem

**2. Web Frontend: Vanilla JS (with optional React)**
- **Decision**: Use vanilla JS in `wwwroot/` with Chart.js and Fuse.js loaded via CDN or bundled
- **Rationale**: No build step required, simplest integration, matches reference pattern
- **Alternatives Considered**:
  - React in wwwroot: Possible but adds build complexity (Vite/webpack), use if component model needed
  - TypeScript: Adds compilation step, optional enhancement
  - Svelte/Vue: Additional tooling, no advantage over vanilla for this scope

**3. Communication: WebView2 PostMessage + fetch()**
- **Decision**: Web frontend calls DataBank API directly via `fetch()`. C# code-behind only handles WebView2 initialization and configuration. If native features needed (file dialogs, settings), use `WebMessageReceived`.
- **Rationale**: API is HTTP-based, web layer can call it directly without C# proxy
- **Pattern from reference**: The reference app uses `postMessage` for C# operations (folder browse, analysis). Here, the web layer calls the API directly for data operations.

**4. Charts: Chart.js**
- **Decision**: Use Chart.js for pie chart and progress bar visualizations
- **Rationale**: Canvas-based, lightweight, no framework dependency, excellent for simple charts
- **Alternatives Considered**:
  - Recharts: React-only, unnecessary if using vanilla JS
  - D3.js: Too low-level for simple pie chart and progress bars
  - CSS-only progress bars: Possible for bars, but need Chart.js for pie chart

**5. Fuzzy Search: Fuse.js**
- **Decision**: Use Fuse.js for similar string detection in the web layer
- **Rationale**: Lightweight, client-side, no server dependencies, good performance
- **Alternatives Considered**:
  - Server-side search: Adds API complexity, latency
  - Lunr.js: More complex, full-text search focus
  - Custom Levenshtein: Reinventing the wheel

**6. API Base URL Configuration**
- **Decision**: API base URL configurable via a config object in `app.js` or passed from C# code-behind via WebMessageReceived
- **Rationale**: Allows pointing to different API instances without rebuilding
- **Default**: `http://localhost:5000` (typical ASP.NET dev server)

**7. State Management: Vanilla JS variables**
- **Decision**: Use simple JS variables and DOM manipulation for state
- **Rationale**: No framework overhead, sufficient for view-only dashboard
- **Alternatives Considered**:
  - React state/context: Only if using React
  - Redux/MobX: Overkill for this scope

## Risks / Trade-offs

**[Risk] WebView2 Runtime Availability** →
- Mitigation: WebView2 Evergreen Runtime is pre-installed on Windows 10/11. Installer can bundle it.
- Trade-off: Requires Windows, but that's the target platform

**[Risk] API Connectivity** →
- Mitigation: Clear error states, retry options, configurable API URL
- Trade-off: App requires network connection to API service

**[Risk] Performance with Large Datasets** →
- Mitigation: Client-side pagination, lazy loading, Fuse.js debounced search
- Trade-off: Initial fetch may be slow for very large datasets

**[Risk] CORS if API and WebView2 origin differ** →
- Mitigation: WebView2 loads from `file:///` protocol. API must allow CORS or be configured appropriately. Alternatively, use C# code-behind as proxy if CORS is an issue.
- Trade-off: May need API CORS configuration or C# proxy pattern

**[Trade-off] No Offline Support** →
- Chose online-only for simplicity
- Acceptable since API requires network connection

**[Trade-off] View-Only Interface** →
- Chose to start with read-only for faster delivery
- Can add editing capabilities later via PUT endpoints

**[Trade-off] Vanilla JS over React** →
- Chose vanilla JS for simplicity and zero build step
- Can migrate to React in wwwroot later if component complexity grows
