using System.Text;
using System.Text.RegularExpressions;

namespace Sentiment_Analysis.Services.NLP;

public static class TextPreprocessor
{
    private static readonly Dictionary<string, string> Contractions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "isn't", "is not" },
        { "isnt", "is not" },
        { "aren't", "are not" },
        { "arent", "are not" },
        { "wasn't", "was not" },
        { "wasnt", "was not" },
        { "weren't", "were not" },
        { "werent", "were not" },
        { "haven't", "have not" },
        { "havent", "have not" },
        { "hasn't", "has not" },
        { "hasnt", "has not" },
        { "hadn't", "had not" },
        { "hadnt", "had not" },
        { "won't", "will not" },
        { "wont", "will not" },
        { "wouldn't", "would not" },
        { "wouldnt", "would not" },
        { "don't", "do not" },
        { "dont", "do not" },
        { "doesn't", "does not" },
        { "doesnt", "does not" },
        { "didn't", "did not" },
        { "didnt", "did not" },
        { "can't", "cannot" },
        { "cant", "cannot" },
        { "couldn't", "could not" },
        { "couldnt", "could not" },
        { "shouldn't", "should not" },
        { "shouldnt", "should not" },
        { "i'm", "i am" },
        { "im", "i am" },
        { "it's", "it is" },
        { "you're", "you are" },
        { "they're", "they are" },
        { "we're", "we are" }
    };

    public static string ExpandContractions(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var words = text.Split(' ');
        var sb = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            var clean = words[i].Trim();
            if (Contractions.TryGetValue(clean, out var expanded))
            {
                sb.Append(expanded).Append(' ');
            }
            else
            {
                sb.Append(words[i]).Append(' ');
            }
        }
        return sb.ToString().Trim();
    }

    public static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var expanded = ExpandContractions(text);
        // Replace multiple whitespace with single space
        return Regex.Replace(expanded, @"\s+", " ").Trim();
    }

    public static List<string> SplitIntoClauses(string text)
    {
        var normalized = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized)) return new List<string>();

        // Split by contrastive conjunctions or clause boundaries
        // Conjunctions: but, however, although, yet, nevertheless, though, whereas, on the other hand
        var pattern = @"(?<=[.!?])\s+|\s+(?=but\b|however\b|although\b|yet\b|nevertheless\b|whereas\b|though\b|except that\b)";
        var rawClauses = Regex.Split(normalized, pattern, RegexOptions.IgnoreCase);

        var clauses = new List<string>();
        foreach (var clause in rawClauses)
        {
            var trimmed = clause.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                clauses.Add(trimmed);
            }
        }

        if (clauses.Count == 0 && !string.IsNullOrWhiteSpace(normalized))
        {
            clauses.Add(normalized);
        }

        return clauses;
    }

    public static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        var normalized = NormalizeText(text).ToLowerInvariant();
        return Regex.Split(normalized, @"[^\p{L}\p{N}']+")
                    .Where(w => w.Length > 0)
                    .ToArray();
    }
}
