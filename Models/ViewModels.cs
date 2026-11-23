namespace Sentiment_Analysis.Models;

public class DashboardViewModel
{
    public int TotalFeedback { get; set; }
    public int PositiveCount { get; set; }
    public int NegativeCount { get; set; }
    public int NeutralCount { get; set; }
    public int MixedCount { get; set; }

    public double PositivePercentage => TotalFeedback > 0 ? Math.Round((double)PositiveCount / TotalFeedback * 100, 1) : 0;
    public double NegativePercentage => TotalFeedback > 0 ? Math.Round((double)NegativeCount / TotalFeedback * 100, 1) : 0;
    public double NeutralPercentage => TotalFeedback > 0 ? Math.Round((double)NeutralCount / TotalFeedback * 100, 1) : 0;
    public double MixedPercentage => TotalFeedback > 0 ? Math.Round((double)MixedCount / TotalFeedback * 100, 1) : 0;

    // Net Sentiment Score (-100 to +100) = % Positive - % Negative
    public double NetSentimentScore => Math.Round(PositivePercentage - NegativePercentage, 1);

    public double AverageConfidence { get; set; }

    // Recent comments
    public List<Comment> RecentComments { get; set; } = new();

    // Chart Data
    public List<string> TrendLabels { get; set; } = new();
    public List<int> TrendPositive { get; set; } = new();
    public List<int> TrendNegative { get; set; } = new();
    public List<int> TrendNeutral { get; set; } = new();

    // Aspect breakdown
    public Dictionary<string, AspectSummary> AspectSummaries { get; set; } = new();

    // Emotion counts
    public Dictionary<string, int> EmotionCounts { get; set; } = new();

    // Insights
    public List<string> TopComplaints { get; set; } = new();
    public List<string> TopPraises { get; set; } = new();
}

public class AspectSummary
{
    public string AspectName { get; set; } = string.Empty;
    public int TotalMentions { get; set; }
    public int PositiveMentions { get; set; }
    public int NegativeMentions { get; set; }
    public int NeutralMentions { get; set; }
    public double SatisfactionRate => TotalMentions > 0 ? Math.Round((double)PositiveMentions / TotalMentions * 100, 1) : 0;
}

public class FeedbackHistoryViewModel
{
    public List<Comment> Comments { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SentimentFilter { get; set; }
    public string? AspectFilter { get; set; }
    public string? DateRangeFilter { get; set; }
    public string? SortOrder { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public class AnalysisResultDto
{
    public string Sentiment { get; set; } = "Neutral";
    public double SentimentScore { get; set; }
    public double Confidence { get; set; }
    public string PrimaryEmotion { get; set; } = "Neutral";
    public bool IsMixed { get; set; }
    public List<AspectResultDto> Aspects { get; set; } = new();
    public Dictionary<string, double> EmotionProbabilities { get; set; } = new();
    public string ModelEngine { get; set; } = "Hybrid (ML.NET + NLP Rule Engine)";
    public int NegationCount { get; set; }
    public string NormalizedText { get; set; } = string.Empty;
}

public class AspectResultDto
{
    public string Aspect { get; set; } = string.Empty;
    public string Sentiment { get; set; } = "Neutral";
    public double Score { get; set; }
    public string? MatchedPhrase { get; set; }
}

public class ModelEvaluationReport
{
    public string ModelName { get; set; } = "ML.NET SDCA Multiclass Sentiment Classifier";
    public DateTime TrainedAt { get; set; } = DateTime.Now;
    public int DatasetSize { get; set; }
    public double MicroAccuracy { get; set; }
    public double MacroAccuracy { get; set; }
    public double LogLoss { get; set; }
    public Dictionary<string, ClassMetric> PerClassMetrics { get; set; } = new();
    public int[,] ConfusionMatrix { get; set; } = new int[3, 3];
    public string[] ClassLabels { get; set; } = new[] { "Negative", "Neutral", "Positive" };
    public string Notes { get; set; } = string.Empty;
}

public class ClassMetric
{
    public string ClassName { get; set; } = string.Empty;
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int Support { get; set; }
}
