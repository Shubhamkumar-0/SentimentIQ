using Microsoft.ML;
using Microsoft.ML.Data;
using Sentiment_Analysis.Models;

namespace Sentiment_Analysis.Services.ML;

public class SentimentInput
{
    public string SentimentText { get; set; } = string.Empty;
    public string Label { get; set; } = "Neutral"; // Negative, Neutral, Positive
}

public class SentimentPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;

    public float[] Score { get; set; } = Array.Empty<float>();
}

public class MLNetSentimentEngine
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<SentimentInput, SentimentPrediction>? _predEngine;
    private readonly object _lock = new();
    private ModelEvaluationReport? _cachedReport;
    private readonly string _modelPath;

    public MLNetSentimentEngine(string? customModelPath = null)
    {
        _mlContext = new MLContext(seed: 42);
        var baseDir = AppContext.BaseDirectory;
        _modelPath = customModelPath ?? Path.Combine(baseDir, "MLModel", "SentimentModel.zip");
        InitializeOrTrain();
    }

    private void InitializeOrTrain()
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            TrainAndEvaluate();
        }
    }

    public void TrainAndEvaluate()
    {
        lock (_lock)
        {
            var dataset = GetConsultationDataset();
            var dataView = _mlContext.Data.LoadFromEnumerable(dataset);

            // Train / Test split (80% train, 20% test)
            var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.20, seed: 42);

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                    inputColumnName: nameof(SentimentInput.Label),
                    outputColumnName: "KeyLabel")
                .Append(_mlContext.Transforms.Text.FeaturizeText(
                    inputColumnName: nameof(SentimentInput.SentimentText),
                    outputColumnName: "Features"))
                .AppendCacheCheckpoint(_mlContext)
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "KeyLabel",
                    featureColumnName: "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                    inputColumnName: "PredictedLabel",
                    outputColumnName: nameof(SentimentPrediction.PredictedLabel)));

            _model = pipeline.Fit(split.TrainSet);

            // Save model
            _mlContext.Model.Save(_model, split.TrainSet.Schema, _modelPath);
            _predEngine = _mlContext.Model.CreatePredictionEngine<SentimentInput, SentimentPrediction>(_model);

            // Evaluate on test set
            var testPredictions = _model.Transform(split.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(
                testPredictions,
                labelColumnName: "KeyLabel",
                scoreColumnName: "Score");

            // Build evaluation report
            var report = new ModelEvaluationReport
            {
                ModelName = "ML.NET SDCA Maximum Entropy Multiclass Classifier",
                TrainedAt = DateTime.Now,
                DatasetSize = dataset.Count,
                MicroAccuracy = Math.Round(metrics.MicroAccuracy, 4),
                MacroAccuracy = Math.Round(metrics.MacroAccuracy, 4),
                LogLoss = Math.Round(metrics.LogLoss, 4),
                ClassLabels = new[] { "Negative", "Neutral", "Positive" },
                Notes = "Trained on domain-specific consultation feedback dataset covering all 5 clinical and advisory aspects."
            };

            // Calculate Confusion Matrix & Per-class Precision / Recall
            var actualsAndPreds = _mlContext.Data.CreateEnumerable<SentimentPredictionWithActual>(testPredictions, reuseRowObject: false).ToList();
            var labels = new[] { "Negative", "Neutral", "Positive" };
            var cm = new int[3, 3];

            foreach (var item in actualsAndPreds)
            {
                int actualIdx = Array.IndexOf(labels, item.Label);
                int predIdx = Array.IndexOf(labels, item.PredictedLabel);
                if (actualIdx >= 0 && actualIdx < 3 && predIdx >= 0 && predIdx < 3)
                {
                    cm[actualIdx, predIdx]++;
                }
            }

            report.ConfusionMatrix = cm;

            // Per class precision, recall, F1
            for (int i = 0; i < 3; i++)
            {
                string label = labels[i];
                int truePositive = cm[i, i];
                int falsePositive = 0;
                int falseNegative = 0;
                int support = 0;

                for (int j = 0; j < 3; j++)
                {
                    support += cm[i, j];
                    if (j != i)
                    {
                        falsePositive += cm[j, i];
                        falseNegative += cm[i, j];
                    }
                }

                double precision = (truePositive + falsePositive) > 0 ? (double)truePositive / (truePositive + falsePositive) : 1.0;
                double recall = (truePositive + falseNegative) > 0 ? (double)truePositive / (truePositive + falseNegative) : 1.0;
                double f1 = (precision + recall) > 0 ? 2 * (precision * recall) / (precision + recall) : 0.0;

                report.PerClassMetrics[label] = new ClassMetric
                {
                    ClassName = label,
                    Precision = Math.Round(precision, 3),
                    Recall = Math.Round(recall, 3),
                    F1Score = Math.Round(f1, 3),
                    Support = support
                };
            }

            _cachedReport = report;
        }
    }

    public (string sentiment, double confidence, double[] probabilities) Predict(string text)
    {
        lock (_lock)
        {
            if (_predEngine == null || _model == null)
            {
                TrainAndEvaluate();
            }

            var prediction = _predEngine!.Predict(new SentimentInput { SentimentText = text });

            // Apply softmax to raw scores
            var rawScores = prediction.Score;
            var probs = new double[3];
            if (rawScores != null && rawScores.Length >= 3)
            {
                double maxScore = rawScores.Max();
                double sumExp = rawScores.Sum(s => Math.Exp(s - maxScore));
                for (int i = 0; i < 3; i++)
                {
                    probs[i] = Math.Round(Math.Exp(rawScores[i] - maxScore) / sumExp, 3);
                }
            }
            else
            {
                probs = new[] { 0.33, 0.34, 0.33 };
            }

            double confidence = probs.Max();
            string predicted = !string.IsNullOrEmpty(prediction.PredictedLabel) ? prediction.PredictedLabel : "Neutral";

            return (predicted, confidence, probs);
        }
    }

    public ModelEvaluationReport GetEvaluationReport()
    {
        lock (_lock)
        {
            if (_cachedReport == null)
            {
                TrainAndEvaluate();
            }
            return _cachedReport!;
        }
    }

    private class SentimentPredictionWithActual
    {
        public string Label { get; set; } = string.Empty;
        public string PredictedLabel { get; set; } = string.Empty;
    }

    private static List<SentimentInput> GetConsultationDataset()
    {
        return new List<SentimentInput>
        {
            // POSITIVE SAMPLES
            new() { SentimentText = "The staff was excellent and helpful.", Label = "Positive" },
            new() { SentimentText = "Doctor gave very thorough advice and clear explanations.", Label = "Positive" },
            new() { SentimentText = "Waiting time was quick and the consultation was wonderful.", Label = "Positive" },
            new() { SentimentText = "Very friendly receptionist and clean modern clinic room.", Label = "Positive" },
            new() { SentimentText = "Great service, solved my health issue immediately.", Label = "Positive" },
            new() { SentimentText = "Highly satisfied with the professional treatment.", Label = "Positive" },
            new() { SentimentText = "The consultant was extremely polite, attentive and patient.", Label = "Positive" },
            new() { SentimentText = "Appointment started right on time, no delay at all.", Label = "Positive" },
            new() { SentimentText = "The doctor listened carefully to all my symptoms.", Label = "Positive" },
            new() { SentimentText = "Superb hospital facilities, immaculate hygiene and comfortable seating.", Label = "Positive" },
            new() { SentimentText = "I felt completely cared for and supported throughout.", Label = "Positive" },
            new() { SentimentText = "Clear guidance and instructions were given on medications.", Label = "Positive" },
            new() { SentimentText = "Everything was smooth, efficient, and well organized.", Label = "Positive" },
            new() { SentimentText = "Truly wonderful experience, great bedside manner.", Label = "Positive" },
            new() { SentimentText = "Best consultation I have had in years, very helpful advice.", Label = "Positive" },
            new() { SentimentText = "The nurse was gentle, friendly and comforting.", Label = "Positive" },
            new() { SentimentText = "Quick turnaround, prompt attention and great resolution.", Label = "Positive" },
            new() { SentimentText = "Clean consultation chambers, quiet ambiance, polite staff.", Label = "Positive" },
            new() { SentimentText = "The advisor gave outstanding recommendations.", Label = "Positive" },
            new() { SentimentText = "Pleased with the outcome, felt heard and validated.", Label = "Positive" },
            new() { SentimentText = "Prompt scheduling and very courteous staff.", Label = "Positive" },
            new() { SentimentText = "I am so happy with the recovery plan suggested.", Label = "Positive" },
            new() { SentimentText = "Excellent consultation, well worth the time.", Label = "Positive" },
            new() { SentimentText = "No issue at all, very smooth and professional.", Label = "Positive" },

            // NEGATIVE SAMPLES
            new() { SentimentText = "The waiting time was terrible.", Label = "Negative" },
            new() { SentimentText = "The service was not good.", Label = "Negative" },
            new() { SentimentText = "I am very frustrated with the poor service.", Label = "Negative" },
            new() { SentimentText = "Rude receptionist and dismissive doctor.", Label = "Negative" },
            new() { SentimentText = "Had to wait over two hours past my scheduled appointment time.", Label = "Negative" },
            new() { SentimentText = "The consultation was rushed and completely unhelpful.", Label = "Negative" },
            new() { SentimentText = "Dirty facilities, broken chairs and unhygienic restroom.", Label = "Negative" },
            new() { SentimentText = "Doctor did not explain my diagnosis properly.", Label = "Negative" },
            new() { SentimentText = "Terrible experience, felt completely ignored by the staff.", Label = "Negative" },
            new() { SentimentText = "The treatment prescribed was totally ineffective and worsened symptoms.", Label = "Negative" },
            new() { SentimentText = "Extremely disappointed with the lack of communication.", Label = "Negative" },
            new() { SentimentText = "Horrible attitude from the advisor, very arrogant.", Label = "Negative" },
            new() { SentimentText = "Complete waste of time and money.", Label = "Negative" },
            new() { SentimentText = "Never coming back here, worst consultation ever.", Label = "Negative" },
            new() { SentimentText = "Staff was not helpful and gave contradictory information.", Label = "Negative" },
            new() { SentimentText = "No one answered my questions and I left feeling confused.", Label = "Negative" },
            new() { SentimentText = "The room was noisy, dirty and unorganized.", Label = "Negative" },
            new() { SentimentText = "I was angry because the doctor arrived an hour late.", Label = "Negative" },
            new() { SentimentText = "Poor customer care and unhelpful front desk.", Label = "Negative" },
            new() { SentimentText = "The consultation was not useful at all.", Label = "Negative" },
            new() { SentimentText = "Felt totally dismissed and neglected by the consultant.", Label = "Negative" },
            new() { SentimentText = "Waiting room was overcrowded with no air conditioning.", Label = "Negative" },
            new() { SentimentText = "Very bad experience, uncaring attitude.", Label = "Negative" },
            new() { SentimentText = "Incompetent service and delayed response.", Label = "Negative" },

            // NEUTRAL SAMPLES
            new() { SentimentText = "The consultation happened yesterday.", Label = "Neutral" },
            new() { SentimentText = "I attended the routine follow-up session at 10 AM.", Label = "Neutral" },
            new() { SentimentText = "Consultation lasted for 15 minutes as scheduled.", Label = "Neutral" },
            new() { SentimentText = "I received a prescription for standard vitamins.", Label = "Neutral" },
            new() { SentimentText = "The appointment was booked online last Monday.", Label = "Neutral" },
            new() { SentimentText = "Blood pressure and temperature were recorded by the assistant.", Label = "Neutral" },
            new() { SentimentText = "My next consultation is scheduled for next month.", Label = "Neutral" },
            new() { SentimentText = "The consultation took place in room number 3.", Label = "Neutral" },
            new() { SentimentText = "The doctor asked about my past medical history.", Label = "Neutral" },
            new() { SentimentText = "Consultation was average, standard procedure followed.", Label = "Neutral" },
            new() { SentimentText = "I arrived at the hospital and checked in at the kiosk.", Label = "Neutral" },
            new() { SentimentText = "The lab test results will be available in three days.", Label = "Neutral" },
            new() { SentimentText = "General checkup completed, no specific findings.", Label = "Neutral" },
            new() { SentimentText = "Follow-up consultation conducted over video call.", Label = "Neutral" },
            new() { SentimentText = "Payment was processed at the reception desk.", Label = "Neutral" },
            new() { SentimentText = "Submitted feedback form after the consultation.", Label = "Neutral" },
            new() { SentimentText = "The consultation occurred as planned.", Label = "Neutral" },
            new() { SentimentText = "Requested a copy of the consultation summary report.", Label = "Neutral" },
            new() { SentimentText = "The consultation was standard without any special remarks.", Label = "Neutral" },
            new() { SentimentText = "Waiting time was 10 minutes and doctor asked standard questions.", Label = "Neutral" }
        };
    }
}
