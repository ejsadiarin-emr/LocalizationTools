## 1. Frontend - Export Button

- [x] 1.1 Add Export button to toolbar in index.html (disabled by default)
- [x] 1.2 Add CSS styling for Export button (match existing button style)
- [x] 1.3 Wire button click to call `exportFilteredData()`

## 2. Frontend - Export Logic

- [x] 2.1 Implement `buildExportJson()` function to construct export object from filteredEntries
- [x] 2.2 Filter values array to selectedLocales only
- [x] 2.3 Filter sources object keys to selectedLocales only
- [x] 2.4 Generate default filename with timestamp and locales
- [x] 2.5 Implement `exportFilteredData()` to post message to C# backend

## 3. Frontend - Button State Management

- [x] 3.1 Update Export button enabled/disabled state in `applyFilters()` based on filteredEntries.length
- [x] 3.2 Update Export button state on initial data load

## 4. Backend - Export Handler

- [x] 4.1 Add `exportJson` message handler in MainWindow.xaml.cs
- [x] 4.2 Implement SaveFileDialog with default filename suggestion
- [x] 4.3 Write JSON string to selected file path
- [x] 4.4 Return success/failure status to frontend

## 5. Frontend - Notifications

- [x] 5.1 Show success notification after successful export
- [x] 5.2 Handle export failure gracefully (show error notification)
