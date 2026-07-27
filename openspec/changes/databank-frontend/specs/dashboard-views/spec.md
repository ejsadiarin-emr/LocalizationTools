## ADDED Requirements

### Requirement: Dashboard displays translation coverage overview
The system SHALL display a dashboard showing overall translation coverage statistics, rendered as a web page inside a WPF WebView2 control.

#### Scenario: Dashboard loads with coverage data
- **WHEN** user navigates to the dashboard view (default view on app launch)
- **THEN** web frontend fetches entries from DataBank API (`GET /api/entries`)
- **AND** computes total entries count and overall translation percentage client-side
- **AND** displays the statistics in summary cards

#### Scenario: Coverage pie chart visualization
- **WHEN** dashboard loads
- **THEN** web frontend renders a Chart.js pie chart showing distribution of translated, untranslated, needs review, and do not translate entries
- **AND** chart legend displays status labels with counts

### Requirement: Dashboard shows per-locale progress bars
The system SHALL display individual progress bars for each locale showing translation completion.

#### Scenario: Locale progress bars display
- **WHEN** dashboard loads
- **THEN** web frontend groups entries by locale and computes per-locale completion percentage
- **AND** renders a horizontal progress bar for each locale with completion percentage label

#### Scenario: Progress bar color coding
- **WHEN** locale progress bar renders
- **THEN** bar color reflects overall status: green for >80% translated, yellow for 50-80%, red for <50%

### Requirement: Dashboard refreshes data
The system SHALL allow users to refresh dashboard data without reloading the page.

#### Scenario: Manual refresh button
- **WHEN** user clicks refresh button on dashboard
- **THEN** web frontend re-fetches data from DataBank API and re-renders all visualizations

#### Scenario: Auto-refresh on navigation
- **WHEN** user navigates back to dashboard from another view
- **THEN** web frontend automatically refreshes data to ensure current state

### Requirement: Dashboard handles empty state
The system SHALL display a meaningful message when no data is available.

#### Scenario: No entries found
- **WHEN** DataBank API returns empty entries array
- **THEN** dashboard displays "No localization data available" message
- **AND** pie chart and progress bars are hidden
