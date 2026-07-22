using System;

namespace TestCodebase.Globalization
{
    /// <summary>
    /// CA rules: CA1303, CA1305, CA1307, CA1308, CA1309, CA1310, CA1311
    /// These require --with-ca-rules flag to be detected.
    /// </summary>
    public class CaRuleExamples
    {
        // CA1303: Do not pass literals as localized parameters
        // (Passing string literals to methods with string parameter names suggesting localization)
        public void MethodWithLocalizedString(string localizableParam)
        {
            // This method receives a localizable parameter
        }

        public void CallWithLiteral()
        {
            MethodWithLocalizedString("This is a literal that should be localized");
        }

        // CA1305: Specify IFormatProvider
        public string FormatNumber(double value)
        {
            return value.ToString();
        }

        // CA1305: Specify IFormatProvider for string.Format
        public string FormatMessage(int count, string name)
        {
            return string.Format("{0} has {1} items", name, count);
        }

        // CA1307: Specify StringComparison for clarity
        public bool FindIgnoreCase(string text, string search)
        {
            return text.IndexOf(search) >= 0;
        }

        // CA1308: Normalize strings to uppercase
        public string NormalizeEmail(string email)
        {
            return email.ToLower();
        }

        // CA1309: Use ordinal StringComparison
        public bool CompareStrings(string a, string b)
        {
            return string.Compare(a, b) == 0;
        }

        // CA1311: Specify culture or use invariant for ToUpper/ToLower
        public string ToUpper(string text)
        {
            return text.ToUpper();
        }

        // ACCEPTABLE patterns (should NOT trigger CA rules)
        public string AcceptableFormat(double value)
        {
            return value.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }

        public string AcceptableCompare(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        public string AcceptableNormalize(string text)
        {
            return text.ToUpperInvariant();
        }
    }
}
