# DeltaV Localization Refactor — Consolidated Project Context

> **Single source of truth** for this initiative. Read before touching any code, tooling,
> or translation workflow. This file is written for both human engineers and AI coding
> agents. If you are an AI agent: treat this as ground truth over any assumptions from
> training data.

---

## 1. The Problem

DeltaV ships 5–10+ language versions. Historically, each language version has been
built as a **separate binary/image**, with its own full build process (some take hours).

**Root cause:** Localization-related strings drive business logic in the codebase:

```csharp
if (status == "Running") { ... }          // string literal gates control flow
var obj = db.Find("Start Pump");          // display text used as a DB lookup key
if (lang == "Chinese") { /* special-case behavior */ }
```

When a build fails for one locale because a translated string doesn't match what the
logic expects, the historical fix has been to **add more locale-specific branching**
(e.g., an `if` block that only exists for Chinese/Japanese builds). This compounds over time.

### Two kinds of strings

| Type | Definition | Example | Problem if hardcoded |
|---|---|---|---|
| **Display** | UI text, labels, log messages, dialogs | `button.Text = "Start Pump"` | Needs translation, but safe — doesn't break logic |
| **Behavioral** | Drives control flow, DB lookups, equality checks | `if (status == "Running")` | Translation **breaks the program**, forcing per-locale workarounds |

**Core fix:** Behavioral strings become invariant keys/enums that never change per locale.
Display strings are extracted into resource files and resolved client-side at runtime from a single build.

---

## 2. The Goal

- **One build process**, not one per language. Locale becomes runtime-loaded data (JSON pack), not a compile-time artifact.
- Localization strings **stop driving business logic** anywhere in the codebase.
- New code is prevented (via CI gate) from reintroducing this pattern.
- Existing legacy instances are found, tracked, and migrated incrementally.
- Translation quality improves because translators/AI have **DeltaV-specific context** for ambiguous terms.

---

## 3. The Three Tools

These are related but **should be built and reasoned about separately** — do not conflate them.

### Tool 1 — Static Analysis (Roslyn Analyzer) — ✅ complete

**Location:** `src/` (see that folder's own README.md for build/usage details)

- Classifies string literals in C# source as **Behavioral** or **Display** using syntax-level heuristics.
- Emits standard Roslyn diagnostics:
  - `LOC001` (Warning) — string literal in a conditional (if/switch/ternary)
  - `LOC002` (Warning) — string literal passed to a data-access/lookup call (Find/Get/Query)
  - `LOC003` (Warning) — string literal in an equality comparison (==/.Equals())
  - `LOC010` (Info) — display string not yet routed through `Localize(...)`
- Diagnostics flow into **SARIF** (`dotnet build /p:ErrorLog=results.sarif`), which SonarQube and Azure DevOps both consume natively.
- Ships a code fix (lightbulb in IDE) that extracts a LOC010 string into a `Localize("suggested.key")` call.
- **CI strategy:** Gate builds on *new* LOC001–LOC003 occurrences only (diff against a committed SARIF baseline). Do not fail builds on the entire legacy backlog.

**Status:** Complete — 34 tests passing, NuGet package generated.

### Tool 2 — Extraction + Context CLI (`dv-extract-strings`) — ⬜ not yet built

**Input:** SARIF 2.1.0 file containing LOC010 diagnostics from Tool 1. Each LOC010 diagnostic includes:
- File path and line number of the string literal
- The string value itself (e.g., `"Start Pump"`)

**Key generation:** Tool 2 is self-contained — it re-derives resource keys by reading source files and running the same slug algorithm as Tool 1's code fix (`{Class}.{Method}.{slug}`). Does NOT modify Tool 1.

**Output: TWO separate files (not one combined file):**

#### Output 1: Resource JSON (`Resources/en.json`)
Flat key-value pairs that Weblate reads natively:
```json
{
  "PumpController.InitializeUI.startpump": "Start Pump",
  "AlarmSystem.CheckPressure.pressurehigh": "Pressure high"
}
```
Weblate natively supports JSON resource files — it parses this directly as a translation file.

#### Output 2: Data Bank (`data-bank.json`)
Generic translation glossary — translator-centric metadata about each term. Stored separately from the resource JSON:
```json
[
  {
    "key": "PumpController.InitializeUI.startpump",
    "source_string": "Start Pump",
    "domain": "PumpModule"
  }
]
```

Fields:
- `key` — resource key (same as in en.json)
- `source_string` — English source text
- `domain` — product area derived from file path (first folder after `src/`); fallback `"General"`

The Data Bank is intentionally generic. It answers "what does this term mean in our product?" not "which file was this found in?" — the latter lives in SARIF (for developers). The Data Bank is for translators and AI auto-translation.

**How they connect:**
```
Tool 2 produces:
    │
    ├──► Resources/en.json          ──► Weblate reads directly (translation file)
    │
    └──► data-bank.json             ──► Data Bank (translation glossary)
                                          │
                                          ▼
                                    Tool 3 syncs to Weblate glossary via API
                                          │
                                          ▼
                                    Translator sees: "Start Pump" + domain context
```

This is the bridge between "the analyzer found a problem" and "the problem is now structured data that Tool 3 and Weblate can use."

### Tool 3 — Data Bank + Weblate Glossary Sync — ⬜ not yet built

**Data Bank** = the context records store (Output 2 from Tool 2). Can be as simple as a JSON file in the repo or a small database table.

- Consolidates context records from Tool 2 into a persistent store.
- Syncs that context into **Weblate's glossary feature** via its API: term + DeltaV-specific definition + component/domain tag + approved per-locale translations.
- This context is used two ways:
  1. **Human translators** see it directly in the Weblate editor UI.
  2. **AI/auto-translation** (DeepL, LibreTranslate, etc.) gets relevant glossary entries prepended to its prompt/context, so machine translation of ambiguous terms is disambiguated per-component.

**Note:** Weblate's glossary and translation files are separate features. The resource JSON (`en.json`) is the translation file. The glossary is a separate term database that provides context to translators.

---

## 4. The End-to-End Workflow

```
 Dev writes/edits code
        │
        ▼
 Roslyn Analyzer (Tool 1) runs on build/PR
        │
        ├── LOC001/LOC002/LOC003 (Behavioral) ── new occurrence? ──► FAILS CI (must fix before merge)
        │                                   └─ existing legacy? ──► warning only, tracked in backlog
        │
        └── LOC010 (Display) ──► info diagnostic, does not fail build
                    │
                    ▼
        dv-extract-strings CLI (Tool 2) — run periodically or on-demand, sweeps SARIF
                    │
                    ├──► writes Resources/en.json (key-value pairs, Weblate reads this natively)
                    └──► writes data-bank.json (translation glossary: key + source_string + domain)
                                │                                │
                                │                                ▼
                                │                     Data Bank (translation glossary)
                                │                                │
                                │                                ▼
                                │                     Tool 3 syncs glossary → Weblate via API
                                │
                                ▼
                    Weblate detects new en.json keys via webhook
                                │
                                ├──► imports key, marks "untranslated," notifies translators
                                ├──► shows English value + DeltaV context from glossary
                                └──► (optional) auto-translates via DeepL/LibreTranslate,
                                      using glossary context to disambiguate terms
                                │
                                ▼
                    Translator reviews/edits/approves in Weblate
                                │
                                ▼
                    Weblate auto-detects new translation, opens a PR with updated
                    fr.json / ja.json / etc.
                                │
                                ▼
                    Dev reviews & merges PR
                                │
                                ▼
                    CI builds ONE binary. All locale JSON packs are loaded
                    client-side at runtime — no per-locale build.
```

---

## 5. Current Implementation Status

| Piece | Status | Location |
|---|---|---|
| Tool 1 — Roslyn Analyzer (LOC001–LOC003, LOC010) + code fix | ✅ Complete — 34 tests passing, NuGet package generated | `src/` |
| Tool 1 — SARIF → SonarQube/Azure DevOps integration | ✅ Complete — SARIF 2.1.0 compatible, 9 integration tests | `src/README.md` |
| Tool 1 — CI baseline-gate (fail only on new violations) | Documented approach, tooling not yet built | — |
| Tool 2 — `dv-extract-strings` CLI | Not started | — |
| Tool 3 — Data bank + Weblate glossary sync | Not started | — |
| Weblate instance/config for DeltaV | Not confirmed as set up | — |

---

## 6. Open Questions / Decisions Still Needed

- Exact `Localize(...)` API/method signature to standardize on across the codebase.
- Whether Tool 2 runs on a schedule, on every PR, or on-demand.
- Ownership of the SARIF baseline file and who approves suppressions/exceptions for legacy violations.
- Which languages get DeepL/LibreTranslate auto-translation vs. human-only.
- Domain derivation heuristic for non-standard project structures (what if no `src/` folder?).

---

## 7. Ground Rules

1. **Don't conflate the three tools.** Tool 1 classifies. Tool 2 extracts + contextualizes. Tool 3 stores + syncs context for translation. Keep them as separate, composable pieces.
2. **Behavioral strings are the priority**, not display strings. A missed translation is a UX bug; a behavioral string is what's currently costing hours-long build times.
3. **Don't gate CI on the legacy backlog.** Only new violations should block merges.
4. **"Module" (and terms like it) are ambiguous across DeltaV components on purpose.** Any translation tooling must carry component-level context.
5. **The end state is one build.** Any solution that still requires a build step per language is not solving the actual problem.

---

## 8. Diagnostic Rules Reference

| Rule ID | Name | Severity | Description | CI Gate |
|---|---|---|---|---|
| `LOC001` | StringInConditional | Warning | String literal in if/switch/ternary condition | Yes (new only) |
| `LOC002` | StringInDataAccess | Warning | String literal passed to Find/Get/Query call | Yes (new only) |
| `LOC003` | StringInEquality | Warning | String literal in == or .Equals() comparison | Yes (new only) |
| `LOC010` | DisplayStringNotLocalized | Info | Display string not routed through Localize() | No |

---

## 9. DeltaV-Specific Classification Rules

**Display strings** (localizable, no CI gate):
- Alarm message text: `"Pressure high"` → display
- Status label: `"Running"` → display
- User prompt: `"Enter setpoint"` → display
- Error message to operator: `"Communication lost"` → display
- Report label: `"Module State"` → display

**Behavioral strings** (CI gate, requires review):
- Plant model state: `if (state == "Running")` → behavioral
- Control mode: `switch (mode) { "manual", "auto" }` → behavioral
- Alarm severity: `if (severity == "critical")` → behavioral
- Unit type: `if (unit == "pressure")` → behavioral
- Device type: `if (type == "module")` → behavioral

---

## 10. Context Disambiguation Problem

The word "module" appears in multiple DeltaV contexts:

| Context | Source Term | Correct Translation (FR) | Incorrect (No Context) |
|---------|-------------|--------------------------|------------------------|
| Physical device | `module` | `module` (keep English) | `modulo` (math) |
| Software component | `module` | `module` | `composant` |
| Control module | `control module` | `module de contrôle` | `module de commande` |
| Module type (unit) | `module` | `module` | `unité` |

**Weblate glossary entries**:

| Source | Target (FR) | Context | Definition | Notes |
|--------|-------------|---------|------------|-------|
| `module` | `module` | DeltaV physical device | Physical I/O or control device | Keep English |
| `control module` | `module de contrôle` | Software configuration | Logical grouping of control strategy | Not "module de commande" |
| `plant` | `usine` | Physical facility | Manufacturing facility | Not "plante" (plant organism) |
| `alarm` | `alarme` | Safety system | Safety notification | Not "alerte" (temporary warning) |
| `calibration` | `calibration` | Instrument setup | Device accuracy adjustment | Keep English, same in French |

---

## 11. Key Technical Decisions

1. **JSON over .resx**: Source of truth in JSON, consumed by runtime localization library. Enables Weblate integration and AI processing.
2. **Behavioral = CI gate**: Only behavioral strings (logic-driven) block PRs. Display strings get warnings/info but don't block.
3. **Baseline-suppress legacy**: Only new LOC001-LOC003 violations block merges. Existing violations are warnings tracked in backlog.
4. **SARIF 2.1.0 as standard**: All analysis output in SARIF 2.1.0 format — required by both SonarQube and Azure DevOps. `dotnet build /p:ErrorLog=` produces 2.1.0 natively on modern .NET.
5. **Weblate as translation memory**: The glossary provides context for AI disambiguation and consistency enforcement.
6. **DeltaV domain context**: Glossary entries explicitly define DeltaV-specific meanings.
7. **Data Bank is translator-centric**: The data bank stores translation context (key, source string, domain), not code metadata (file, line, class). Code location stays in SARIF for developers; the data bank is for translators and AI auto-translation.
8. **Tool 2 is self-contained**: Tool 2 re-derives resource keys from source files using the same slug algorithm as Tool 1's code fix. No modifications to Tool 1 required.
9. **Flat JSON format**: Resource files use flat key-value pairs (matches Tool 1's code fix output). Simpler to generate and merge; nested format can be added later if needed.

---

## 12. Resource File Convention

**Location:** `Resources/{locale}.json`

**Naming examples:**
```
Resources/en.json        # English (source language)
Resources/fr.json        # French
Resources/de.json        # German
Resources/zh.json        # Chinese
```

**Flat key format (primary):**
```json
{
  "PumpController.InitializeUI.startpump": "Start Pump",
  "AlarmSystem.CheckPressure.pressurehigh": "Pressure high",
  "ErrorHandler.ShowDialog.devicenotfound": "Device '{0}' not found"
}
```

This is the format Tool 1's code fix and Tool 2 produce. Flat keys are simpler to generate, merge, and deduplicate. Weblate reads this natively.

**Nested key format (alternative):**
```json
{
  "plant.model.state": {
    "idle": "Idle",
    "running": "Running",
    "faulted": "Faulted"
  },
  "control.module.mode": {
    "manual": "Manual",
    "auto": "Automatic"
  }
}
```

Nested format is more human-readable for large key sets but harder to merge automatically. Can be adopted later if the team prefers it — a flat-to-nested converter is straightforward.

---

## 13. Implementation Phases

### Phase 1: Foundation (Week 1-2)
1. Create Roslyn Analyzer project (`src/`)
2. Implement LOC001 (StringInConditional) and LOC002 (StringInDataAccess)
3. Write unit tests with `Microsoft.CodeAnalysis.Testing`
4. Create `Resources/en.json` with existing string keys

### Phase 2: Classification (Week 3-4)
5. Implement ClassificationEngine with rule-based logic
6. Implement LOC003 (StringInEquality) and LOC010 (DisplayStringNotLocalized)
7. Implement CodeFixProvider for auto-extraction

### Phase 3: CI/CD Integration (Week 5-6)
8. Configure SARIF generation in pipeline
9. Implement baseline-gate script (diff against committed baseline)
10. Setup Weblate integration with webhook for new keys

### Phase 4: Production Rollout (Week 7-8)
11. Run analyzer on full DeltaV codebase, categorize all strings
12. Extract Display strings to `Resources/en.json`
13. Enable quality gates on PRs

---

## 14. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| New behavioral strings blocked | 100% | CI gate enforcement |
| Display strings extracted | 90%+ | `Resources/en.json` coverage |
| Translation context accuracy | 95%+ | Weblate glossary usage |
| AI translation quality | 90%+ | Human review of first batch |
| Build processes | 1 (was 5-10+) | Pipeline count |
