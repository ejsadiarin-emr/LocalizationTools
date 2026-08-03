namespace DataBank.Cli.Models;

public class DataBankOutput
{
    public int Version { get; set; } = 3;
    public string Generated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? BasePath { get; set; }
    public List<LocalizedStringEntry> Entries { get; set; } = [];
    public TranslationSummary? TranslationSummary { get; set; }
}
