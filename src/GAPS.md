# Localization Analyzer Gaps

## Current Rules

| Rule | Description | Severity |
|------|-------------|----------|
| LOC001 | String literal in conditional expression | Warning |
| LOC002 | String literal in data access call | Warning |
| LOC003 | String literal in equality comparison | Warning |
| LOC004 | String Concatenation in Output | Warning |
| LOC005 | Hardcoded Date/Number Format | Warning |
| LOC006 | Missing StringComparison | Info |
| LOC007 | Hardcoded Plural Logic | Warning |
| LOC010 | Display string not localized | Info |
| LOC011 | String Interpolation in Localizable Context | Warning |
| LOC012 | Hardcoded DateTime Format | Warning |
| LOC013 | Dynamic Resource Key | Info |
| LOC014 | English-Only Pluralization | Warning |
| LOC015 | Punctuation Outside String | Info |

## CA Rule Integration (Opt-in)

The CLI tool can load CA1303-CA1311 globalization rules from `Microsoft.CodeAnalysis.NetAnalyzers` via the `--with-ca-rules` flag. These rules provide additional coverage for:
- CA1303: Do not pass literals as localized parameters
- CA1304: Specify CultureInfo
- CA1305: Specify IFormatProvider
- CA1307: Specify StringComparison for clarity
- CA1308: Normalize strings to uppercase
- CA1309: Use ordinal StringComparison
- CA1310: Specify StringComparison for correctness
- CA1311: Specify culture or use invariant for ToUpper/ToLower

## Gaps to Discuss

### Category 1: Strings that SHOULD be flagged (user-facing)

These are strings users see and need translation:

- **Exception messages** — shown in UI error dialogs
- **Validation messages** — "Please enter a value", "Invalid email"
- **Status messages** — "Saved successfully", "Loading..."
- **Button/dialog text** — already partially covered by LOC010
- **Tooltip text** — already partially covered by LOC010
- **Error messages in catch blocks** — user-facing errors
- **Confirmation dialogs** — "Are you sure you want to delete?"

### Category 2: Strings that should NOT be flagged (technical)

These are never shown to users:

- **URLs/paths** — "https://api.example.com", "/api/v1/users"
- **SQL query fragments** — "SELECT * FROM"
- **Regex patterns** — "^\\d{3}-\\d{4}$"
- **Format strings** — "{0:N2}", "{name}"
- **Constants/enums** — `const string ApiKey = "x"`
- **Log templates** — "User {userId} performed {action}"
- **Exception type names** — `nameof(InvalidOperationException)`
- **Property/field names** — used in reflection
- **Assembly/resource names** — "MyApp.Resources.Strings"
- **Serialization keys** — "[JsonProperty("name")]"
- **Debug symbols** — "#if DEBUG"
- **Compiler directives** — `#pragma warning disable`
- **Namespace declarations** — not user-facing
- **Using statements** — not user-facing

### Category 3: Ambiguous (needs context)

These depend on whether they're displayed to users:

- **String concatenation** — `"Hello " + name` (could be UI or logging)
- **String.Format** — could be UI message or debug output
- **Return statements** — `return "error"` (depends on caller)
- **Variable assignments** — `var msg = "done"` (depends on usage)
- **Attributes** — `[Description("text")]` (sometimes displayed)
- **Constructor arguments** — `new ErrorDialog("msg")` (UI-facing)
- **LINQ query strings** — depends on context
- **Async task names** — `Task.Run("ProcessData")` (not user-facing)

### Category 4: False positives — FIXED

- ~~LOC002 matches `GetHashCode`, `GetType`, `ToString` (not data access)~~ **Fixed**: changed `Contains` to `StartsWith`
- ~~LOC010 only catches specific UI property names (misses custom properties)~~ Partially addressed
- ~~LOC010 doesn't catch `ILogger.Log<T>` generic methods~~ **Fixed**: added suffix-based logger class detection
- **NEW**: LOC010 now skips `Debug.WriteLine` / `Trace.WriteLine` (not user-facing)

## Recommendations

1. ~~**Add exclusion list** for technical strings (URLs, SQL, regex, format strings)~~ — TODO
2. ~~**Expand LOC010** to catch more UI patterns (custom properties, generic logging)~~ — Partially done
3. ~~**Fix LOC002** to exclude common non-data-access methods~~ — Done
4. **Consider new rules** for exception messages, validation messages
5. **Mark Category 3 as configurable** — let teams opt-in to stricter checking
