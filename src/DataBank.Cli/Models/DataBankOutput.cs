namespace DataBank.Cli.Models;

public class DataBankOutput
{
    public int Version { get; set; } = 1;
    public string Generated { get; set; } = DateTime.UtcNow.ToString("o");
    public List<LocalizedStringEntry> Entries { get; set; } = [];
}
