using System;

namespace TestCodebase.Formatting
{
    /// <summary>
    /// LOC005: Hardcoded date/number format strings
    /// LOC012: Hardcoded datetime format without CultureInfo
    /// </summary>
    public class ReportFormatter
    {
        // LOC005 + LOC012: Hardcoded date format in ToString
        public string FormatDate(DateTime date)
        {
            return date.ToString("MM/dd/yyyy");
        }

        // LOC005 + LOC012: Hardcoded time format
        public string FormatTime(DateTime date)
        {
            return date.ToString("hh:mm:ss tt");
        }

        // LOC012: Hardcoded datetime format with time separator
        public string FormatDateTime(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // LOC005: Hardcoded number format
        public string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2");
        }

        // LOC012: ParseExact without CultureInfo
        public DateTime ParseDate(string input)
        {
            return DateTime.ParseExact(input, "MM/dd/yyyy", null);
        }

        // LOC012: TryParseExact without CultureInfo
        public bool TryParseDate(string input, out DateTime result)
        {
            return DateTime.TryParseExact(input, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out result);
        }

        // ACCEPTABLE: Standard format specifier (single letter)
        public string FormatDateStandard(DateTime date)
        {
            return date.ToString("D");
        }

        // ACCEPTABLE: CultureInfo provided
        public string FormatDateWithCulture(DateTime date)
        {
            return date.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
