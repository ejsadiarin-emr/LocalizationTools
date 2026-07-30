## Why

The current data-bank.json schema stores one entry per key per locale, resulting in duplicated keys across the dataset (609 unique keys produce 1,195 entries). This makes it difficult to view and edit translations for a single key across all locales — the primary workflow for localization work. Users must search for the same key multiple times across different locale-filtered views, with no side-by-side comparison.

A multi-value schema collapses all locale variants of a key into a single entry, enabling a "same string viewer" where all translations for a key are visible and editable in one place.

## What Changes

- **BREAKING**: `data-bank.json` schema changes from v2 to v3 — `value` (string) and `locale` (string) are replaced by `values` (array of `{locale, value}` objects)
- **BREAKING**: `source` changes from a single object to a locale-keyed dictionary (`{locale: source}`) since each locale comes from a different file
- **BREAKING**: `id` format changes — no longer includes locale in the ID since entries are now per-key, not per-key-per-locale
- Remove unused metadata fields: `rcId`, `rcDefine`, `isBehavioral`, `translationStatus` (derive status from `isTranslated` + `doNotTranslate`)
- Remove `source.encoding` (parser concern, not data concern)
- All parsers (FHX, JSON, RC, AHC, GRF, RESX) updated to generate grouped entries
- API models and MongoDB schema updated to match new structure
- Frontend updated to display multi-locale values and support grouped editing
- New unique ID strategy (key-based or UUIDv7)

## Capabilities

### New Capabilities
- `multivalue-schema`: Core schema definition for multi-locale entries — the data model, JSON format, and MongoDB document structure
- `same-string-viewer`: Frontend capability to display all locale values for a key side-by-side, with inline editing
- `locale-aware-editing`: API and frontend support for editing individual locale values within a grouped entry

### Modified Capabilities
<!-- No existing specs to modify — this is a greenfield schema change -->

## Impact

- **Data format**: `data-bank.json` v2 → v3 (breaking change, requires re-parse or migration)
- **Models**: `LocalizedStringEntry`, `DataBankEntryDocument`, `EntryMetadata`, `CreateEntryRequest` — all restructured
- **Parsers**: All 6 parsers (`FhxParser`, `JsonParser`, `RcParser`, `AhcParser`, `GrfParser`, `ResxParser`) must group entries by key before output
- **API**: `IDataBankRepository`, `MongoDataBankRepository` — new indexes, aggregation queries, and update patterns for nested locale values
- **Frontend**: `app.js` — table rendering, filters, detail panel, dashboard stats all adapted for grouped entries
- **MongoDB**: Index strategy changes (unique on `key` instead of `key+locale`), new aggregation pipelines for locale-based queries
- **Backward compatibility**: No automatic migration — raw l10n files must be re-parsed to generate v3 format
