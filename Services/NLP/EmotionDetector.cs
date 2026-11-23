using System.Text.RegularExpressions;

namespace Sentiment_Analysis.Services.NLP;

public class EmotionDetector
{
    private static readonly Dictionary<string, string[]> EmotionLexicon = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "Happiness", new[]
            {
                "happy", "delighted", "joyful", "pleased", "thrilled", "glad", "cheerful",
                "enthusiastic", "wonderful", "great", "loved", "smile", "smiling", "blessed",
                "grateful", "ecstatic", "enjoyed"
            }
        },
        {
            "Satisfaction", new[]
            {
                "satisfied", "satisfactory", "content", "fulfilled", "met expectations", "recommended",
                "impressive", "commendable", "appreciated", "thankful", "resolved", "helpful",
                "relief", "relieved", "smooth", "flawless", "professional", "excellent", "good"
            }
        },
        {
            "Frustration", new[]
            {
                "frustrated", "frustrating", "annoyed", "annoying", "irritated", "irritating",
                "waste of time", "fed up", "tired of waiting", "tedious", "exhausting", "helpless",
                "disorganized", "useless", "confusing", "unresponsive", "stuck", "terrible wait"
            }
        },
        {
            "Anger", new[]
            {
                "angry", "furious", "enraged", "mad", "outraged", "disgusted", "offensive",
                "insulted", "rude", "scam", "rip-off", "cheated", "unacceptable", "appalled",
                "horrible", "worst", "hate", "abusive", "hostile", "disgrace"
            }
        },
        {
            "Sadness", new[]
            {
                "sad", "unhappy", "depressed", "disappointed", "disappointing", "sorrow",
                "regrettable", "heartbroken", "upset", "distressed", "hopeless", "crying",
                "unfortunate", "dismayed", "neglected", "abandoned"
            }
        }
    };

    private static readonly HashSet<string> Negations = new(StringComparer.OrdinalIgnoreCase)
    {
        "not", "no", "never", "without", "hardly", "barely", "scarcely", "cannot", "cant",
        "dont", "doesnt", "didnt", "isnt", "wasnt", "arent", "werent"
    };

    public (string primaryEmotion, Dictionary<string, double> distribution) DetectEmotions(string text, string overallSentiment)
    {
        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "Happiness", 0.0 },
            { "Satisfaction", 0.0 },
            { "Frustration", 0.0 },
            { "Anger", 0.0 },
            { "Sadness", 0.0 }
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            return ("Neutral", scores);
        }

        var tokens = TextPreprocessor.Tokenize(text);
        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            bool isNegated = false;
            for (int k = Math.Max(0, i - 3); k < i; k++)
            {
                if (Negations.Contains(tokens[k]))
                {
                    isNegated = true;
                    break;
                }
            }

            foreach (var kvp in EmotionLexicon)
            {
                var emotion = kvp.Key;
                var words = kvp.Value;

                if (words.Contains(token))
                {
                    if (isNegated)
                    {
                        // If positive emotion is negated (e.g. "not happy"), convert toward Frustration/Sadness
                        if (emotion == "Happiness" || emotion == "Satisfaction")
                        {
                            scores["Frustration"] += 0.8;
                            scores["Sadness"] += 0.5;
                        }
                    }
                    else
                    {
                        scores[emotion] += 1.0;
                    }
                }
            }
        }

        // Check for specific multi-word patterns
        var lower = text.ToLowerInvariant();
        if (lower.Contains("waste of time") || lower.Contains("tired of waiting") || lower.Contains("fed up"))
        {
            scores["Frustration"] += 1.5;
        }
        if (lower.Contains("rip-off") || lower.Contains("unacceptable") || lower.Contains("how dare"))
        {
            scores["Anger"] += 1.5;
        }

        // Align with overall sentiment if no explicit emotion keywords hit
        double total = scores.Values.Sum();
        if (total < 0.1)
        {
            if (overallSentiment == "Positive")
            {
                scores["Satisfaction"] = 0.70;
                scores["Happiness"] = 0.30;
            }
            else if (overallSentiment == "Negative")
            {
                scores["Frustration"] = 0.60;
                scores["Sadness"] = 0.40;
            }
            else
            {
                return ("Neutral", scores);
            }
            total = scores.Values.Sum();
        }

        // Normalize probabilities
        var distribution = new Dictionary<string, double>();
        foreach (var kvp in scores)
        {
            distribution[kvp.Key] = total > 0 ? Math.Round(kvp.Value / total, 2) : 0.0;
        }

        var topEmotion = distribution.OrderByDescending(x => x.Value).First();
        var primary = topEmotion.Value >= 0.25 ? topEmotion.Key : (overallSentiment == "Positive" ? "Satisfaction" : overallSentiment == "Negative" ? "Frustration" : "Neutral");

        return (primary, distribution);
    }
}
