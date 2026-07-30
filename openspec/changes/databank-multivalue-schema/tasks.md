## 1. Models and Schema

- [x] 1.1 Update `LocalizedStringEntry` — replace `Value` (string) and `Locale` (string) with `Values` (List<LocaleValue>), add `LocaleValue` class with `Locale` and `Value` properties
- [x] 1.2 Update `EntryMetadata` — remove `RcId`, `RcDefine`, `IsBehavioral`, `TranslationStatus` properties; keep `Comment`, `FormatSpecifiers`, `DoNotTranslate`, `IsTranslated`
- [x] 1.3 Update `DataBankEntryDocument` (MongoDB) — same changes as LocalizedStringEntry: `Values` array instead of flat `Value`/`Locale`, add `Sources` dictionary keyed by locale
- [x] 1.4 Update `ApiModels.cs` — update `CreateEntryRequest` to accept `Values` array and `Sources` dictionary
- [x] 1.5 Update `DataBankOutput` — bump version to 3

## 2. Parsers — Merge Strategy

- [x] 2.1 Add post-parse grouping utility — takes flat `List<LocalizedStringEntry>` and groups by `Key`, merging `Values` and `Sources` per locale
- [x] 2.2 Update `FhxParser` — integrate grouping step so output is one entry per key with multi-locale values
- [x] 2.3 Update `JsonParser` — integrate grouping step
- [x] 2.4 Update `RcParser` — integrate grouping step
- [x] 2.5 Update `AhcParser` — integrate grouping step
- [x] 2.6 Update `GrfParser` — integrate grouping step
- [x] 2.7 Update `ResxParser` — integrate grouping step
- [x] 2.8 Verify parser output — re-parse test data and confirm v3 schema compliance

## 3. MongoDB and Repository

- [x] 3.1 Update `MongoDataBankRepository` — change indexes: unique index on `Key` (remove `Locale` from unique index)
- [x] 3.2 Update `GetFilteredEntriesAsync` — support filtering by locale using `$elemMatch` on `Values` array
- [x] 3.3 Update `ReplaceOrInsertManyAsync` — match on `Key` instead of `Key + Locale`
- [x] 3.4 Update aggregation pipelines — `GetEntryCountByLocaleAsync`, `GetTranslationStatusCountsAsync`, `GetTranslationStatusCountsByLocaleAsync` to work with nested `Values` array
- [x] 3.5 Add locale-specific update method — update a single locale value within an entry's `Values` array

## 4. API Endpoints

- [x] 4.1 Update `EntriesEndpoints` — adjust existing endpoints for new schema shape
- [x] 4.2 Add `PUT /api/entries/{key}/locales/{locale}` endpoint — update a specific locale value
- [x] 4.3 Add `PATCH /api/entries/{key}/values` endpoint — bulk update multiple locale values
- [x] 4.4 Update `StatsEndpoints` — recalculate locale coverage counts using grouped entries

## 5. Frontend

- [x] 5.1 Update `app.js` `renderTable` — render grouped rows with locale columns (en, zh-CN, ru, ja)
- [x] 5.2 Update `app.js` `showDetail` — display all locale values and sources in the detail panel
- [x] 5.3 Update `app.js` `populateFilters` — adapt locale filter for grouped entries (filter by keys that have non-empty value for selected locale)
- [x] 5.4 Update `app.js` `applyFilters` — search across all locale values, not just one
- [x] 5.5 Update `app.js` `getStatus` / `updateDashboard` — derive status from `isTranslated` + `doNotTranslate`, calculate per-locale coverage stats
- [x] 5.6 Add inline editing — click on locale cell to edit, save on Enter/blur, cancel on Escape
- [x] 5.7 Update `renderGrfTab` — adapt for new schema structure

## 6. Testing and Verification

- [x] 6.1 Re-parse all l10n-files with updated parsers → generate data-bank.json v3
- [x] 6.2 Verify data-bank.json v3 schema compliance (609 unique keys, no duplicate keys)
- [ ] 6.3 Verify MongoDB import with new schema
- [ ] 6.4 Verify API endpoints return correct grouped structure
- [ ] 6.5 Verify frontend renders grouped table with locale columns
- [ ] 6.6 Verify inline editing works (update single locale value)
- [ ] 6.7 Verify search works across all locale values
