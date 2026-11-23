using Sentiment_Analysis.Models;

namespace Sentiment_Analysis.Services;

public interface ISentimentAnalysisService
{
    AnalysisResultDto Analyze(string? text);
    ModelEvaluationReport GetEvaluationReport();
    void RetrainModel();
}
