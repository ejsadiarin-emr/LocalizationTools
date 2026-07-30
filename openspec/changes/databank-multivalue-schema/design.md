## Context

The databank tool parses localization files (FHX, JSON, RC, AHC, GRF, RESX) into a structured JSON format (`data-bank.json`) and stores them in MongoDB via an API. The frontend (WebView2 desktop app) displays entries for viewing and editing.

Current state: 609 unique keys produce 1,195 entries because each key appears once per locale (mostly en + zh-CN). The flat schema forces users to filter by locale to see translations, with no way to compare all languages for a key side-by-side.

Key constraint: Keys are globally unique across formats (verified — 0 cross-format collisions in the dataset). This means `key` alone is a sufficient unique identifier.

Stakeholders: Localization engineers who need to search across 60K-100K keys in production, view all translations for a key, and edit values on the fly.

## Goals / Non-Goals

**Goals:**
- Collapse all locale variants of a key into a single entry in data-bank.json
- Enable "same string viewer" — display all locale values for a key in one row
- Enable inline editing of individual locale values within a grouped entry
- Simplify metadata by removing unused fields (rcId, rcDefine, isBehavioral, translationStatus)
- Derive translation status from `isTranslated` + `doNotTranslate` booleans
- Maintain backward compatibility at the API level (flat endpoint can still exist alongside grouped)

**Non-Goals:**
- Real-time collaborative editing
- Automatic migration from v2 — raw l10n files will be re-parsed
- Supporting locale-specific metadata (e.g., different `doNotTranslate` per locale) — metadata remains per-key
- Changing the parser output format beyond grouping (parser internals stay the same)

## Decisions

### Decision 1: Schema shape — `values` array vs. locale-keyed object

**Chosen**: Array of `{locale, value}` objects

```json
"values": [
  { "locale": "en", "value": "CRITICAL" },
  { "locale": "zh-CN", "value": "危急" }
]
```

**Alternative considered**: Locale-keyed object `{"en": "CRITICAL", "zh-CN": "危急"}`

**Rationale**: Array preserves insertion order, is easier to iterate in MongoDB aggregation pipelines, and allows future extension (e.g., adding `lastModified` per locale without breaking the schema). Object keys can't be queried as easily in MongoDB.

### Decision 2: Source structure — locale-keyed dictionary

**Chosen**: `sources` as a dictionary keyed by locale

```json
"sources": {
  "en": { "format": "fhx", "file": "...", "path": "..." },
  "zh-CN": { "format": "fhx", "file": "...", "path": "..." }
}
```

**Alternative considered**: Single source array `[{locale, format, file, path}]`

**Rationale**: Dictionary allows O(1) lookup by locale when displaying a specific translation. The `sources` structure mirrors `values` — each locale entry in `values` has a corresponding entry in `sources`. For keys with a single locale (55 keys), only one source entry exists.

### Decision 3: ID strategy — key-based

**Chosen**: Use the key directly as the ID (since keys are globally unique)

```json
"id": "@CRITICAL@"
```

**Alternative considered**: UUIDv7

**Rationale**: Keys are already unique (verified), human-readable, and meaningful. UUIDv7 adds complexity without benefit for this use case. If key format ever changes, the ID strategy can be revisited.

**Risk**: If a future parser introduces keys that collide across formats, this breaks. Mitigation: the verified dataset shows 0 cross-format collisions, and the format prefix is embedded in `sources`, not the key.

### Decision 4: Metadata simplification

**Chosen**: Remove `rcId`, `rcDefine`, `isBehavioral`, `translationStatus`. Keep `comment`, `formatSpecifiers`, `doNotTranslate`, `isTranslated`.

**Rationale**:
- `rcId` / `rcDefine`: Not referenced in frontend (`app.js`), API, or CLI output. Dead fields.
- `isBehavioral`: Not used in filtering, display, or any business logic.
- `translationStatus`: Frontend derives status from `value.trim() !== ''` anyway. The field is always "Untranslated" in the data. Derive from `isTranslated` + `doNotTranslate`.

**Derivation logic**:
```
if (doNotTranslate)       → "Do Not Translate"
else if (!isTranslated)   → "Untranslated"
else                      → "Translated"
```

### Decision 5: MongoDB index strategy

**Chosen**: Unique index on `key` (replaces current unique index on `key + locale`)

Additional indexes:
- `values.locale` (for locale-based filtering)
- `sources.format` (for format-based filtering)
- `metadata.doNotTranslate` (for DNT filtering)

**Rationale**: With one document per key, the unique constraint moves to `key`. Locale queries use array filters on `values.locale`.

### Decision 6: API endpoint evolution

**Chosen**: Keep existing `/api/entries` endpoint returning the new schema. Add `/api/entries/grouped` as an optional convenience endpoint that pre-groups by key (for clients that want the aggregated view without client-side processing).

**Rationale**: Since entries are already grouped in the new schema, the flat endpoint naturally returns grouped data. The `/grouped` endpoint is a future optimization, not a requirement for this change.

### Decision 7: Parser merge strategy

**Chosen**: Parsers continue to produce flat entries internally. A post-processing step groups entries by key before writing to data-bank.json.

**Rationale**: Parser logic (line parsing, locale detection, format detection) stays clean and single-purpose. The grouping is a data transformation step that happens after parsing, not during. This keeps parsers testable in isolation.

## Risks / Trade-offs

- **[Breaking change]** → No automatic migration. Re-parse raw l10n files to generate v3. This is acceptable for a PoC but will need a migration tool for production.
- **[Key collision risk]** → If a future format introduces keys that collide with existing ones, the key-based ID breaks. Mitigation: prefix ID with format if needed (e.g., `fhx::@CRITICAL@`).
- **[Metadata per-locale limitation]** → `doNotTranslate` is per-key, not per-locale. If a key is DNT in one language but translatable in another, this schema can't express that. Acceptable for current use case.
- **[MongoDB query complexity]** → Filtering by locale requires array operators (`$elemMatch`, `$filter`) instead of simple field equality. Performance impact is minimal at current scale (609 docs).
- **[Frontend complexity]** → Table rendering becomes more complex (multi-locale columns or expandable rows). Requires careful UX design for the "same string viewer."

## Migration Plan

1. Re-parse all raw l10n files with updated parsers → generates v3 data-bank.json
2. Drop existing MongoDB `DataBankEntry` collection
3. Import v3 data into MongoDB
4. Deploy updated API
5. Deploy updated frontend

**Rollback**: Keep v2 data-bank.json as backup. If v3 fails, revert to v2 parsers and schema.

## Open Questions

- Should the desktop app support editing individual locale values inline, or open a modal/panel for editing? (UX decision)
- Should `values` include a `lastModified` timestamp per locale for tracking edit history? (Future enhancement)
- Should we add a `format` field at the entry level (since all sources share the same format for a given key)? (Could simplify queries)
