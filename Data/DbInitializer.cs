using Microsoft.EntityFrameworkCore;
using Sentiment_Analysis.Models;
using Sentiment_Analysis.Services;

namespace Sentiment_Analysis.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context, ISentimentAnalysisService sentimentService)
    {
        // Apply migrations or ensure schema is created
        context.Database.EnsureCreated();

        // Check if we need to backfill existing comments with sentiment results
        var unanalyzedComments = context.Comments
            .Where(c => c.Confidence == 0.5 && c.PrimaryEmotion == "Neutral" && c.SentimentScore == 0.0)
            .ToList();

        foreach (var c in unanalyzedComments)
        {
            var analysis = sentimentService.Analyze(c.CommentText);
            c.Sentiment = analysis.Sentiment;
            c.SentimentScore = analysis.SentimentScore;
            c.Confidence = analysis.Confidence;
            c.PrimaryEmotion = analysis.PrimaryEmotion;
            c.IsMixed = analysis.IsMixed;

            if (!context.AspectResults.Any(a => a.CommentId == c.Id))
            {
                foreach (var asp in analysis.Aspects)
                {
                    context.AspectResults.Add(new AspectResult
                    {
                        CommentId = c.Id,
                        Aspect = asp.Aspect,
                        Sentiment = asp.Sentiment,
                        Score = asp.Score,
                        MatchedPhrase = asp.MatchedPhrase
                    });
                }
            }

            if (!context.SentimentResults.Any(s => s.CommentId == c.Id))
            {
                context.SentimentResults.Add(new SentimentResult
                {
                    CommentId = c.Id,
                    ModelEngine = analysis.ModelEngine,
                    PositiveProbability = analysis.Sentiment == "Positive" ? analysis.Confidence : 0.1,
                    NegativeProbability = analysis.Sentiment == "Negative" ? analysis.Confidence : 0.1,
                    NeutralProbability = analysis.Sentiment == "Neutral" ? analysis.Confidence : 0.1,
                    CalibratedConfidence = analysis.Confidence,
                    NegationCount = analysis.NegationCount,
                    DetectedEmotions = string.Join(", ", analysis.EmotionProbabilities.Select(e => $"{e.Key}:{e.Value}")),
                    NormalizedTokens = analysis.NormalizedText
                });
            }
        }
        context.SaveChanges();

        // Seed realistic consultation feedback dataset if fewer than 10 comments exist
        if (context.Comments.Count() < 10)
        {
            var seedData = new (string user, string text, int daysAgo)[]
            {
                ("Dr. Ananya Roy", "The staff was excellent and helpful throughout the checkup.", 6),
                ("Vikramaditya Rao", "The waiting time was terrible and the front desk was unhelpful.", 5),
                ("Kavita Nair", "The consultation happened yesterday at 10 AM.", 5),
                ("Rahul Sharma", "The doctor was very caring and helpful but the waiting time was terrible.", 4),
                ("Pooja Verma", "The service was not good and the medication instructions were confusing.", 4),
                ("Amitabh Sen", "I am very frustrated with the poor service and long queue.", 3),
                ("Sneha Kulkarni", "Clean clinic facilities, polite receptionist, and outstanding medical advice.", 3),
                ("Rohan Malhotra", "Waited 45 minutes past my slot. Doctor was rushed and barely listened.", 2),
                ("Divya Iyer", "Superb consultation. The physician explained all diagnostic reports patiently.", 2),
                ("Arjun Mehta", "Routine follow-up completed. Blood pressure checked.", 1),
                ("Meera Nambiar", "Very happy with the compassionate care received from the nursing staff.", 1),
                ("Siddharth Patel", "Facility cleanliness was exceptional and the turnaround time was fast.", 0),
                ("Tanvi Deshmukh", "Doctor provided good treatment but clinic seating was dirty and uncomfortable.", 0),
                ("Karthik Reddy", "Extremely disappointed. Waste of time, doctor arrived an hour late.", 0)
            };

            foreach (var item in seedData)
            {
                var analysis = sentimentService.Analyze(item.text);
                var comment = new Comment
                {
                    UserName = item.user,
                    CommentText = item.text,
                    Sentiment = analysis.Sentiment,
                    SentimentScore = analysis.SentimentScore,
                    Confidence = analysis.Confidence,
                    PrimaryEmotion = analysis.PrimaryEmotion,
                    IsMixed = analysis.IsMixed,
                    CreatedAt = DateTime.Now.AddDays(-item.daysAgo).AddHours(item.daysAgo * 2)
                };

                context.Comments.Add(comment);
                context.SaveChanges();

                foreach (var asp in analysis.Aspects)
                {
                    context.AspectResults.Add(new AspectResult
                    {
                        CommentId = comment.Id,
                        Aspect = asp.Aspect,
                        Sentiment = asp.Sentiment,
                        Score = asp.Score,
                        MatchedPhrase = asp.MatchedPhrase
                    });
                }

                context.SentimentResults.Add(new SentimentResult
                {
                    CommentId = comment.Id,
                    ModelEngine = analysis.ModelEngine,
                    PositiveProbability = analysis.Sentiment == "Positive" ? analysis.Confidence : 0.15,
                    NegativeProbability = analysis.Sentiment == "Negative" ? analysis.Confidence : 0.15,
                    NeutralProbability = analysis.Sentiment == "Neutral" ? analysis.Confidence : 0.20,
                    CalibratedConfidence = analysis.Confidence,
                    NegationCount = analysis.NegationCount,
                    DetectedEmotions = string.Join(", ", analysis.EmotionProbabilities.Select(e => $"{e.Key}:{e.Value}")),
                    NormalizedTokens = analysis.NormalizedText,
                    AnalyzedAt = comment.CreatedAt
                });

                context.SaveChanges();
            }
        }
    }
}
