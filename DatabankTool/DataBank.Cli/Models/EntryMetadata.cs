namespace DataBank.Cli.Models;

public class EntryMetadata
{
    public string? Comment { get; set; }
    public List<string> FormatSpecifiers { get; set; } = [];
    public bool DoNotTranslate { get; set; }
    public bool IsTranslated { get; set; }

    public TranslationStatus GetDerivedStatus()
    {
        if (DoNotTranslate) return TranslationStatus.DoNotTranslate;
        if (!IsTranslated) return TranslationStatus.Untranslated;
        return TranslationStatus.Translated;
    }
}
