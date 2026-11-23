namespace Sentiment_Analysis.Services;

/// <summary>
/// Backward-compatible adapter for the original starter analyzer.
/// Delegates to the advanced ISentimentAnalysisService.
/// </summary>
public class SentimentAnalyzer
{
    private readonly ISentimentAnalysisService _service;

    public SentimentAnalyzer(ISentimentAnalysisService service)
    {
        _service = service;
    }

    public string Analyze(string? comment)
    {
        var result = _service.Analyze(comment);
        return result.Sentiment;
    }
}
