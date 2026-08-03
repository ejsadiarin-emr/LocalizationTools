## ADDED Requirements

### Requirement: Desktop displays line numbers in source info
The Desktop detail panel SHALL display the line number for each source entry alongside the file path.

#### Scenario: Line number shown in detail panel
- **WHEN** a user clicks an entry in the table to open the detail panel
- **THEN** the Sources section SHALL show the line number for each locale source (e.g., "Line: 42") or omit the line if null

### Requirement: Desktop provides "Open Source File" action
The Desktop detail panel SHALL include an "Open Source File" button for each source entry that has a file path.

#### Scenario: Button visible for entries with source files
- **WHEN** the detail panel displays an entry with at least one source that has a non-null file path
- **THEN** an "Open Source File" button SHALL be visible next to each source file path

#### Scenario: Button not shown when no source files
- **WHEN** the detail panel displays an entry with no sources
- **THEN** no "Open Source File" button SHALL be shown

### Requirement: Desktop opens source file at line number
When the user clicks "Open Source File", the system SHALL open the source file. If a line number is available, the system SHOULD attempt to open at that line using VS Code's `code -g` if available, otherwise fall back to the default application.

#### Scenario: Open file with line number via VS Code
- **WHEN** the user clicks "Open Source File" for a source with `line = 42` and `file = "FHX\\EN\\AlarmWords.txt"`
- **THEN** the C# handler SHALL attempt to detect VS Code by checking local install path or `where code`
- **AND** if VS Code is found, SHALL open via `code -g "filePath:line"` 
- **AND** the status bar SHALL show "Opened {filename} at line {line} in VS Code"

#### Scenario: Open file with line number fallback
- **WHEN** the user clicks "Open Source File" for a source with `line = 42` and VS Code is not detected
- **THEN** the system SHALL open the file using `System.Diagnostics.Process.Start` with `UseShellExecute = true`
- **AND** the status bar SHALL show "Opened {filename} (line {line})"

#### Scenario: Open file without line number
- **WHEN** the user clicks "Open Source File" for a source with `line = null`
- **THEN** the system SHALL open the file without attempting to navigate to a specific line

### Requirement: WebView2 message protocol for file opening
The Desktop app SHALL support a new `openSourceFile` message from the WebView2 frontend to the C# backend.

#### Scenario: JS sends openSourceFile message
- **WHEN** the user clicks the "Open Source File" button in the HTML frontend
- **THEN** the frontend SHALL call `window.chrome.webview.postMessage({ action: "openSourceFile", filePath: "...", line: <number|null> })`

#### Scenario: C# handles openSourceFile message
- **WHEN** the C# `CoreWebView2_WebMessageReceived` handler receives an `openSourceFile` action
- **THEN** it SHALL extract `filePath` and optional `line` from the message
- **AND** if `line` is present, it SHALL first attempt VS Code via `code -g "filePath:line"`
- **AND** if VS Code is unavailable, it SHALL fall back to `System.Diagnostics.Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true })`
