using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentiment_Analysis.Data;
using Sentiment_Analysis.Models;
using Sentiment_Analysis.Services;

namespace Sentiment_Analysis.Controllers;

public class CommentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISentimentAnalysisService _sentimentService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        ApplicationDbContext db,
        ISentimentAnalysisService sentimentService,
        ILogger<CommentsController> logger)
    {
        _db = db;
        _sentimentService = sentimentService;
        _logger = logger;
    }

    // GET: /Comments/ or /
    public async Task<IActionResult> Index()
    {
        var comments = await _db.Comments
            .AsNoTracking()
            .Include(c => c.AspectResults)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var aspects = await _db.AspectResults
            .AsNoTracking()
            .ToListAsync();

        var dashboard = new DashboardViewModel
        {
            TotalFeedback = comments.Count,
            PositiveCount = comments.Count(c => c.Sentiment == "Positive"),
            NegativeCount = comments.Count(c => c.Sentiment == "Negative"),
            NeutralCount = comments.Count(c => c.Sentiment == "Neutral"),
            MixedCount = comments.Count(c => c.Sentiment == "Mixed"),
            AverageConfidence = comments.Count > 0 ? Math.Round(comments.Average(c => c.Confidence) * 100, 1) : 0,
            RecentComments = comments.Take(6).ToList()
        };

        // Trend over time (last 7 days grouped by date)
        var sevenDaysAgo = DateTime.Now.Date.AddDays(-6);
        var dateGroups = comments
            .Where(c => c.CreatedAt.Date >= sevenDaysAgo)
            .GroupBy(c => c.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        for (int i = 6; i >= 0; i--)
        {
            var targetDate = DateTime.Now.Date.AddDays(-i);
            dashboard.TrendLabels.Add(targetDate.ToString("MMM dd"));
            var group = dateGroups.FirstOrDefault(g => g.Key == targetDate);
            dashboard.TrendPositive.Add(group?.Count(c => c.Sentiment == "Positive") ?? 0);
            dashboard.TrendNegative.Add(group?.Count(c => c.Sentiment == "Negative") ?? 0);
            dashboard.TrendNeutral.Add(group?.Count(c => c.Sentiment == "Neutral" || c.Sentiment == "Mixed") ?? 0);
        }

        // Aspect Summaries
        var standardAspects = new[] { "Staff Behaviour", "Waiting Time", "Service Quality", "Facilities", "Communication" };
        foreach (var aspName in standardAspects)
        {
            var aspectItems = aspects.Where(a => a.Aspect.Equals(aspName, StringComparison.OrdinalIgnoreCase)).ToList();
            dashboard.AspectSummaries[aspName] = new AspectSummary
            {
                AspectName = aspName,
                TotalMentions = aspectItems.Count,
                PositiveMentions = aspectItems.Count(a => a.Sentiment == "Positive"),
                NegativeMentions = aspectItems.Count(a => a.Sentiment == "Negative"),
                NeutralMentions = aspectItems.Count(a => a.Sentiment == "Neutral")
            };
        }

        // Emotion breakdown
        var emotionGroups = comments
            .Where(c => !string.IsNullOrEmpty(c.PrimaryEmotion))
            .GroupBy(c => c.PrimaryEmotion)
            .ToDictionary(g => g.Key, g => g.Count());

        var standardEmotions = new[] { "Satisfaction", "Happiness", "Frustration", "Anger", "Sadness", "Neutral" };
        foreach (var em in standardEmotions)
        {
            dashboard.EmotionCounts[em] = emotionGroups.TryGetValue(em, out int cnt) ? cnt : 0;
        }

        // Voice of the patient: Top complaints and praises
        dashboard.TopComplaints = aspects
            .Where(a => a.Sentiment == "Negative" && !string.IsNullOrEmpty(a.MatchedPhrase))
            .OrderByDescending(a => a.Id)
            .Select(a => $"{a.Aspect}: {a.MatchedPhrase}")
            .Distinct()
            .Take(4)
            .ToList();

        if (dashboard.TopComplaints.Count == 0)
        {
            dashboard.TopComplaints.Add("No critical complaints recorded in the system.");
        }

        dashboard.TopPraises = aspects
            .Where(a => a.Sentiment == "Positive" && !string.IsNullOrEmpty(a.MatchedPhrase))
            .OrderByDescending(a => a.Id)
            .Select(a => $"{a.Aspect}: {a.MatchedPhrase}")
            .Distinct()
            .Take(4)
            .ToList();

        if (dashboard.TopPraises.Count == 0)
        {
            dashboard.TopPraises.Add("Positive ratings on professional consultation standard.");
        }

        return View(dashboard);
    }

    // GET: /Comments/History
    public async Task<IActionResult> History(
        string? searchQuery,
        string? sentimentFilter,
        string? aspectFilter,
        string? dateRangeFilter,
        string? sortOrder,
        int page = 1,
        int pageSize = 10)
    {
        var query = _db.Comments
            .AsNoTracking()
            .Include(c => c.AspectResults)
            .Include(c => c.SentimentResult)
            .AsQueryable();

        // Search query
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(c => c.UserName.ToLower().Contains(search) || c.CommentText.ToLower().Contains(search));
        }

        // Filter by Sentiment
        if (!string.IsNullOrWhiteSpace(sentimentFilter) && sentimentFilter != "All")
        {
            query = query.Where(c => c.Sentiment == sentimentFilter);
        }

        // Filter by Aspect
        if (!string.IsNullOrWhiteSpace(aspectFilter) && aspectFilter != "All")
        {
            query = query.Where(c => c.AspectResults.Any(a => a.Aspect == aspectFilter));
        }

        // Filter by Date Range
        if (!string.IsNullOrWhiteSpace(dateRangeFilter) && dateRangeFilter != "All")
        {
            var now = DateTime.Now;
            if (dateRangeFilter == "Today")
            {
                var today = now.Date;
                query = query.Where(c => c.CreatedAt >= today);
            }
            else if (dateRangeFilter == "7Days")
            {
                var d7 = now.AddDays(-7);
                query = query.Where(c => c.CreatedAt >= d7);
            }
            else if (dateRangeFilter == "30Days")
            {
                var d30 = now.AddDays(-30);
                query = query.Where(c => c.CreatedAt >= d30);
            }
        }

        // Sorting
        query = sortOrder switch
        {
            "date_asc" => query.OrderBy(c => c.CreatedAt),
            "score_desc" => query.OrderByDescending(c => c.SentimentScore),
            "score_asc" => query.OrderBy(c => c.SentimentScore),
            "confidence_desc" => query.OrderByDescending(c => c.Confidence),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var comments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new FeedbackHistoryViewModel
        {
            Comments = comments,
            SearchQuery = searchQuery,
            SentimentFilter = sentimentFilter,
            AspectFilter = aspectFilter,
            DateRangeFilter = dateRangeFilter,
            SortOrder = sortOrder,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(viewModel);
    }

    // GET: /Comments/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Comment());
    }

    // POST: /Comments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Comment comment)
    {
        if (!ModelState.IsValid)
        {
            return View(comment);
        }

        try
        {
            // Run Sentiment & Aspect & Emotion analysis
            var analysis = _sentimentService.Analyze(comment.CommentText);

            comment.Sentiment = analysis.Sentiment;
            comment.SentimentScore = analysis.SentimentScore;
            comment.Confidence = analysis.Confidence;
            comment.PrimaryEmotion = analysis.PrimaryEmotion;
            comment.IsMixed = analysis.IsMixed;
            comment.CreatedAt = DateTime.Now;

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            // Store Aspect Results
            if (analysis.Aspects != null && analysis.Aspects.Count > 0)
            {
                foreach (var asp in analysis.Aspects)
                {
                    _db.AspectResults.Add(new AspectResult
                    {
                        CommentId = comment.Id,
                        Aspect = asp.Aspect,
                        Sentiment = asp.Sentiment,
                        Score = asp.Score,
                        MatchedPhrase = asp.MatchedPhrase
                    });
                }
            }

            // Store Detailed Sentiment Analysis Log
            _db.SentimentResults.Add(new SentimentResult
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

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Feedback submitted successfully! Classified as '{comment.Sentiment}' (Confidence: {Math.Round(comment.Confidence * 100)}%).";
            return RedirectToAction(nameof(Details), new { id = comment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing comment sentiment.");
            ModelState.AddModelError(string.Empty, "An error occurred while analyzing and saving your comment. Please try again.");
            return View(comment);
        }
    }

    // GET: /Comments/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var comment = await _db.Comments
            .AsNoTracking()
            .Include(c => c.AspectResults)
            .Include(c => c.SentimentResult)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null)
        {
            return NotFound();
        }

        return View(comment);
    }

    // POST: /Comments/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment != null)
        {
            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Comment deleted successfully.";
        }
        return RedirectToAction(nameof(History));
    }

    // POST API: /Comments/LiveAnalyze (for interactive preview)
    [HttpPost]
    public IActionResult LiveAnalyze([FromBody] LiveAnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
        {
            return Json(new AnalysisResultDto { Sentiment = "Neutral", Confidence = 0.50, PrimaryEmotion = "Neutral" });
        }

        var result = _sentimentService.Analyze(request.Text);
        return Json(result);
    }

    // GET: /Comments/ExportCsv
    public async Task<IActionResult> ExportCsv(
        string? searchQuery,
        string? sentimentFilter,
        string? aspectFilter,
        string? dateRangeFilter)
    {
        var query = _db.Comments
            .AsNoTracking()
            .Include(c => c.AspectResults)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToLower();
            query = query.Where(c => c.UserName.ToLower().Contains(search) || c.CommentText.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(sentimentFilter) && sentimentFilter != "All")
        {
            query = query.Where(c => c.Sentiment == sentimentFilter);
        }

        if (!string.IsNullOrWhiteSpace(aspectFilter) && aspectFilter != "All")
        {
            query = query.Where(c => c.AspectResults.Any(a => a.Aspect == aspectFilter));
        }

        var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("Id,UserName,CommentText,Sentiment,SentimentScore,Confidence,PrimaryEmotion,IsMixed,Aspects,CreatedAt");

        foreach (var item in list)
        {
            var safeComment = $"\"{item.CommentText.Replace("\"", "\"\"")}\"";
            var safeUser = $"\"{item.UserName.Replace("\"", "\"\"")}\"";
            var aspectsStr = $"\"{string.Join("; ", item.AspectResults.Select(a => $"{a.Aspect}:{a.Sentiment}"))}\"";

            builder.AppendLine($"{item.Id},{safeUser},{safeComment},{item.Sentiment},{item.SentimentScore},{item.Confidence},{item.PrimaryEmotion},{item.IsMixed},{aspectsStr},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var fileName = $"Consultation_Feedback_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    // GET: /Comments/ModelEvaluation
    public IActionResult ModelEvaluation()
    {
        var report = _sentimentService.GetEvaluationReport();
        return View(report);
    }
}

public class LiveAnalyzeRequest
{
    public string? Text { get; set; }
}
