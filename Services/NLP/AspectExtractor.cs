using System.Text.RegularExpressions;
using Sentiment_Analysis.Models;

namespace Sentiment_Analysis.Services.NLP;

public class AspectExtractor
{
    private static readonly Dictionary<string, string[]> AspectKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "Staff Behaviour", new[]
            {
                "staff", "doctor", "nurse", "receptionist", "attendant", "advisor", "consultant",
                "physician", "specialist", "counselor", "attitude", "polite", "rude", "behaviour",
                "behavior", "manner", "bedside manner", "courteous", "friendly", "unfriendly",
                "respectful", "disrespectful", "professional", "unprofessional", "arrogant",
                "caring", "dismissive", "attentive", "inattentive", "patient", "impatient"
            }
        },
        {
            "Waiting Time", new[]
            {
                "wait", "waiting", "queue", "line", "delay", "delayed", "late", "punctual",
                "punctuality", "schedule", "scheduled", "hours", "minutes", "slow", "fast",
                "quick", "prompt", "on time", "in time", "timing", "appointment time",
                "turnaround", "took too long", "long wait"
            }
        },
        {
            "Service Quality", new[]
            {
                "service", "consultation", "treatment", "diagnosis", "advice", "care", "outcome",
                "resolution", "standard", "quality", "effective", "ineffective", "efficient",
                "inefficient", "thorough", "rushed", "prescription", "checkup", "exam",
                "examination", "solution", "help", "support", "expertise", "competent", "incompetent"
            }
        },
        {
            "Facilities", new[]
            {
                "facility", "facilities", "room", "clinic", "hospital", "cabin", "office",
                "seating", "chair", "clean", "cleanliness", "dirty", "unhygienic", "hygiene",
                "parking", "amenities", "equipment", "air conditioning", "ac", "restroom",
                "washroom", "toilet", "ambiance", "atmosphere", "environment", "noise", "tidy", "messy"
            }
        },
        {
            "Communication", new[]
            {
                "communication", "communicated", "explain", "explanation", "explained", "listened",
                "listening", "clarity", "clear", "unclear", "instructions", "guidance", "guide",
                "understanding", "miscommunication", "responsive", "answer", "answered",
                "queries", "doubts", "language", "informative", "clarified", "ignored"
            }
        }
    };

    public List<AspectResultDto> ExtractAspects(string text, Func<string, (string sentiment, double score)> clauseScorer)
    {
        var results = new List<AspectResultDto>();
        if (string.IsNullOrWhiteSpace(text)) return results;

        var clauses = TextPreprocessor.SplitIntoClauses(text);

        foreach (var aspectKvp in AspectKeywords)
        {
            var aspectName = aspectKvp.Key;
            var keywords = aspectKvp.Value;

            // Check if this aspect is mentioned in any clause
            foreach (var clause in clauses)
            {
                var lowerClause = clause.ToLowerInvariant();
                string? matchedKeyword = null;

                foreach (var kw in keywords)
                {
                    // Match whole word or phrase
                    if (Regex.IsMatch(lowerClause, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase))
                    {
                        matchedKeyword = kw;
                        break;
                    }
                }

                if (matchedKeyword != null)
                {
                    var (sentiment, score) = clauseScorer(clause);

                    // Check if we already have an entry for this aspect
                    var existing = results.FirstOrDefault(r => r.Aspect.Equals(aspectName, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        results.Add(new AspectResultDto
                        {
                            Aspect = aspectName,
                            Sentiment = sentiment,
                            Score = score,
                            MatchedPhrase = $"Matched '{matchedKeyword}' in: \"{clause.Trim()}\""
                        });
                    }
                    else
                    {
                        // Update if this clause has stronger polarity
                        if (Math.Abs(score) > Math.Abs(existing.Score))
                        {
                            existing.Sentiment = sentiment;
                            existing.Score = score;
                            existing.MatchedPhrase = $"Matched '{matchedKeyword}' in: \"{clause.Trim()}\"";
                        }
                    }
                }
            }
        }

        return results;
    }
}
