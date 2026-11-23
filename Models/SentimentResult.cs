using System.ComponentModel.DataAnnotations;

namespace Sentiment_Analysis.Models;

public class SentimentResult
{
    public int Id { get; set; }

    public int CommentId { get; set; }
    public virtual Comment Comment { get; set; } = null!;

    [StringLength(100)]
    public string ModelEngine { get; set; } = "Hybrid (ML.NET + Advanced Rule NLP)";

    public double PositiveProbability { get; set; }

    public double NegativeProbability { get; set; }

    public double NeutralProbability { get; set; }

    public double CalibratedConfidence { get; set; }

    public int NegationCount { get; set; }

    public int IntensifierCount { get; set; }

    [StringLength(500)]
    public string DetectedEmotions { get; set; } = string.Empty; // e.g., "Frustration:0.80, Satisfaction:0.10"

    [StringLength(1000)]
    public string NormalizedTokens { get; set; } = string.Empty;

    public DateTime AnalyzedAt { get; set; } = DateTime.Now;
}
