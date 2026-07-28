## Why

The DataBank Desktop app currently only handles JSON data files. However, GRF files (Graphical Resource Files) exist in the localization workflow under `l10n-files/GRF/` with both EN and Translated variants. Users need visibility into which GRF files are present in the project without requiring parsing of the binary GRF format.

## What Changes

- Add a new "GRF Files" tab to the DataBank Desktop app UI
- Display GRF filenames from the `l10n-files/GRF/` directory structure
- Show file organization (EN vs Translated subfolders)
- No parsing of GRF file contents - display filenames only

## Capabilities

### New Capabilities

- `grf-file-display`: Tab-based UI for listing GRF files with folder organization display

### Modified Capabilities

(none)

## Impact

- `DatabankTool/DataBank.Desktop/wwwroot/index.html` - Add tab navigation and GRF section
- `DatabankTool/DataBank.Desktop/wwwroot/styles.css` - Tab styling
- `DatabankTool/DataBank.Desktop/wwwroot/app.js` - Tab switching logic and GRF file listing
- `DatabankTool/DataBank.Desktop/MainWindow.xaml.cs` - Backend support for GRF directory scanning
