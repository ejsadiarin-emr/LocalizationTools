# Localization Code Smells & Standards Research

## Purpose

Research on localization-related code smells and standards that can enhance the LocalizationAnalyzers tool.

---

## 1. Existing .NET Globalization Analyzer Rules (Microsoft.CodeAnalysis.NetAnalyzers)

These are the built-in CA rules already in the .NET SDK ([source](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/globalization-warnings)):

| Rule | What it detects | Gap? |
|------|----------------|------|
| **CA1303** | Do not pass literals as localized parameters | Existing |
| **CA1304** | Specify CultureInfo | Existing |
| **CA1305** | Specify IFormatProvider | Existing |
| **CA1307** | Specify StringComparison for clarity | Existing |
| **CA1308** | Normalize strings to uppercase | Existing |
| **CA1309** | Use ordinal StringComparison | Existing |
| **CA1310** | Specify StringComparison for correctness | Existing |
| **CA1311** | Specify culture or use invariant for ToUpper/ToLower | Existing |
| **CA2101** | Specify marshalling for P/Invoke strings | Existing |

---

## 2. High-Value Code Smells to Detect (Gaps / Enhancement Opportunities)

### A. String Concatenation for User-Facing Text

**Why it's a problem:** Sentence structure varies by language. Concatenating fragments breaks translation because word order, adjective agreement, and declensions differ across languages.

- [Source: Microsoft Globalization - String Concatenation](https://learn.microsoft.com/en-us/globalization/internationalization/concatenation)
- [Source: InDutch - Why string concatenation breaks localization](https://www.indutch.com/blog/2026/3/10/why-string-concatenation-breaks-your-localization-and-what-to-do-instead)
- [Source: Lingoport - Best Practices](https://lingoport.com/blog/9-best-practices-for-internationalization)

**Detectable patterns:**

- `"string " + variable + " string"` in user-facing contexts
- String concatenation before passing to UI elements, `MessageBox`, logging, etc.
- `StringBuilder` assembly of user-visible messages from multiple fragments

### B. String Interpolation in Localized Contexts

**Why it's a problem:** `$"Hello {name}"` prevents translators from reordering words. Many languages have different word order than English.

- [Source: Medium - String Interpolation Seemed Harmless, Localization Broke](https://medium.com/dot-net-sql-learning/string-interpolation-seemed-harmless-localization-broke-e061ba5630d4)
- [Source: i18next Best Practices](https://www.i18next.com/principles/best-practices)

**Detectable patterns:**

- `$"..."` used directly in UI strings or passed to `IStringLocalizer`
- Interpolated strings inside `_()` or `localizer["..."]` calls

### C. Hardcoded User-Facing Strings

**Why it's a problem:** Strings should be externalized to resource files for translation.

- [Source: CredibleSoft - Common Localization Pitfalls](https://crediblesoft.com/localization-internationalization-testing-best-practices-tools-pitfalls)
- [Source: dotnet-guide.com - Common Pitfalls](https://www.dotnet-guide.com/localization.html)

**Detectable patterns:**

- String literals passed directly to `MessageBox.Show()`, `ToastNotification`, or UI binding without `IStringLocalizer`
- `throw new Exception("message literal")` instead of using localized resources
- Hardcoded strings in ASP.NET MVC views without `@Localizer["key"]`

### D. Culture-Sensitive Formatting Without Explicit Culture

**Why it's a problem:** Date/time/number formatting changes per locale. Using `.ToString()` without culture info produces different results per machine.

- [Source: Microsoft CA1305](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1305)
- [Source: dotnet-guide.com - Building Multilingual Applications](https://www.dotnet-guide.com/globalization.html)

**Detectable patterns:**

- `DateTime.Now.ToString("MM/dd/yyyy")` — hardcoded format
- `someInt.ToString()` or `someDecimal.ToString("C")` without `IFormatProvider`
- `string.Format("{0:C}", price)` without `CultureInfo`
- `decimal.Parse("123.45")` without culture (will fail on cultures where `,` is decimal separator)

### E. Hardcoded Date/Time Formats

**Why it's a problem:** Date formats are locale-dependent. `MM/dd/yyyy` is US-centric; Europe uses `dd/MM/yyyy`.

- [Source: dotnet-guide.com - Common Pitfalls](https://www.dotnet-guide.com/localization.html)
- [Source: Microsoft Globalization](https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization)

**Detectable patterns:**

- `DateTime.Parse("01/02/2024")` with hardcoded format
- `DateTime.ToString("MM/dd/yyyy")` without `CultureInfo`
- Use of `DateTime.TryParseExact` with `CultureInfo.InvariantCulture` when user-facing

### F. String Comparison Without Explicit StringComparison

**Why it's a problem:** Default string comparison is culture-sensitive, leading to surprising results with accented characters.

- [Source: CA1307, CA1309, CA1310](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/globalization-warnings)
- [Source: Meziantou - String comparisons are harder than it seems](https://www.meziantou.net/string-comparisons-are-harder-than-it-seems.htm)

**Detectable patterns:**

- `string.Equals(a, b)` without `StringComparison`
- `string.Contains("text")` without `StringComparison`
- `string.IndexOf("text")` without `StringComparison`
- `string.StartsWith("text")` / `string.EndsWith("text")` without `StringComparison`

### G. CurrentCulture vs CurrentUICulture Confusion

**Why it's a problem:** `CurrentCulture` controls formatting; `CurrentUICulture` controls resource loading. Mixing them up causes wrong resources or wrong formatting.

- [Source: Stack Overflow - CA1305 CurrentCulture vs CurrentUICulture](https://stackoverflow.com/questions/13010555/ca1305-verbosity-when-specifying-culture)
- [Source: dotnet-guide.com](https://www.dotnet-guide.com/localization.html)

**Detectable patterns:**

- Setting `Thread.CurrentThread.CurrentCulture` when you meant to set `CurrentUICulture` for resource loading
- Setting `CultureInfo.CurrentCulture = culture` without also setting `CurrentUICulture`

### H. Resource File / .resx Anti-Patterns

**Why it's a problem:** Poor resource organization leads to maintenance issues and missing translations.

- [Source: Microsoft - Provide localized resources](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/provide-resources)
- [Source: Reddit - Prevent hardcoded strings with IStringLocalizer](https://www.reddit.com/r/dotnet/comments/162qyfn/prevent_using_hard_coded_string_when_using/)

**Detectable patterns:**

- Resource keys that are dynamic/computed (`localizer[$"key_{suffix}"]`) — can't be statically verified
- Using string literals as resource keys without constants (`localizer["SomeKey"]` vs `localizer[ResourceKeys.SomeKey]`)
- Missing fallback culture resource file (e.g., only `Resources.fr-FR.resx` but no `Resources.fr.resx`)

### I. Text Truncation / UI Layout Issues

**Why it's a problem:** Translated text is often 30-40% longer than English. Fixed-width UI elements cause truncation.

- [Source: Acclaro - Common Localization Bugs](https://www.acclaro.com/resources/ensure-localized-product-free-pesky-bugs)
- [Source: qaskills.sh - Internationalization Testing Guide](https://qaskills.sh/blog/internationalization-testing-i18n-guide)

**Detectable patterns:**

- Fixed-width containers without text wrapping
- Hardcoded `MaxLength` or character limits on user-facing text
- Truncation logic (`text.Substring(0, 50)`) on user-visible strings

### J. RTL (Right-to-Left) Layout Issues

**Why it's a problem:** Arabic, Hebrew, Urdu, and Farsi require RTL layout. Physical CSS properties (margin-left) break in RTL.

- [Source: phazurlabs/ux-ui-mastery - Internationalization Patterns](https://github.com/phazurlabs/ux-ui-mastery/blob/main/skills/cross-cultural-i18n-ux/references/internationalization-patterns.md)
- [Source: lingual.dev - i18n Checklist Part 2](https://lingual.dev/blog/checklist-for-your-i18n-efforts-part-2)

**Detectable patterns:**

- Physical layout properties (`margin-left`, `padding-right`) in XAML/WPF/WinForms without RTL consideration
- Hardcoded `FlowDirection.LeftToRight` without considering locale

### K. Encoding / Unicode Issues

**Why it's a problem:** Non-UTF-8 encoding causes garbled characters for non-Latin scripts.

- [Source: Microsoft - Globalization](https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization)
- [Source: lingual.dev - Unicode checklist](https://lingual.dev/blog/checklist-for-your-i18n-efforts-part-2)

**Detectable patterns:**

- `Encoding.ASCII` or `Encoding.Default` instead of `Encoding.UTF8`
- `StreamReader`/`StreamWriter` without explicit encoding
- `File.ReadAllText()` without specifying UTF-8 encoding

### L. Pluralization Anti-Patterns

**Why it's a problem:** English has simple singular/plural; many languages have complex plural rules (e.g., Polish has 3 forms, Arabic has 6).

- [Source: Microsoft Globalization](https://learn.microsoft.com/en-us/globalization/internationalization/concatenation)
- [Source: i18next Best Practices](https://www.i18next.com/principles/best-practices)

**Detectable patterns:**

- `"You have " + count + " items"` — can't handle plural forms
- Conditional logic like `count == 1 ? "item" : "items"` — only works for English
- No ICU MessageFormat usage for pluralization

---

## 3. Potential New Analyzer Rules (Prioritized by Impact)

| Priority | Rule Name | Description | Rationale |
|----------|-----------|-------------|-----------|
| **High** | String interpolation in localizable context | Detect `$"..."` passed to localizer/UI | Breaks word order for translators |
| **High** | String concatenation for user-facing text | Detect `+` concatenation in UI strings | Most common i18n bug |
| **High** | Hardcoded date/time formats | Detect format strings like `"MM/dd/yyyy"` | Locale-dependent, breaks in other regions |
| **High** | Missing IFormatProvider in ToString | Detect `.ToString()` on numeric/date types | Already partially covered by CA1305 but can be enhanced |
| **Medium** | Dynamic resource key | Detect computed/localizer key strings | Can't verify translations statically |
| **Medium** | CurrentCulture/CurrentUICulture confusion | Detect misassignment | Common source of subtle bugs |
| **Medium** | Encoding without UTF-8 | Detect non-UTF-8 encoding usage | Causes garbled text |
| **Medium** | English-only pluralization | Detect `"item" + count` patterns | Breaks in most non-English languages |
| **Low** | Punctuation outside translatable string | Detect `"text" + ":"` or `"text" + "."` | Punctuation rules differ by locale |
| **Low** | Hardcoded currency/symbol | Detect `"$"` or `"€"` literals | Should use culture-aware formatting |

---

## 4. Key Sources for Reference

1. **Microsoft Globalization Docs** — https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization
2. **CA Rules Overview** — https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/globalization-warnings
3. **String Concatenation Issues** — https://learn.microsoft.com/en-us/globalization/internationalization/concatenation
4. **i18next Best Practices** — https://www.i18next.com/principles/best-practices
5. **gosmopolitan (Go linter for i18n)** — https://github.com/xen0n/gosmopolitan (reference for rule ideas)
6. **Lingual.dev i18n Checklist** — https://lingual.dev/blog/checklist-for-your-i18n-efforts-part-1/
7. **Internationalization Patterns** — https://github.com/phazurlabs/ux-ui-mastery/blob/main/skills/cross-cultural-i18n-ux/references/internationalization-patterns.md
8. **Microsoft Internationalization Testing** — https://learn.microsoft.com/en-us/globalization/testing/how-to-perform-internationalization-testing
9. **Acclaro Localization Bugs** — https://www.acclaro.com/resources/ensure-localized-product-free-pesky-bugs

---

## 5. Recommendation

The **highest-impact additions** would be:

1. **String interpolation in localizable context** — very common, easy to detect via Roslyn
2. **String concatenation for user-facing text** — biggest source of translation bugs
3. **Hardcoded date/time format patterns** — regex-matchable, high-value
4. **Dynamic resource keys** — prevents static verification of translations
5. **English-only pluralization patterns** — detectable via simple pattern matching

These would complement the existing CA1303-CA1311 rules by catching patterns that the built-in analyzers don't cover.
