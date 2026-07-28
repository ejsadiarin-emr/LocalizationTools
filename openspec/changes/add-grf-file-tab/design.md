## Context

The DataBank Desktop app is a WPF application using WebView2 to display localization data. Currently it loads JSON files and shows entries in a dashboard/table view. GRF (Graphical Resource Files) exist in `l10n-files/GRF/` with EN and Translated subfolders but have no visibility in the tool.

The app uses a simple architecture: WPF backend handles file I/O, WebView2 renders HTML/CSS/JS frontend, and communication happens via WebMessageReceived.

## Goals / Non-Goals

**Goals:**
- Add tab navigation to switch between DataBank JSON view and GRF Files view
- Display GRF filenames organized by folder (EN vs Translated)
- Keep the implementation minimal and consistent with existing patterns

**Non-Goals:**
- Parse or display GRF file contents (binary format)
- Edit or modify GRF files
- Support GRF files outside the `l10n-files/GRF/` directory

## Decisions

**Decision 1: Tab-based navigation in the HTML frontend**
- Rationale: The app already uses WebView2 with HTML rendering. Adding tabs in HTML is simpler than modifying the WPF XAML and maintains the existing architecture pattern.
- Alternative considered: WPF TabControl - would require more XAML changes and break the WebView2-centric pattern.

**Decision 2: Backend scans directory and sends file list via WebMessage**
- Rationale: Follows the existing pattern where WPF handles file I/O and sends data to WebView2. The backend can use `Directory.GetFiles()` to list GRF files.
- Alternative considered: Frontend JavaScript fetch - would require CORS setup or file:// permissions, adding unnecessary complexity.

**Decision 3: Display flat list with folder indicator**
- Rationale: GRF files don't need complex data structures. A simple list showing filename and parent folder (EN/Translated) is sufficient for visibility.
- Alternative considered: Tree view - overkill for the current directory structure.

## Risks / Trade-offs

- **Risk**: Directory path hardcoded to `l10n-files/GRF/`
  - Mitigation: Could be made configurable later, but current scope assumes standard project structure

- **Trade-off**: No file content preview
  - Accepted: GRF is a proprietary binary format; parsing would require significant effort with no clear value for the stated goal
