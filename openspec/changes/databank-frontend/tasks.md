## 1. WPF Project Setup

- [ ] 1.1 Create `DatabankTool/DataBank.Desktop/` directory structure
- [ ] 1.2 Create `DataBank.Desktop.csproj` with `UseWPF`, `net10.0-windows`, `Microsoft.Web.WebView2` package reference
- [ ] 1.3 Create `App.xaml` and `App.xaml.cs` (minimal Application class with StartupUri)
- [ ] 1.4 Create `MainWindow.xaml` with WebView2 control in a Grid layout
- [ ] 1.5 Create `MainWindow.xaml.cs` with WebView2 initialization (EnsureCoreWebView2Async, Navigate to wwwroot/index.html)
- [ ] 1.6 Add `wwwroot/` folder with `Content` item in csproj for CopyToOutputDirectory

## 2. Web Frontend Scaffolding

- [ ] 2.1 Create `wwwroot/index.html` with HTML structure (header, nav, main sections for dashboard, table, detail)
- [ ] 2.2 Create `wwwroot/styles.css` with base CSS variables for colors, spacing, typography
- [ ] 2.3 Create `wwwroot/app.js` with app initialization, API configuration, and view routing logic
- [ ] 2.4 Add Chart.js script tag (CDN or local) in index.html
- [ ] 2.5 Add Fuse.js script tag (CDN or local) in index.html
- [ ] 2.6 Implement simple client-side view switching (hash-based routing: #dashboard, #table, #detail)

## 3. API Integration Layer

- [ ] 3.1 Define `apiBaseUrl` constant in app.js (default `http://localhost:5000`)
- [ ] 3.2 Create `fetchEntries()` function that calls `GET /api/entries` and returns JSON array
- [ ] 3.3 Create `fetchEntryById(id)` function that calls `GET /api/entries/{id}`
- [ ] 3.4 Create `fetchWithRetry(url, options, retries)` helper with exponential backoff
- [ ] 3.5 Create in-memory entries cache (`let allEntries = []`)
- [ ] 3.6 Create loading state management (show/hide spinner during fetch)
- [ ] 3.7 Create error state management (display error messages with retry button)
- [ ] 3.8 Test API connectivity on app startup with connection check

## 4. Dashboard View Implementation

- [ ] 4.1 Create dashboard HTML section with summary cards (total entries, translated %, untranslated count, needs review count)
- [ ] 4.2 Create `computeStats(entries)` function to calculate coverage statistics from entries array
- [ ] 4.3 Implement Chart.js pie chart for status distribution (translated, untranslated, needs review, do not translate)
- [ ] 4.4 Create per-locale progress bars HTML structure with dynamic rendering
- [ ] 4.5 Implement `computeLocaleStats(entries)` function grouping entries by locale and computing completion %
- [ ] 4.6 Apply progress bar color coding: green >80%, yellow 50-80%, red <50%
- [ ] 4.7 Add refresh button that re-fetches from API and re-renders dashboard
- [ ] 4.8 Implement empty state message when no entries available

## 5. Table View Implementation

- [ ] 5.1 Create table HTML structure with thead/tbody for columns: Key, Source (EN), Translation, Locale, Format, Status
- [ ] 5.2 Create `renderTable(entries)` function that populates table rows from entries array
- [ ] 5.3 Implement column sorting (click header to toggle asc/desc)
- [ ] 5.4 Create locale filter dropdown populated from unique locale values in entries
- [ ] 5.5 Create format filter dropdown with options: resx, rc, fhx, ahc
- [ ] 5.6 Create status filter dropdown with options: translated, untranslated, needs review, do not translate
- [ ] 5.7 Implement combined client-side filtering (AND logic across all active filters)
- [ ] 5.8 Add search input with debounced (300ms) case-insensitive substring matching on Key and Source
- [ ] 5.9 Implement row color coding based on translation status
- [ ] 5.10 Implement client-side pagination (50 entries per page) with page controls
- [ ] 5.11 Add row click handler to navigate to entry detail view

## 6. Entry Detail View Implementation

- [ ] 6.1 Create detail view HTML section with metadata display area
- [ ] 6.2 Implement `renderDetail(entry)` function showing all entry fields
- [ ] 6.3 Add status indicator with color coding matching table row colors
- [ ] 6.4 Add Next/Previous navigation buttons that cycle through filtered entries list
- [ ] 6.5 Add "Back to Table" button that preserves filter/search/sort state
- [ ] 6.6 Create similar strings section placeholder in detail view

## 7. String Similarity Detection

- [ ] 7.1 Initialize Fuse.js instance with entries array and configuration (keys: source string, threshold: 0.7)
- [ ] 7.2 Create `findSimilarEntries(entry, entries)` function using Fuse.js search
- [ ] 7.3 Implement similarity score display (percentage) for each similar entry
- [ ] 7.4 Sort similar entries by score descending
- [ ] 7.5 Exclude current entry from similar results
- [ ] 7.6 Add click handler on similar entries to navigate to that entry's detail view
- [ ] 7.7 Implement simple cache object for similar strings results per entry ID

## 8. Styling and Theming

- [ ] 8.1 Create CSS variables for status colors (green, red, yellow, gray)
- [ ] 8.2 Style dashboard summary cards with consistent spacing
- [ ] 8.3 Style Chart.js canvas container
- [ ] 8.4 Style progress bars with color coding and percentage labels
- [ ] 8.5 Style table with alternating row colors, hover state, sticky header
- [ ] 8.6 Style filter dropdowns and search input
- [ ] 8.7 Style entry detail view with metadata grid layout
- [ ] 8.8 Style similar strings section with similarity score badges
- [ ] 8.9 Add loading spinner and skeleton screen styles
- [ ] 8.10 Add error message and empty state styles

## 9. C# Code-Behind (WebView2 Bridge)

- [ ] 9.1 Implement WebView2 initialization in MainWindow.xaml.cs (CoreWebView2Environment, EnsureCoreWebView2Async)
- [ ] 9.2 Implement navigate to `wwwroot/index.html` via file:/// protocol
- [ ] 9.3 Add WebMessageReceived handler for configuration messages (API URL)
- [ ] 9.4 Add WebMessageReceived handler for file dialog requests (future use)
- [ ] 9.5 Post API base URL configuration to web frontend on startup

## 10. Testing and Verification

- [ ] 10.1 Verify WPF app launches and WebView2 loads index.html
- [ ] 10.2 Verify API connection works from web frontend (test with running DataBank API)
- [ ] 10.3 Verify dashboard renders pie chart and progress bars with real data
- [ ] 10.4 Verify table displays entries with correct columns and color coding
- [ ] 10.5 Verify filters (locale, format, status) work correctly with AND logic
- [ ] 10.6 Verify search finds entries by key and source string
- [ ] 10.7 Verify pagination works with >50 entries
- [ ] 10.8 Verify entry detail shows all metadata
- [ ] 10.9 Verify similar strings detection works via Fuse.js
- [ ] 10.10 Verify Next/Previous navigation in detail view
- [ ] 10.11 Verify Back to Table preserves filter state
- [ ] 10.12 Verify error handling when API is unreachable
