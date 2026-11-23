using Microsoft.Extensions.Logging.Abstractions;
using Sentiment_Analysis.Services;
using Sentiment_Analysis.Services.ML;
using Sentiment_Analysis.Services.NLP;
using Xunit;
using Xunit.Abstractions;

namespace ConsultationSentiment.Tests;

public class SentimentAnalysisTests
{
    private readonly ISentimentAnalysisService _sentimentService;
    private readonly ITestOutputHelper _output;

    public SentimentAnalysisTests(ITestOutputHelper output)
    {
        _output = output;
        var mlEngine = new MLNetSentimentEngine();
        var aspectExtractor = new AspectExtractor();
        var emotionDetector = new EmotionDetector();
        var logger = NullLogger<SentimentAnalysisService>.Instance;

        _sentimentService = new SentimentAnalysisService(mlEngine, aspectExtractor, emotionDetector, logger);
    }

    [Fact]
    public void TestCase1_StaffWasExcellentAndHelpful_ReturnsPositive()
    {
        var text = "The staff was excellent and helpful.";
        var result = _sentimentService.Analyze(text);

        Assert.Equal("Positive", result.Sentiment);
        Assert.True(result.SentimentScore > 0, "Sentiment score should be positive");
        Assert.True(result.Confidence >= 0.70, "Confidence should be high");
        Assert.Contains(result.Aspects, a => a.Aspect == "Staff Behaviour" && a.Sentiment == "Positive");
    }

    [Fact]
    public void TestCase2_WaitingTimeWasTerrible_ReturnsNegative()
    {
        var text = "The waiting time was terrible.";
        var result = _sentimentService.Analyze(text);

        Assert.Equal("Negative", result.Sentiment);
        Assert.True(result.SentimentScore < 0, "Sentiment score should be negative");
        Assert.Contains(result.Aspects, a => a.Aspect == "Waiting Time" && a.Sentiment == "Negative");
    }

    [Fact]
    public void TestCase3_ConsultationHappenedYesterday_ReturnsNeutral()
    {
        var text = "The consultation happened yesterday.";
        var result = _sentimentService.Analyze(text);

        Assert.Equal("Neutral", result.Sentiment);
        Assert.InRange(result.SentimentScore, -0.15, 0.15);
    }

    [Fact]
    public void TestCase4_StaffWasHelpfulButWaitingTimeWasTerrible_ReturnsMixed()
    {
        var text = "The staff was helpful but the waiting time was terrible.";
        var result = _sentimentService.Analyze(text);

        Assert.True(result.IsMixed, "Should be detected as mixed sentiment compound sentence");
        Assert.Equal("Mixed", result.Sentiment);

        var staffAspect = result.Aspects.FirstOrDefault(a => a.Aspect == "Staff Behaviour");
        var waitAspect = result.Aspects.FirstOrDefault(a => a.Aspect == "Waiting Time");

        Assert.NotNull(staffAspect);
        Assert.Equal("Positive", staffAspect.Sentiment);

        Assert.NotNull(waitAspect);
        Assert.Equal("Negative", waitAspect.Sentiment);
    }

    [Fact]
    public void TestCase5_ServiceWasNotGood_HandlesNegation_ReturnsNegative()
    {
        var text = "The service was not good.";
        var result = _sentimentService.Analyze(text);

        Assert.Equal("Negative", result.Sentiment);
        Assert.True(result.SentimentScore < 0, "Negated positive should yield negative polarity");
        Assert.Contains(result.Aspects, a => a.Aspect == "Service Quality" && a.Sentiment == "Negative");
    }

    [Fact]
    public void TestCase6_FrustratedWithPoorService_DetectsFrustrationEmotion()
    {
        var text = "I am very frustrated with the poor service.";
        var result = _sentimentService.Analyze(text);

        Assert.Equal("Negative", result.Sentiment);
        Assert.Equal("Frustration", result.PrimaryEmotion);
        Assert.True(result.EmotionProbabilities.ContainsKey("Frustration"));
        Assert.True(result.EmotionProbabilities["Frustration"] >= 0.30);
    }

    [Fact]
    public void TestTextPreprocessor_ExpandsContractionsProperly()
    {
        var input = "The doctor wasn't helpful and couldn't explain.";
        var expanded = TextPreprocessor.ExpandContractions(input);

        Assert.Contains("was not", expanded);
        Assert.Contains("could not", expanded);
    }

    [Fact]
    public void TestMLNetEngine_ReturnsValidEvaluationReport()
    {
        var report = _sentimentService.GetEvaluationReport();

        _output.WriteLine($"MicroAccuracy: {report.MicroAccuracy}");
        _output.WriteLine($"MacroAccuracy: {report.MacroAccuracy}");
        _output.WriteLine($"LogLoss: {report.LogLoss}");
        _output.WriteLine($"DatasetSize: {report.DatasetSize}");

        Assert.NotNull(report);
        Assert.True(report.DatasetSize > 0, "Dataset size should be > 0");
        Assert.True(report.MicroAccuracy >= 0.0, "Micro accuracy should be >= 0");
        Assert.NotNull(report.ConfusionMatrix);
        Assert.Equal(3, report.ConfusionMatrix.GetLength(0));
        Assert.Equal(3, report.ConfusionMatrix.GetLength(1));
        Assert.Contains("Positive", report.PerClassMetrics.Keys);
        Assert.Contains("Negative", report.PerClassMetrics.Keys);
        Assert.Contains("Neutral", report.PerClassMetrics.Keys);
    }
}
