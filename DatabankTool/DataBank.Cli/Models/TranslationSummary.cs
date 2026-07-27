namespace DataBank.Cli.Models;

public class TranslationSummary
{
    public TranslationCounts Overall { get; set; } = new();
    public List<LocaleTranslationCounts> ByLocale { get; set; } = [];
}

public class TranslationCounts
{
    public int Translated { get; set; }
    public int Untranslated { get; set; }
    public int DoNotTranslate { get; set; }
    public int NeedsReview { get; set; }
}

public class LocaleTranslationCounts
{
    public string Locale { get; set; } = string.Empty;
    public int Translated { get; set; }
    public int Untranslated { get; set; }
    public int DoNotTranslate { get; set; }
    public int NeedsReview { get; set; }
}
