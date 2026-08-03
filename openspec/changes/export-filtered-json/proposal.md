## Why

Users need to export filtered localization data from the DataBank Desktop app for sharing, backup, or use in other tools. Currently, there is no export functionality — data can only be loaded, filtered, and viewed in-app. This feature enables users to extract specific locale subsets (e.g., only "en" and "zh-CN") as JSON files conforming to the data-bank.json schema.

## What Changes

- Add "Export" button to the DataBank toolbar (enabled only when filtered data exists)
- Build JSON export from `filteredEntries` using data-bank.json schema (version 3)
- Filter `values` and `sources` arrays to only include selected locales
- C# backend handles SaveFileDialog and file write
- Dynamic filename includes timestamp and selected locales (e.g., `databank-export-2026-08-03T12-30-00-en-zh-CN.json`)

## Capabilities

### New Capabilities

- `json-export`: Export filtered localization entries as JSON file conforming to data-bank.json schema, with locale filtering applied to values and sources

### Modified Capabilities

None — no existing spec-level behavior changes.

## Impact

- **Frontend (app.js)**: New `buildExportJson()` and `exportFilteredData()` functions (~40 lines)
- **UI (index.html)**: New Export button in toolbar (~5 lines)
- **Backend (MainWindow.xaml.cs)**: Handle `exportJson` WebView2 message with SaveFileDialog + file write (~30 lines)
- **No new dependencies** — uses existing WebView2 messaging and System.IO for file write
