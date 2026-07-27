namespace DataBank.Cli.Models;

public class EntryMetadata
{
    public string? Comment { get; set; }
    public int? RcId { get; set; }
    public string? RcDefine { get; set; }
    public bool IsBehavioral { get; set; }
    public List<string> FormatSpecifiers { get; set; } = [];
    public bool DoNotTranslate { get; set; }
    public bool IsTranslated { get; set; }
    public TranslationStatus TranslationStatus { get; set; } = TranslationStatus.Untranslated;
}
