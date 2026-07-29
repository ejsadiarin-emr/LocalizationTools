# Databank API vs CLI Architecture Analysis

## Current Architecture: Two Parallel Systems (Problem)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Program.cs (DI)                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  SINGLETON: IDataBankService → FileDataBankService  (file-based)   │
│  SINGLETON: IExtractionService → ExtractionService                 │
│  SINGLETON: IStatisticsService → StatisticsService                 │
│  SCOPED:    IDataBankRepository → MongoDataBankRepository (Mongo)  │
│                                                                     │
├────────────────────────┬────────────────────────────────────────────┤
│  MVC Controllers       │  Minimal API Endpoints                    │
│  (uses IDataBankService│  (uses IDataBankRepository)               │
│   → FileDataBankService│   → MongoDataBankRepository               │
│   → JSON file)         │   → MongoDB)                              │
├────────────────────────┼────────────────────────────────────────────┤
│ GET  /api/entries      │  GET  /api/entries       ← COLLISION     │
│ GET  /api/entries/{id} │  GET  /api/entries/{id}  ← COLLISION     │
│ POST /api/entries      │  POST /api/entries       ← COLLISION     │
│ PUT  /api/entries/{id} │  PUT  /api/entries/{id}  ← COLLISION     │
│ DELETE /api/entries/{id}│ DELETE /api/entries/{id} ← COLLISION     │
│ GET  /api/stats        │  GET  /api/metadata                      │
│ GET  /api/stats/coverage│ GET  /api/sessions                      │
│ POST /api/extract      │  GET  /api/entries/count                  │
└────────────────────────┴────────────────────────────────────────────┘
```

**5 route collisions** on `/api/entries`. ASP.NET will pick one (likely the MVC controller wins due to `MapControllers()` being called first).

### The Core Problem

| Layer | Data Store | State |
|---|---|---|
| MVC Controllers + ExtractionService + StatisticsService | `data-bank.json` file via `FileDataBankService` (in-memory ConcurrentDictionary) | **Active** |
| Minimal API Endpoints | MongoDB via `MongoDataBankRepository` | **Active** |
| MongoDB collections | `DataBankEntry`, `DataBankMetadata`, `TranslationSession` | **Exist but unwritten** |

The extraction flow writes to the **file**, not MongoDB. The MongoDB repository is wired up but **nothing writes to it** — `ExtractionService` calls `_dataBankService.AddEntries()` which goes to `FileDataBankService` → JSON file.

---

## Shared Code

The API **references the CLI project directly** (`DataBank.Api.csproj` → `DataBank.Cli`). The API's `ExtractionService` calls CLI parsers (`ResxParser`, `RcParser`, `FhxParser`, `AhcParser`) directly. The data models (`LocalizedStringEntry`, `SourceInfo`, `EntryMetadata`, `DataBankOutput`) are shared.

---

## MongoDB Document Schema vs CLI JSON Schema

### CLI `data-bank.json` (what's in the file):

```json
{
  "version": 2,
  "generated": "2026-07-25T10:00:00Z",
  "entries": [
    {
      "id": "resx::SomeKey::de",
      "key": "SomeKey",
      "value": "Einige Werte",
      "locale": "de",
      "source": {
        "format": "resx",
        "file": "Messages.de.resx",
        "path": "/full/path/to/Messages.de.resx",
        "encoding": "utf-8"
      },
      "metadata": {
        "comment": "A comment",
        "rcId": null,
        "rcDefine": null,
        "isBehavioral": false,
        "formatSpecifiers": ["{0}", "{1}"],
        "doNotTranslate": false,
        "isTranslated": true,
        "translationStatus": "Translated"
      }
    }
  ],
  "translationSummary": {
    "totalKeys": 100,
    "translatedKeys": 80,
    "untranslatedKeys": 15,
    "doNotTranslateKeys": 5,
    "needsReviewKeys": 0,
    "completionPercentage": 80.0
  }
}
```

### MongoDB `DataBankEntryDocument` (what's stored):

```json
{
  "_id": "resx::SomeKey::de",
  "Key": "SomeKey",
  "Value": "Einige Werte",
  "Locale": "de",
  "Source": {
    "Format": "resx",
    "File": "Messages.de.resx",
    "Path": "/full/path/to/Messages.de.resx",
    "Encoding": "utf-8"
  },
  "Metadata": {
    "Comment": "A comment",
    "RcId": null,
    "RcDefine": null,
    "IsBehavioral": false,
    "FormatSpecifiers": ["{0}", "{1}"],
    "DoNotTranslate": false,
    "IsTranslated": true,
    "TranslationStatus": "Translated"
  }
}
```

**Schema field mapping is 1-to-1** — `DataBankEntryDocument` mirrors `LocalizedStringEntry` correctly. The `BsonElement` names are PascalCase in MongoDB but that's just the BSON serialization — the data structure is equivalent.

### What's missing from MongoDB:

| CLI JSON | MongoDB | Status |
|---|---|---|
| `version` | `DataBankMetadataDocument.Version` | ✅ Separate collection |
| `generated` | `DataBankMetadataDocument.Generated` | ✅ Separate collection |
| `translationSummary` | Not stored anywhere | ❌ Computed on-the-fly (correct approach) |
| Entry fields | All present in `DataBankEntryDocument` | ✅ Complete |

---

## Key Differences Between API and CLI

1. **File Detection**: CLI uses `FileDetector.DiscoverFiles()` with `.grf` support; API `ExtractionService` uses hardcoded `*.resx, *.rc, *.fhx, *.ahc` patterns (no `.grf` or `.json` support)
2. **Locale Override / Encoding Override**: CLI supports `--locale` and `--encoding` params; API extraction has no such parameters
3. **`resource.h` Symbol Resolution**: CLI supports `--resource-h` for RC symbol resolution; API extraction does not
4. **Translation Status Flagging**: CLI has `--flag-untranslated` that runs `TranslationStatusAnalyzer.Analyze()` and embeds `TranslationSummary` in output; API extraction does not do this
5. **Coverage Analysis**: CLI has `--coverage` that runs `CoverageAnalyzer.Analyze()` using EN/Translated directory pair detection; API's `StatisticsService.GetCoverage()` is a simpler implementation that just counts by locale, not using file-pair matching
6. **Format Detection**: CLI's `FileDetector.DetectFormat()` handles `.txt` files in FHX directories and `.json` files; API only checks extensions
7. **Dual API layers**: Both MVC Controllers AND Minimal API Endpoints exist simultaneously, operating on different data stores (file vs MongoDB)
8. **Output JSON**: CLI writes `DataBankOutput` with `version`, `generated`, `entries`, `translationSummary`; API `FileDataBankService.SaveData()` writes `DataBankOutput` WITHOUT `version` and WITHOUT `generated` fields (only `Entries`)

---

## Recommendation: Schema for API Response

Since MongoDB is the target persistence, the API should return data that matches the CLI's `DataBankOutput` JSON shape.

### Option A: Return full `DataBankOutput` structure from a single endpoint

```
GET /api/databank/export
→ {
    "version": 2,
    "generated": "2026-07-25T10:00:00Z",
    "entries": [...],
    "translationSummary": { ... }  // computed from MongoDB aggregation
  }
```

This gives API consumers the same JSON as `data-bank.json`. The `translationSummary` is computed via MongoDB aggregation pipelines (not stored), which is correct — it's derived data.

### Option B: Keep current CRUD endpoints, add export endpoint (Recommended)

```
GET /api/entries              → paginated entries (current behavior, MongoDB-backed)
GET /api/entries/{id}         → single entry
GET /api/entries/count        → count
GET /api/metadata             → version + generated
GET /api/databank/export      → full DataBankOutput JSON (for parity with CLI)
```

- CRUD endpoints stay granular (good for UI consumption)
- Export endpoint gives 1:1 parity with `data-bank.json`
- `translationSummary` is computed, not stored (avoids staleness)
- MongoDB aggregation pipelines handle the summary efficiently

### Schema the API should return per-entry (matching CLI):

```json
{
  "id": "string",
  "key": "string",
  "value": "string",
  "locale": "string",
  "source": {
    "format": "resx|rc|fhx|ahc|grf|json",
    "file": "string",
    "path": "string",
    "encoding": "string|null"
  },
  "metadata": {
    "comment": "string|null",
    "rcId": "int|null",
    "rcDefine": "string|null",
    "isBehavioral": false,
    "formatSpecifiers": [],
    "doNotTranslate": false,
    "isTranslated": false,
    "translationStatus": "Translated|Untranslated|DoNotTranslate|NeedsReview"
  }
}
```

This is exactly what `DataBankEntryDocument` already stores. The only work needed is:
1. Remove the MVC Controller layer + `FileDataBankService`
2. Rewire `ExtractionService` to write to `MongoDataBankRepository` instead of `FileDataBankService`
3. Add an export endpoint that aggregates from MongoDB

---

## Summary

| Question | Answer |
|---|---|
| Does MongoDB integrate correctly? | **Partially** — `MongoDataBankRepository` is wired up and has correct indexes, but **nothing writes to it**. Extraction goes to file. |
| Is there an in-memory store? | **Yes** — `FileDataBankService` holds a `ConcurrentDictionary` + JSON file. This should be removed. |
| Should API return same schema as CLI? | **Yes** — `DataBankEntryDocument` already mirrors the CLI models correctly. Add a `GET /api/databank/export` endpoint. |
| What about `translationSummary`? | **Compute it** from MongoDB aggregation, don't store it. It's derived data. |
