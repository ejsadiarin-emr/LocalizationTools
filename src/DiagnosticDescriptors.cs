using Microsoft.CodeAnalysis;

namespace LocalizationAnalyzers;

/// <summary>
/// Diagnostic descriptors for localization analyzers.
/// </summary>
public static class DiagnosticDescriptors
{
    // LOC001 — String in Conditional
    private static readonly LocalizableString Loc001Title = "String literal in conditional expression";
    private static readonly LocalizableString Loc001MessageFormat = "String literal '{0}' used in conditional; translate to break when localized";
    private static readonly LocalizableString Loc001Description = "String literals in conditionals break when translated. Use resource keys instead.";

    public static readonly DiagnosticDescriptor LOC001 = new(
        id: "LOC001",
        title: Loc001Title,
        messageFormat: Loc001MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Loc001Description);

    // LOC002 — String in Data Access
    private static readonly LocalizableString Loc002Title = "String literal in data access call";
    private static readonly LocalizableString Loc002MessageFormat = "String literal '{0}' passed to data-access method; use a resource key or constant";
    private static readonly LocalizableString Loc002Description = "String literals in data access calls break when translated. Use resource keys or constants.";

    public static readonly DiagnosticDescriptor LOC002 = new(
        id: "LOC002",
        title: Loc002Title,
        messageFormat: Loc002MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Loc002Description);

    // LOC003 — String in Equality Comparison
    private static readonly LocalizableString Loc003Title = "String literal in equality comparison";
    private static readonly LocalizableString Loc003MessageFormat = "String literal '{0}' used in equality check; translate to break when localized";
    private static readonly LocalizableString Loc003Description = "String literals in equality comparisons break when translated. Use resource keys or constants.";

    public static readonly DiagnosticDescriptor LOC003 = new(
        id: "LOC003",
        title: Loc003Title,
        messageFormat: Loc003MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Loc003Description);

    // LOC010 — Display String Not Localized
    private static readonly LocalizableString Loc010Title = "Display string not localized";
    private static readonly LocalizableString Loc010MessageFormat = "Display string '{0}' not routed through Localize()";
    private static readonly LocalizableString Loc010Description = "Display strings shown to users should be routed through Localize() for translation.";

    public static readonly DiagnosticDescriptor LOC010 = new(
        id: "LOC010",
        title: Loc010Title,
        messageFormat: Loc010MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Loc010Description);

    // LOC004 — String Concatenation in Output
    private static readonly LocalizableString Loc004Title = "String concatenation in output context";
    private static readonly LocalizableString Loc004MessageFormat = "String concatenation '{0}' in output context; use interpolation or resource keys for localizable text";
    private static readonly LocalizableString Loc004Description = "String concatenation in output contexts produces untranslatable fragments. Use string interpolation or resource keys instead.";

    public static readonly DiagnosticDescriptor LOC004 = new(
        id: "LOC004",
        title: Loc004Title,
        messageFormat: Loc004MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Loc004Description);

    // LOC005 — Hardcoded Date/Number Format
    private static readonly LocalizableString Loc005Title = "Hardcoded date/number format string";
    private static readonly LocalizableString Loc005MessageFormat = "Hardcoded format string '{0}' is locale-dependent; use CultureInfo-aware formatting";
    private static readonly LocalizableString Loc005Description = "Hardcoded date/number format strings produce locale-dependent output. Use CultureInfo-aware formatting instead.";

    public static readonly DiagnosticDescriptor LOC005 = new(
        id: "LOC005",
        title: Loc005Title,
        messageFormat: Loc005MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Loc005Description);

    // LOC006 — Missing StringComparison
    private static readonly LocalizableString Loc006Title = "Missing StringComparison parameter";
    private static readonly LocalizableString Loc006MessageFormat = "String method '{0}' called without StringComparison; specify StringComparison for locale-safe behavior";
    private static readonly LocalizableString Loc006Description = "String methods without StringComparison use culture-dependent defaults, which can cause unexpected behavior across locales.";

    public static readonly DiagnosticDescriptor LOC006 = new(
        id: "LOC006",
        title: Loc006Title,
        messageFormat: Loc006MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Loc006Description);

    // LOC007 — Hardcoded Plural Logic
    private static readonly LocalizableString Loc007Title = "Hardcoded pluralization logic";
    private static readonly LocalizableString Loc007MessageFormat = "Hardcoded plural logic on '{0}'; use ICU MessageFormat or resource-based plural rules";
    private static readonly LocalizableString Loc007Description = "Hardcoded pluralization (e.g., count == 1 ? \"item\" : \"items\") doesn't work for languages with complex plural forms (Russian, Arabic, etc.).";

    public static readonly DiagnosticDescriptor LOC007 = new(
        id: "LOC007",
        title: Loc007Title,
        messageFormat: Loc007MessageFormat,
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Loc007Description);
}
