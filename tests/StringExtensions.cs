using System.Text.RegularExpressions;

public static class StringExtensions
{
    /// <summary>
    /// Compares the string against a given pattern.
    /// </summary>
    /// <param name = "input">The input string.</param>
    /// <param name = "pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character.</param>
    /// <param name = "substring">Search pattern as substring if input string</param>
    /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
    public static bool Like(this string input, string pattern, bool substring = false)
    {
        var regexPattern = Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".");
        if (!substring)
            regexPattern= "^" + regexPattern + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return regex.IsMatch(input);
    }
}