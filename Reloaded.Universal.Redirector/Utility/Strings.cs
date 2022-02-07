using System;

namespace Reloaded.Universal.Redirector.Utility;

public static class Strings
{
    /// <summary>
    /// Replaces the first occurrence of a piece of text in a string.
    /// </summary>
    /// <param name="text">The string to trim.</param>
    /// <param name="search">The text from start to remove.</param>
    /// <param name="comparison">The comparison to perform.</param>
    public static string TrimStart(this string text, string search, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        int position = text.IndexOf(search, comparison);
        return position < 0 ? text : text.Substring(position + search.Length);
    }
}