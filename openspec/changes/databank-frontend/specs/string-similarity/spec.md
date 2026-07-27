## ADDED Requirements

### Requirement: System detects similar strings via fuzzy matching
The web frontend SHALL identify similar source strings using Fuse.js fuzzy matching in the browser.

#### Scenario: Similar strings detection runs on entry detail
- **WHEN** user opens entry detail view
- **THEN** web frontend runs Fuse.js search against all loaded entries using the current entry's Source (EN) as query
- **AND** returns entries with source strings similar to current entry's source string

#### Scenario: Similarity threshold is configurable
- **WHEN** web frontend performs fuzzy matching
- **THEN** it uses a minimum similarity threshold (default 70%) to filter results
- **AND** threshold can be configured via a constant in app.js

### Requirement: Similar strings display with similarity score
The web frontend SHALL display similar strings with their similarity percentage.

#### Scenario: Similar strings list shows scores
- **WHEN** similar strings are detected
- **THEN** system displays each similar string with similarity percentage (e.g., "85% similar")

#### Scenario: Similar strings are sorted by similarity
- **WHEN** similar strings are displayed
- **THEN** system sorts them by similarity score descending (most similar first)

### Requirement: Similar strings are cross-locale
The web frontend SHALL find similar strings across all locales, not just the same locale.

#### Scenario: Cross-locale similarity detection
- **WHEN** system finds similar strings for entry in locale "en-US"
- **THEN** it includes similar strings from all other locales in the loaded dataset

### Requirement: Similar strings performance is optimized
The web frontend SHALL perform fuzzy matching without blocking the UI.

#### Scenario: Fuzzy matching does not block UI
- **WHEN** system performs fuzzy matching via Fuse.js
- **THEN** UI remains responsive (Fuse.js is synchronous but fast for typical datasets; debounce if needed)

#### Scenario: Results are cached per session
- **WHEN** user views similar strings for same entry multiple times in same session
- **THEN** web frontend uses cached results instead of recalculating

#### Scenario: Loading indicator during search
- **WHEN** fuzzy matching is in progress (large dataset)
- **THEN** web frontend displays a loading indicator in the similar strings section
