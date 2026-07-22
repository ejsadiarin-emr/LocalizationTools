using System;

namespace TestCodebase.Globalization
{
    /// <summary>
    /// LOC006: Missing StringComparison parameter
    /// LOC007: Hardcoded plural logic
    /// LOC014: English-only pluralization
    /// </summary>
    public class TextProcessor
    {
        // LOC006: String.Contains without StringComparison
        public bool ContainsWord(string text, string word)
        {
            return text.Contains(word);
        }

        // LOC006: String.StartsWith without StringComparison
        public bool HasPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix);
        }

        // LOC006: String.EndsWith without StringComparison
        public bool HasSuffix(string text, string suffix)
        {
            return text.EndsWith(suffix);
        }

        // LOC006: String.IndexOf without StringComparison
        public int FindPosition(string text, string search)
        {
            return text.IndexOf(search);
        }

        // LOC006: String.Equals without StringComparison
        public bool IsMatch(string a, string b)
        {
            return a.Equals(b);
        }

        // LOC006: ToLower without culture
        public string ToLowerCase(string text)
        {
            return text.ToLower();
        }

        // ACCEPTABLE: StringComparison provided
        public bool ContainsWordSafe(string text, string word)
        {
            return text.Contains(word, StringComparison.Ordinal);
        }

        // LOC007 + LOC014: Hardcoded plural logic with ternary
        public string FormatItemCount(int count)
        {
            return count == 1 ? "1 item" : count + " items";
        }

        // LOC007 + LOC014: Hardcoded plural logic with if/else
        public string FormatUserCount(int count)
        {
            string result;
            if (count == 1)
            {
                result = "1 user";
            }
            else
            {
                result = count + " users";
            }
            return result;
        }

        // LOC014: Concatenation with count in output
        public void PrintItemCount(int count)
        {
            Console.WriteLine("You have " + count + " items in your cart");
        }
    }
}
