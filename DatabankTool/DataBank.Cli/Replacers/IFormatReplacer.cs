namespace DataBank.Cli.Replacers;

public interface IFormatReplacer
{
    string? ReplaceLine(string line, string oldValue, string newValue);
}
