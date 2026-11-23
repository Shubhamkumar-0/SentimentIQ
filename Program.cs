using Microsoft.EntityFrameworkCore;
using Sentiment_Analysis.Data;
using Sentiment_Analysis.Services;
using Sentiment_Analysis.Services.ML;
using Sentiment_Analysis.Services.NLP;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Services
builder.Services.AddControllersWithViews();

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Register NLP & ML Services
builder.Services.AddSingleton<MLNetSentimentEngine>();
builder.Services.AddSingleton<AspectExtractor>();
builder.Services.AddSingleton<EmotionDetector>();
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
builder.Services.AddScoped<SentimentAnalyzer>();

var app = builder.Build();

// Database Migration & Seeding Lifecycle
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var sentimentService = services.GetRequiredService<ISentimentAnalysisService>();
        DbInitializer.Initialize(db, sentimentService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing or seeding the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Comments}/{action=Index}/{id?}"
);

app.Run();
