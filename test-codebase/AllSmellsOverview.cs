using System;

namespace TestCodebase
{
    /// <summary>
    /// Comprehensive overview of all localization code smells detected by this tool.
    /// Run the analyzer against this directory to see all rules in action:
    ///
    ///   LocalizationAnalyzers test-codebase/ results.sarif
    ///   LocalizationAnalyzers test-codebase/ results.sarif --with-ca-rules
    ///
    /// Each rule id is listed in comments below the offending code.
    /// </summary>
    public class AllSmellsOverview
    {
        // =========================================================================
        // LOC001 — String in conditional
        // Breaks when translated because word order differs across languages.
        // =========================================================================
        public bool CheckStatus(string input)
        {
            if (input == "active") { return true; }   // LOC001
            return false;
        }

        // =========================================================================
        // LOC003 — String in equality comparison
        // Same issue as LOC001: literal used for behavioral branching.
        // =========================================================================
        public bool IsAdmin(string role)
        {
            return role == "admin";   // LOC003
        }

        // =========================================================================
        // LOC004 — String concatenation in output
        // Concatenation fragments can't be reordered by translators.
        // =========================================================================
        public void LogSomething(string user, string action)
        {
            Console.WriteLine("User " + user + " did " + action);   // LOC004
        }

        // =========================================================================
        // LOC005 — Hardcoded date/number format
        // "MM/dd/yyyy" is US-centric; Europe uses "dd/MM/yyyy".
        // =========================================================================
        public string FormatDate(DateTime dt)
        {
            return dt.ToString("MM/dd/yyyy");   // LOC005
        }

        // =========================================================================
        // LOC006 — Missing StringComparison
        // Default comparison is culture-sensitive, leading to surprising results.
        // =========================================================================
        public bool ContainsWord(string text, string word)
        {
            return text.Contains(word);   // LOC006
        }

        // =========================================================================
        // LOC007 — Hardcoded plural logic
        // "count == 1 ? singular : plural" only works for English.
        // =========================================================================
        public string PluralTernary(int count)
        {
            return count == 1 ? "1 file" : count + " files";   // LOC007
        }

        // =========================================================================
        // LOC010 — Display string not localized
        // Hardcoded string assigned to a UI text property.
        // =========================================================================
        public void SetLabel(LabelWidget label)
        {
            label.Text = "Click here to continue";   // LOC010
        }

        // =========================================================================
        // LOC011 — String interpolation in localizable context
        // $"..." prevents translators from reordering words.
        // =========================================================================
        public string GetGreeting(string name, Localizer loc)
        {
            return loc[$"Hello {name}"];   // LOC011
        }

        // =========================================================================
        // LOC012 — Hardcoded datetime format without CultureInfo
        // DateTime.ToString with format but no CultureInfo.
        // =========================================================================
        public string FormatTime(DateTime dt)
        {
            return dt.ToString("hh:mm:ss");   // LOC012
        }

        // =========================================================================
        // LOC013 — Dynamic resource key
        // Computed keys can't be statically verified by translation tools.
        // =========================================================================
        public string GetError(int code, Localizer loc)
        {
            return loc["Error_" + code];   // LOC013
        }

        // =========================================================================
        // LOC014 — English-only pluralization
        // if/else plural logic doesn't work for languages with complex plurals.
        // =========================================================================
        public string PluralIfElse(int count)
        {
            string text;
            if (count == 1) { text = "item"; } else { text = "items"; }   // LOC014
            return count + " " + text;
        }

        // =========================================================================
        // LOC015 — Punctuation outside translatable string
        // Punctuation rules differ by locale (e.g., spacing around colons in French).
        // =========================================================================
        public string AddPunctuation(string label)
        {
            return label + ":";   // LOC015
        }
    }

    // Minimal stubs so the test codebase compiles
    public class LabelWidget { public string Text { get; set; } }
    public class Localizer { public string this[string k] => k; public string this[string k, params object[] a] => k; }
}
