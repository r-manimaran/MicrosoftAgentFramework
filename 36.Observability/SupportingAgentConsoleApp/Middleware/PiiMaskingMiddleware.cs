using System.Text.RegularExpressions;

namespace SupportingAgentConsoleApp.Middleware;

public static class PiiMaskingMiddleware
{
    private static readonly (Regex Pattern, string Replacement)[] Rules = [
        (new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"), "[EMAIL]"),
        (new Regex(@"\b(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b"), "[PHONE]"),
        (new Regex(@"\b(?:\d[ -]*?){13,16}\b"), "[CARD]"),
        (new Regex(@"\b\d{3}-\d{2}-\d{4}\b"), "[SSN]"),
        (new Regex(@"(?i)api[_\s-]?key[\s:=]+\S+"), "[API_KEY]"),
        ];

    public static string Mask(string text)
    {
        if(string.IsNullOrEmpty(text)) return text;
        foreach (var (pattern, replacement) in Rules)
        {
            text = pattern.Replace(text, replacement);
        }
        return text;
    }
}
