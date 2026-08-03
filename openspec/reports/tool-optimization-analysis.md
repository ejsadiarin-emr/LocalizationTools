# Critical Analysis: Tool Optimization for Real Localization Data

## Part 1: DataBank.Cli — Critical Gaps

### 1.1 RC Parser: Catastrophically Incomplete

**The `RcParser.cs` only parses `STRINGTABLE` blocks.** Looking at the actual `PCInstall.rc`:

- **195 STRINGTABLE entries** → parsed ✓
- **306 DIALOG control strings** (CAPTION, LTEXT, PUSHBUTTON, GROUPBOX, CONTROL) → **completely ignored** ✗

These 306 dialog strings are **the most important user-facing strings** in the entire file — button labels ("OK", "<- Back", "Next ->", "Restart Now"), dialog titles ("DeltaV Workstation Configuration"), groupbox labels ("Select Workstation Type", "Network Redundancy"), checkbox labels ("Install Microsoft SQL"), wizard instructions, etc. The parser misses ALL of them.

**Impact**: The Data Bank would have zero coverage for the primary UI of the installer application.

**Fix required**: Parse `DIALOGEX`/`DIALOG` blocks. Extract:
- `CAPTION "text"` → dialog title
- `LTEXT "text",id,...` → static text labels
- `PUSHBUTTON "text",id,...` → button labels
- `DEFPUSHBUTTON "text",id,...` → default button labels
- `GROUPBOX "text",id,...` → group box labels
- `CONTROL "text",id,"Button",...` → checkbox/radio button labels

### 1.2 RC Parser: No Format Specifier Detection

The actual RC data contains strings like:
- `"Failed in CreateService. Error %d"` — behavioral (developer-facing)
- `"Database Space - Allocated database space low, %d%% free"` — mixed
- `"Adapter Error - %s processing stopped from node %s"` — behavioral

The parser treats all strings equally. There's no `Metadata.IsBehavioral` flag or format specifier detection. This means behavioral strings (which should NOT be sent to translators) would pollute the Data Bank.

**Fix required**: Add `HasFormatSpecifiers` flag to `EntryMetadata`. Detect `%d`, `%s`, `%x`, `%ld`, `%f`, `%%` patterns. Add `IsBehavioral` classification.

### 1.3 RC Parser: Encoding Blindness

`File.ReadAllText(filePath)` defaults to UTF-8. But real RC files on Windows are typically:
- Windows-1252 (Western European)
- UTF-16LE with BOM
- Code page 936 (Simplified Chinese) for the Translated version

The Translated `PCInstall.rc` is 160KB vs EN 111KB — a 44% increase typical of DBCS encoding. If read as UTF-8, Chinese characters would be garbled.

**Fix required**: Detect BOM encoding. Default to system codepage for `.rc` files. Add `--encoding` CLI flag.

### 1.4 No FHX Parser

The `AlarmWords.txt` format is simple tab-delimited Unicode text with 142 entries per file. It's the **only format with explicit "do NOT translate" flags** in the context metadata. The Data Bank has zero coverage for this format.

**Fix required**: New `FhxParser.cs`. Parse `@Key@\t"context"\tValue` format. Extract:
- Key (the `@Key@` identifier)
- Context (the quoted string — indicates source file and category)
- Value (the display text)
- `DoNotTranslate` flag from context containing `"do NOT translate"`

### 1.5 No AHC Parser

The `.ahc` format is XML with inline multi-language support. The actual data shows:
- 96 `<Content>` elements across 4 languages (en, jp, ru, zh)
- Most are **internal identifiers** (`txtLimits`, `txtAlarms`) — should NOT be in Data Bank
- Some are **display text** (Description: `"报警模块详细信息"`) — SHOULD be in Data Bank
- Labels use **library references** (`<Reference>GL.Library.S_*</Reference>`) — translations live elsewhere

**Fix required**: New `AhcParser.cs`. Must distinguish:
- Internal control names (Name attributes like `txtLimits`) → skip
- Library references → note as indirect, skip for Data Bank
- Actual `<Content>` text → extract with language detection
- Empty content → skip

### 1.6 Model Limitations

`LocalizedStringEntry` is too simple for real data:

| Missing Field | Why Needed | Real Data Example |
|---|---|---|
| `IsBehavioral` | Filter out strings that shouldn't be translated | `"Failed in CreateService. Error %d"` |
| `FormatSpecifiers` | Preserve `%d`, `%s` during translation | `"Error %d"` |
| `DoNotTranslate` | FHX has explicit flags | `"do NOT translate"` → `COMM_ALM` |
| `SourceCategory` | Classify strings by origin | `"alarm priority"`, `"alarm type"`, `"dialog"` |
| `LibraryReference` | AHC labels resolve indirectly | `<Reference>GL.Library.S_HH_Limit</Reference>` |
| `Encoding` | Track original file encoding | UTF-16LE vs UTF-8 vs Windows-1252 |
| `RelativePath` | Currently just filename — ambiguous | `RC/EN/PCInstall.rc` vs `RC/Translated/PCInstall.rc` |

### 1.7 Source.Path is Dead

Both parsers set `Path = Path.GetFileName(filePath)` — identical to `File`. The `Path` property never contains an actual path. Multiple files with the same name in different directories become ambiguous in the output.

### 1.8 Silent Exception Swallowing

Both `RcParser.Parse()` and `ResxParser.Parse()` catch all exceptions and discard them. A malformed file or encoding error produces zero entries with no warning. For production use, this is dangerous — the user would see "No localization entries found" with no indication of why.

### 1.9 No Coverage Analysis

The tool extracts entries but doesn't analyze:
- Which keys exist in EN but not in Translated (missing translations)
- Which keys exist in Translated but not in EN (orphaned translations)
- Per-locale completion percentages

The actual FHX data shows `@Batch@`, `@Campaign@`, `@Chronicle@` are untranslated in the Chinese version — this should be flagged.

---

## Part 2: Analyzers — Critical Gaps

### 2.1 LOC002 (Data Access): Still Has False Positives

GAPS.md says the `GetHashCode`/`GetType`/`ToString` issue was "fixed" by changing `Contains` to `StartsWith`. But `StartsWith("Get")` **still matches** `GetHashCode`, `GetType`, `ToString`, `GetAwaiter`, `GetEnumerator`. The fix is incomplete.

Similarly, `StartsWith("Db")` matches `Debug`, `Dbg`, `Disposable`.

**Fix**: Use exact match for known methods + semantic analysis to verify the receiver type implements an interface with a `Find`/`Get`/`Query` method.

### 2.2 LOC006 (StringComparison): Flags Invariant Methods

`ToLowerInvariant()` and `ToUpperInvariant()` are **intentionally culture-independent** — they exist precisely to avoid culture-dependent behavior. Flagging them is a false positive. The analyzer should only flag `ToLower()` and `ToUpper()` without parameters.

### 2.3 LOC007 (Plural Logic): Operator Precedence Bug

Line 56: `if (isCompareToZeroOrOne && IsCountOrSize(binary.Left) || IsCountOrSize(binary.Right))`

This evaluates as `(isCompareToZeroOrOne && IsCountOrSize(binary.Left)) || IsCountOrSize(binary.Right)` — meaning if the right operand is a count/size, the condition fires regardless of whether the left is a numeric literal. Any expression like `items.Count > 0 ? "..." : "..."` would false-positive.

### 2.4 LOC010 (Display String): No Semantic Type Checking

LOC010 checks `UiPropertyNames` (Text, Label, Title, etc.) but has **no semantic verification** that the object is actually a UI type. `myConfig.Text = "hello"` would be flagged even if `myConfig` is a config object, not a UI control.

The `IsUiType` check (line 198-201) only runs for `ObjectCreationExpression` — for assignments, it checks the property name alone. This means `button.Text = "OK"` and `config.Text = "setting"` are both flagged.

**Fix**: Use `SemanticModel.GetTypeInfo()` to verify the receiver type implements UI interfaces or inherits from UI base classes.

### 2.5 LOC010: Test Detection is Filename-Based

`IsTestCode()` checks if the filename contains "Test" or "Spec". This is fragile — a file named `TestHelper.cs` (non-test utility) would be excluded, while a test file named `MyFeature.cs` in a test project would not.

**Fix**: Check the containing namespace or assembly for test framework references (xUnit, NUnit, MSTest).

### 2.6 LOC010: Resource Reference Detection is Too Narrow

`IsResourceReference()` only checks for parent member access names `"Strings"`, `"Resources"`, `"Translations"`. The actual AHC data shows labels like `GL.Library.S_HH_Limit` — these use a `GL.Library` prefix that wouldn't be detected.

### 2.7 Analyzers Don't Cover Real Patterns

Looking at the actual l10n-files data, these patterns exist in the codebase but have no analyzer coverage:

| Pattern | Example from Real Data | Current Coverage |
|---|---|---|
| RC format specifiers in strings | `"Error %d"`, `"%s processing stopped"` | None — these are in resource files, not C# code |
| `@Key@` lookup patterns | `@CRITICAL@`, `@High Alarm@` | None |
| "do NOT translate" flags | `"do NOT translate"` context | None |
| Library reference patterns | `<Reference>GL.Library.S_*</Reference>` | None |
| Multi-language inline patterns | `<LanguageValue Name="zh">` | None |

### 2.8 Helpers Directory is Empty

The `src/Helpers/` directory exists but contains zero files. Common utilities like `StringHelper`, `EncodingDetector`, or `LocaleHelper` should be shared between DataBank.Cli and the Analyzers.

### 2.9 SarifCli Targets net10.0 Only

`SarifCli.cs` compiles only for `net10.0` (preview/unreleased). The analyzer targets `netstandard2.0`. This means the CLI tool can't run on any currently-supported .NET runtime (8.0, 9.0).

---

## Part 3: Priority Recommendations

### Immediate (Blockers)

1. **Add DIALOG parsing to RcParser** — the 306 dialog strings are the highest-value data currently being missed
2. **Add FHX parser** — simple format, high value, explicit "do NOT translate" metadata
3. **Fix encoding detection** — current code produces garbled output for non-UTF-8 RC files
4. **Add `IsBehavioral`/`FormatSpecifiers` to model** — filter out developer-facing strings
5. **Fix LOC002 false positives** — `GetHashCode`/`GetType`/`ToString` still matched
6. **Fix LOC006 false positives** — `ToLowerInvariant`/`ToUpperInvariant` flagged incorrectly

### Short-term (High Value)

7. **Add AHC parser** — XML-based, moderate complexity, multi-language inline
8. **Add relative path to `Source.Path`** — currently just filename, ambiguous
9. **Add coverage analysis** — detect missing translations across locales
10. **Fix LOC007 operator precedence bug** — line 56
11. **Add semantic type checking to LOC010** — reduce false positives on non-UI properties
12. **Make silent exceptions visible** — at minimum log warnings to stderr

### Medium-term (Architecture)

13. **Define `IParser` interface** — unify ResxParser, RcParser, FhxParser, AhcParser
14. **Add shared Helpers** — encoding detection, locale utilities
15. **Change SarifCli target to net8.0** — currently net10.0 only
16. **Add `--encoding` CLI flag** — override auto-detection
17. **Add `--verbose`/`--quiet` CLI flags** — control output verbosity

### Won't Fix (Accept Limitations)

- GRF binary parsing — requires OLE compound document library, low ROI for now
- DLL decompilation — requires ILSpy integration, separate tool
- Full semantic analysis for all analyzers — too expensive for Roslyn analyzer performance budget
