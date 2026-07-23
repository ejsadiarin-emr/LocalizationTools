# Comprehensive Analysis of `l10n-files/` — All 6 File Formats

## 1. RESX (`.resx`) — .NET Resource XML

**Structure**: Standard .NET `<root>` → `<data key="..."><value>...</value></data>` pattern.

**Key characteristics**:
- **EN**: `Strings.resx` — 10 `<data>` entries with PascalCase keys (`ConfirmAndVerifyFailedValidation`, `ESigTitle`, `Username`, etc.)
- **Translated**: `Strings.zh-CN.resx` — Same keys, Chinese (Simplified) values. Locale detected from filename (`.zh-CN` suffix)
- **All strings are display-only** — ESig confirmation prompts, user input labels, error messages without format specifiers
- **No behavioral strings** — no string comparisons, no `switch`/`if` keys used in control flow

**Classification: 100% DISPLAY strings**

---

## 2. RC (`.rc`) — Win32 Resource Scripts

**Structure**: `LANGUAGE` directive → `STRINGTABLE BEGIN...END` blocks + `DIALOGEX` definitions.

**Key statistics**:
- **38 STRINGTABLE blocks** containing **~195 `IDS_*` entries**
- **306 DIALOG control elements** (CAPTION, LTEXT, PUSHBUTTON, CONTROL, GROUPBOX, DEFPUSHBUTTON)
- EN: 111KB (1,722 lines), Translated: 160KB (44% larger due to DBCS)

**CRITICAL: Mixed behavioral + display strings**

| Subcategory | Example | Classification |
|---|---|---|
| **Dialog UI** (CAPTION, PUSHBUTTON, LTEXT, GROUPBOX) | `"OK"`, `"<- Back"`, `"Next ->"`, `"Select Workstation Type"`, `"DeltaV System Name"` | **Display** |
| **Wizard instructions** (LTEXT with multi-line help text) | `"In this step, DeltaV Workstation Configuration will collect..."` | **Display** |
| **User-facing errors** (simple messages) | `"Administrative privileges are required to change passwords."` | **Display** |
| **Developer-facing errors** (with `%d`, `%s`, `%x` format specifiers) | `"Failed in CreateService. Error %d"`, `"Error changing account password.\n"`, `"Failed to update DeltaV registry with strPowerupSrcPath. Error %d"` | **Behavioral** — format strings used in `FormatMessage`/`sprintf` |
| **Technical diagnostic messages** | `"CHawkServices::InstallHawkServices\nFailed in OpenSCManager. Error %d"` — contains class/method names | **Behavioral** — developer debugging |
| **Confirmation dialogs** | `"Are you sure this workstation will operate in a workgroup?"` | **Display** |
| **Validation messages with reasons** | `"The DeltaV System Name or Workstation Name is invalid. \r\nReason: %s.\r\nPlease enter only letters..."` | **Mixed** — display text with behavioral format specifiers |
| **Status strings** | `"Running"` (IDS_STATUS_RUNNING) | **Potentially behavioral** if used in conditionals |

**Key behavioral patterns found**:
- **`%d` / `%s` / `%x` format specifiers**: ~25+ strings use `sprintf`-style formatting → developer-facing, must preserve format
- **Class/function names in strings**: `"CHawkServices::InstallHawkServices\nFailed in..."` — not for end users
- **`\r\n` / `\n` escape sequences**: Embedded newlines for multi-line error output
- **`\t` tab escapes**: Formatting control characters
- **`""` double-quote escaping**: RC-specific string escaping

---

## 3. FHX (`AlarmWords.txt`) — Alarm Word Lookup

**Structure**: Tab-delimited: `@Key@\t"context source"\tValue`

**Statistics**: 142 lines each (EN + Translated)

**Content categories** (from context metadata):

| Category | Context Source | Count | Classification |
|---|---|---|---|
| **Alarm priorities** | `alarmannunciations.fhx, alarm priority` | 4 | **BEHAVIORAL** — used for alarm subsystem lookups |
| **Alarm descriptions** | `alarmtypes.fhx, alarm description` | 15 | **Display** — user-facing descriptions |
| **Alarm types** | `alarmtypes.fhx, alarm type` (incl. SIS/MP variants) | 42 | **BEHAVIORAL** — alarm classification identifiers |
| **Alarm words** (abbreviated) | `alarmtypes.fhx, alarm word` | 20 | **BEHAVIORAL** — short codes (ANY, SYSTEM, HIHI, etc.) |
| **Alarm attributes** | `modules.fhx, SISmodules.fhx, an alarm attribute` | 12 | **BEHAVIORAL** — internal attribute names |
| **Enum options** | `enum.fhx, alarm select option` | 6 | **Mixed** — "Alarm Select" is display, "HI HI"/"LO LO" are behavioral |
| **"do NOT translate"** | (explicit instruction) | 5 | **BEHAVIORAL** — must never be translated |
| **Error messages** | `rtstringtable.rc` | 8 | **Mixed** — some display ("Batch Historian"), some behavioral ("Adapter Error - %s processing stopped from node %s") |
| **SIS-specific types** | `sisalarmtypes.fhx, alarm type` | 8 | **BEHAVIORAL** — SIS subsystem identifiers |
| **MP-specific types** | `mpalarmtypes.fhx, alarm type` | 8 | **BEHAVIORAL** — MP subsystem identifiers |
| **Statistical alarms** | `alarmtypes.fhx, alarm type` | 1 | **Display** — "Statistical Alarm" |

**CRITICAL patterns**:
- **Keys with `@` delimiters**: `@CRITICAL@`, `@WARNING@`, `@High Alarm@` — these are **lookup keys**, NOT display text
- **Translated file keeps EN keys**: `@SIS Change From Normal@` → `SIS?????` — key is English, value is Chinese
- **Some values are identical in EN/Translated**: `@COMM_ALM@` → `COMM_ALM` (marked "do NOT translate")
- **Technical identifiers preserved**: `@DV_HI_ALM@` → `DV_HI_ALM` (internal attribute names)
- **Format specifiers in rtstringtable entries**: `"Adapter Error - %s processing stopped from node %s"` — behavioral
- **"Batch", "Campaign", "Chronicle"** remain untranslated in Translated file — proper nouns/product names

**Classification: ~70% BEHAVIORAL, ~20% DISPLAY, ~10% MIXED**

---

## 4. AHC (`.ahc`) — iFix Contextual Display

**Structure**: XML with `<ContextualDisplay>` → `<ContainedElements>` → `<Text>`, `<Gem>`, `<CheckBox>` elements.

**Multi-language support**: Inline `<LanguageValues>` with **4 languages**: `en`, `jp`, `ru`, `zh`

**Key observations**:

| Element Type | `Name` attribute | Label Source | Title/Content Pattern | Classification |
|---|---|---|---|---|
| **Text** (section headers) | `txtLimits`, `txtAlarms`, `txtMisc`, `txtDiagnostics`, `txtEnable` | `<Reference>GL.Library.S_*</Reference>` | **Identical** in all 4 languages (English) — internal identifiers | **Behavioral** — `Name` is an internal control ID |
| **Gem** (label+value pairs) | `CD_LABEL_SCALEDVALUE*` | `<Reference>GL.Library.S_HH_Limit` etc. | Labels via **library references**, not inline text | **Indirect** — real strings live in `GL.Library` |
| **Gem** (alarm data) | `CD_ALARM_DATA*` | `<Reference>GL.Library.S_HHAlarm` etc. | Alarm labels via library references | **Indirect** — library-managed |
| **CheckBox** | `chkboxEnable` | **Empty** `<Content>` in all 4 languages | No user-facing text | **Non-localizable** |
| **Title** (top-level) | N/A | `"Alarm module detail display"` | **Same English** in en/jp/ru/zh | **NOT TRANSLATED** — needs translator attention |
| **Description** | N/A | EN: `"Alarm module detail display"` | **zh IS translated**: `"报警模块详细信息"` | **Display** — properly localized for zh only |

**CRITICAL patterns**:
- **Library references dominate**: Most labels use `<Reference>GL.Library.S_*</Reference>` — actual translations live in a separate library, not in this file
- **Inline LanguageValues are mostly untranslated**: Title content is English in all 4 languages
- **One exception**: Description for `zh` IS translated — proves the mechanism works
- **Engineering units**: `"%"` is identical across all languages (correct — universal symbol)
- **Internal identifiers in Title fields**: `"txtLimits"`, `"txtAlarms"` etc. — these are NOT meant for translation
- **`no units`** — appears as a scale placeholder, universal across languages

**Classification: ~15% Display (Description zh), ~10% Library-referenced (indirect), ~75% Non-localizable/Behavioral**

---

## 5. GRF (`.grf`) — iFix Graphic Display

**Structure**: OLE compound binary document (like .doc/.xls format)

**Key facts**:
- **EN**: 929,792 bytes (908 KB)
- **Translated**: 927,232 bytes (905.5 KB) — **slightly smaller** (unusual for DBCS)
- Contains embedded `VBAProject` with macros
- Internal streams: `CONTROLSAVESTREAM`, `TabStripStorage`, `DVCtrlAlmSum1Storage`
- **Binary format** — cannot be parsed as text

**Classification: Requires specialized binary parser (OLE compound document reader)**

---

## 6. iFix DLLs (`BaseErrorsRes.dll`) — .NET Satellite Assembly

**Key facts**:
- **Only Translated version** (no EN source file)
- 176,128 bytes (172 KB)
- .NET satellite assembly — compiled from `.resx` → `.resources` → `.dll`
- Contains localized strings for error handling

**Classification: Requires IL decompilation (ILSpy/dnSpy) to extract strings**

---

## Summary Classification Matrix

| Format | Display | Behavioral | Mixed | Non-localizable | Unknown/Binary |
|---|---|---|---|---|---|
| **RESX** | 100% | 0% | 0% | 0% | 0% |
| **RC** | ~60% | ~25% | ~15% | 0% | 0% |
| **FHX** | ~20% | ~70% | ~10% | 0% | 0% |
| **AHC** | ~15% | ~10% | 0% | ~75% | 0% |
| **GRF** | — | — | — | — | 100% (binary) |
| **DLL** | — | — | — | — | 100% (compiled) |

## Parser Implications

| Format | Existing Parser? | Parse Complexity | Key Challenges |
|---|---|---|---|
| **RESX** | Yes (`ResxParser.cs`) | Low | Locale detection from filename |
| **RC** | Partial (`RcParser.cs`) — STRINGTABLE only | Medium | Missing DIALOG/CAPTION/LTEXT/PUSHBUTTON parsing; format specifier detection; behavioral vs display classification |
| **FHX** | **No** | Low-Medium | Tab-delimited, Unicode encoding; context metadata parsing; "do NOT translate" flags |
| **AHC** | **No** | Medium | XML parsing; 4-language inline structure; library reference resolution; distinguishing internal IDs from display text |
| **GRF** | **No** | **Very High** | OLE compound binary; embedded VBA; requires specialized libraries |
| **DLL** | **No** | **Very High** | Requires IL decompilation; .NET assembly metadata |
