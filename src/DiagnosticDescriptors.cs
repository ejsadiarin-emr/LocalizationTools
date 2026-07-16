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
}
