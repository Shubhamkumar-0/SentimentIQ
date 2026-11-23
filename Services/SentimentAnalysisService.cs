using System.Text.RegularExpressions;
using Sentiment_Analysis.Models;
using Sentiment_Analysis.Services.ML;
using Sentiment_Analysis.Services.NLP;

namespace Sentiment_Analysis.Services;

public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly MLNetSentimentEngine _mlEngine;
    private readonly AspectExtractor _aspectExtractor;
    private readonly EmotionDetector _emotionDetector;
    private readonly ILogger<SentimentAnalysisService> _logger;

    private static readonly HashSet<string> PositiveLexicon = new(StringComparer.OrdinalIgnoreCase)
    {
        "good", "great", "excellent", "helpful", "amazing", "awesome", "happy", "satisfied",
        "friendly", "best", "love", "loved", "wonderful", "supportive", "quick", "fast",
        "perfect", "clear", "useful", "professional", "resolved", "caring", "polite",
        "attentive", "thorough", "clean", "hygienic", "recommend", "recommended",
        "patient", "efficient", "courteous", "comforting", "impressive", "outstanding",
        "prompt", "smooth", "flawless", "appreciated", "relief", "reassuring", "kind"
    };

    private static readonly HashSet<string> NegativeLexicon = new(StringComparer.OrdinalIgnoreCase)
    {
        "bad", "poor", "worst", "terrible", "useless", "disappointed", "unhappy", "rude",
        "slow", "problem", "issue", "issues", "hate", "waste", "horrible", "failure",
        "confusing", "late", "unhelpful", "ignored", "dirty", "unhygienic", "disorganized",
        "arrogant", "dismissive", "impatient", "ineffective", "inefficient", "rushed",
        "painful", "frustrated", "frustrating", "annoyed", "annoying", "angry", "furious",
        "unacceptable", "disgrace", "scam", "rip-off", "cheated", "hostile", "appalled",
        "incompetent", "overcrowded", "unresponsive", "neglected", "delay", "delayed"
    };

    private static readonly HashSet<string> Negations = new(StringComparer.OrdinalIgnoreCase)
    {
        "not", "never", "no", "isn't", "isnt", "wasn't", "wasnt", "don't", "dont",
        "didn't", "didnt", "cannot", "cant", "can't", "barely", "hardly", "scarcely",
        "seldom", "neither", "nor", "without", "lack", "lacking", "won't", "wont"
    };

    private static readonly HashSet<string> Intensifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "very", "extremely", "terribly", "immensely", "really", "so", "completely",
        "totally", "highly", "absolutely", "exceptionally", "deeply", "hugely"
    };

    private static readonly HashSet<string> Diminishers = new(StringComparer.OrdinalIgnoreCase)
    {
        "somewhat", "slightly", "a bit", "little", "marginally", "partially"
    };

    public SentimentAnalysisService(
        MLNetSentimentEngine mlEngine,
        AspectExtractor aspectExtractor,
        EmotionDetector emotionDetector,
        ILogger<SentimentAnalysisService> logger)
    {
        _mlEngine = mlEngine;
        _aspectExtractor = aspectExtractor;
        _emotionDetector = emotionDetector;
        _logger = logger;
    }

    public AnalysisResultDto Analyze(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new AnalysisResultDto
            {
                Sentiment = "Neutral",
                SentimentScore = 0.0,
                Confidence = 0.50,
                PrimaryEmotion = "Neutral",
                IsMixed = false,
                ModelEngine = "Hybrid (ML.NET + Advanced Rule NLP)",
                NormalizedText = string.Empty
            };
        }

        var normalized = TextPreprocessor.NormalizeText(text);
        var clauses = TextPreprocessor.SplitIntoClauses(normalized);

        // Score each clause individually
        var clauseScores = new List<(string clause, string sentiment, double score, int posHits, int negHits)>();
        int totalNegationCount = 0;

        foreach (var clause in clauses)
        {
            var res = ScoreClause(clause);
            totalNegationCount += res.negations;
            clauseScores.Add((clause, res.sentiment, res.score, res.posHits, res.negHits));
        }

        // Check for Mixed Sentiment: conflicting polarities between clauses
        bool hasPositiveClause = clauseScores.Any(c => c.sentiment == "Positive" && c.score >= 0.20);
        bool hasNegativeClause = clauseScores.Any(c => c.sentiment == "Negative" && c.score <= -0.20);
        bool isMixed = hasPositiveClause && hasNegativeClause && clauses.Count > 1;

        // Run ML.NET Classifier
        var (mlSentiment, mlConfidence, mlProbabilities) = _mlEngine.Predict(normalized);

        // Overall rule-based score
        double overallRuleScore = clauseScores.Count > 0 ? clauseScores.Average(c => c.score) : 0.0;

        string finalSentiment;
        double finalScore;
        double finalConfidence;

        if (isMixed)
        {
            finalSentiment = "Mixed";
            // In mixed comments, score reflects the weighted balance
            finalScore = Math.Round(overallRuleScore, 2);
            finalConfidence = Math.Round(Math.Max(0.75, mlConfidence), 2);
        }
        else
        {
            if (overallRuleScore > 0.15)
            {
                finalSentiment = "Positive";
                finalScore = Math.Round(Math.Min(1.0, overallRuleScore), 2);
                finalConfidence = Math.Round(Math.Max(mlConfidence, 0.70 + Math.Min(0.25, overallRuleScore * 0.3)), 2);
            }
            else if (overallRuleScore < -0.15)
            {
                finalSentiment = "Negative";
                finalScore = Math.Round(Math.Max(-1.0, overallRuleScore), 2);
                finalConfidence = Math.Round(Math.Max(mlConfidence, 0.70 + Math.Min(0.25, Math.Abs(overallRuleScore) * 0.3)), 2);
            }
            else
            {
                // Fallback to ML.NET prediction when rule score is borderline
                if (mlConfidence >= 0.65 && mlSentiment != "Neutral")
                {
                    finalSentiment = mlSentiment;
                    finalScore = mlSentiment == "Positive" ? 0.50 : -0.50;
                    finalConfidence = Math.Round(mlConfidence, 2);
                }
                else
                {
                    finalSentiment = "Neutral";
                    finalScore = 0.0;
                    finalConfidence = Math.Round(Math.Max(0.60, mlConfidence), 2);
                }
            }
        }

        // Extract Aspects
        var aspectResults = _aspectExtractor.ExtractAspects(normalized, clause =>
        {
            var res = ScoreClause(clause);
            return (res.sentiment, res.score);
        });

        // Detect Emotions
        var (primaryEmotion, emotionDist) = _emotionDetector.DetectEmotions(normalized, finalSentiment);

        return new AnalysisResultDto
        {
            Sentiment = finalSentiment,
            SentimentScore = finalScore,
            Confidence = Math.Min(0.99, Math.Max(0.50, finalConfidence)),
            PrimaryEmotion = primaryEmotion,
            IsMixed = isMixed,
            Aspects = aspectResults,
            EmotionProbabilities = emotionDist,
            ModelEngine = isMixed ? "Rule Engine Multi-Clause Segmenter + ML.NET" : "ML.NET SDCA + Advanced Rule NLP",
            NegationCount = totalNegationCount,
            NormalizedText = normalized
        };
    }

    private (string sentiment, double score, int posHits, int negHits, int negations) ScoreClause(string clause)
    {
        var tokens = TextPreprocessor.Tokenize(clause);
        if (tokens.Length == 0) return ("Neutral", 0.0, 0, 0, 0);

        double positiveScore = 0.0;
        double negativeScore = 0.0;
        int posHits = 0;
        int negHits = 0;
        int negationHits = 0;

        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];

            // Check if token is negated (sliding window of 3 preceding tokens)
            bool isNegated = false;
            for (int k = Math.Max(0, i - 3); k < i; k++)
            {
                if (Negations.Contains(tokens[k]))
                {
                    isNegated = true;
                    negationHits++;
                    break;
                }
            }

            // Check intensifier / diminisher weight
            double multiplier = 1.0;
            if (i > 0)
            {
                if (Intensifiers.Contains(tokens[i - 1])) multiplier = 1.5;
                else if (Diminishers.Contains(tokens[i - 1])) multiplier = 0.6;
            }

            // Idiomatic phrase checks
            if (i > 0 && tokens[i - 1] == "no" && (token == "problem" || token == "issue" || token == "issues"))
            {
                positiveScore += 1.0 * multiplier;
                posHits++;
                continue;
            }

            if (PositiveLexicon.Contains(token))
            {
                if (isNegated)
                {
                    // "not good", "not helpful" -> Negative
                    negativeScore += 1.2 * multiplier;
                    negHits++;
                }
                else
                {
                    positiveScore += 1.0 * multiplier;
                    posHits++;
                }
            }
            else if (NegativeLexicon.Contains(token))
            {
                if (isNegated)
                {
                    // "not bad", "not terrible" -> Mildly Positive / Neutral
                    positiveScore += 0.8 * multiplier;
                    posHits++;
                }
                else
                {
                    negativeScore += 1.0 * multiplier;
                    negHits++;
                }
            }
        }

        double netScore = 0.0;
        double totalHits = positiveScore + negativeScore;
        if (totalHits > 0)
        {
            netScore = (positiveScore - negativeScore) / totalHits;
        }

        string sentiment = "Neutral";
        if (netScore > 0.15) sentiment = "Positive";
        else if (netScore < -0.15) sentiment = "Negative";

        return (sentiment, netScore, posHits, negHits, negationHits);
    }

    public ModelEvaluationReport GetEvaluationReport()
    {
        return _mlEngine.GetEvaluationReport();
    }

    public void RetrainModel()
    {
        _mlEngine.TrainAndEvaluate();
    }
}
