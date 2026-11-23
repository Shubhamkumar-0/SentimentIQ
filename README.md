# Sentiment Analysis of Comments Received Through Consultation Module
## AI-Powered Consultation Feedback Intelligence System

**Academic Final Year Project (B.Tech Computer Science & Engineering)**  
**Technology Stack:** C# 12 · ASP.NET Core MVC (.NET 8) · SQL Server LocalDB · Entity Framework Core · Microsoft ML.NET 3.0 · Chart.js · HTML5 / Modern CSS

---

## 🌟 Executive Overview
The **AI-Powered Consultation Feedback Intelligence System** is an enterprise-grade web application designed to evaluate, analyze, and extract actionable clinical and advisory intelligence from unstructured feedback comments received through consultation modules.

Moving far beyond naive keyword search, the system integrates a **transparent dual-engine architecture**:
1. **Machine Learning Classifier (ML.NET 3.0)**: Trained using Stochastic Dual Coordinate Ascent (SDCA) Maximum Entropy multiclass classification with n-gram text featurization.
2. **Aspect-Based Sentiment Analysis (ABSA) & Clause Parser**: Evaluates 5 core consultation aspects (*Staff Behaviour, Waiting Time, Service Quality, Facilities, Communication*).
3. **Advanced Negation & Mixed Polarity Engine**: Resolves complex grammar, contractions, 3-token negation scopes (*"not good"*, *"no issues"*), and contrastive compound clauses (*"Staff was helpful BUT waiting time was terrible"*).
4. **Emotion Spectrum Detection**: Categorizes dominant emotional states (*Satisfaction, Happiness, Frustration, Anger, Sadness*).
5. **Calibrated Confidence Scoring**: Probabilistic sigmoid calibration mapping confidence between 50% and 99%.

---

## 🚀 Key Features

### 1. Executive Analytics Dashboard
- **KPI Summary Cards**: Total consultations, positive, negative, neutral, mixed counts, and Net Sentiment Score (NSS).
- **Interactive Chart.js Visualizations**:
  - *Sentiment Distribution*: Doughnut chart with center summary.
  - *7-Day Trajectory*: Multi-line trend graph tracking sentiment progression.
  - *Aspect-Based Breakdown*: Grouped bar chart comparing positive/negative/neutral feedback across all 5 aspects.
  - *Emotional Spectrum*: Radar chart mapping emotional intensity.
- **Voice of the Client**: Dynamic extraction of critical complaints and top appreciated service areas.
- **Recent Stream**: Real-time table of incoming feedback with aspect badges and sentiment indicators.

### 2. Feedback Repository & Management
- **Multi-parameter Search & Filtering**:
  - Full-text keyword search across comments and usernames.
  - Filter by sentiment class (*Positive, Negative, Neutral, Mixed*).
  - Filter by aspect (*Staff Behaviour, Waiting Time, Service Quality, Facilities, Communication*).
  - Filter by date range (*Today, Last 7 Days, Last 30 Days, All Time*).
  - Multi-criteria sorting (Newest, Oldest, Highest Sentiment, Lowest Sentiment, Highest Confidence).
- **Pagination**: Configurable page-sized data table.
- **One-Click CSV Export**: Download filtered feedback and complete NLP metadata.
- **Secure Deletion**: Anti-forgery protected delete with confirmation safeguards.

### 3. Feedback Submission with Live AI Preview
- **Real-Time Interactive Prediction**: As the user types their feedback, a debounced AJAX service (`/Comments/LiveAnalyze`) predicts sentiment, confidence, primary emotion, and isolated aspects before form submission.
- **Benchmark Sample Buttons**: Instant one-click population of faculty benchmark test cases.

### 4. Forensic NLP Details View
- In-depth inspection page displaying:
  - Exact matched excerpt phrases for every detected aspect.
  - Softmax probability distributions.
  - Negation trigger counts and normalized tokens.
  - Timestamped model pipeline execution metadata.

### 5. Model Transparency & Evaluation Dashboard
- Explicit evaluation report of the trained ML.NET model:
  - Micro-Accuracy, Macro-Accuracy, and Cross-Entropy Log Loss.
  - Per-class Precision, Recall, and F1-Scores.
  - Interactive 3x3 Confusion Matrix with visual classification heatmaps.

---

## 🛠️ Project Architecture

```
D:\myproject\
├── Controllers/
│   └── CommentsController.cs       # Dashboard, Repository, Live AI API, CSV Export, Model Evaluation
├── Data/
│   ├── ApplicationDbContext.cs     # EF Core DbContext with relational configurations & indexes
│   └── DbInitializer.cs            # Schema creation, migration backfill & representative dataset seeding
├── Models/
│   ├── Comment.cs                  # Primary consultation feedback entity
│   ├── AspectResult.cs             # Relational 1-to-many aspect decomposition entity
│   ├── SentimentResult.cs          # Relational 1-to-1 NLP diagnostic log entity
│   └── ViewModels.cs               # Dashboard, History, AnalysisResult DTOs & ModelEvaluationReport
├── Services/
│   ├── ISentimentAnalysisService.cs# Clean DI interface
│   ├── SentimentAnalysisService.cs # Hybrid orchestrator (Clause segmentation, negation, calibration)
│   ├── SentimentAnalyzer.cs        # Backward-compatible adapter
│   ├── ML/
│   │   └── MLNetSentimentEngine.cs # ML.NET SDCA trainer, test-split evaluator, prediction engine
│   └── NLP/
│       ├── TextPreprocessor.cs     # Contraction expansion, tokenization, clause boundary splitter
│       ├── AspectExtractor.cs      # ABSA extractor for 5 consultation dimensions
│       └── EmotionDetector.cs      # Multi-emotion detection with negation sensitivity
├── Views/
│   ├── Comments/
│   │   ├── Index.cshtml            # Executive Analytics Dashboard (Chart.js)
│   │   ├── History.cshtml          # Search, Filter, Paginate, CSV Export
│   │   ├── Create.cshtml           # Feedback submission with Live AI Preview
│   │   ├── Details.cshtml          # Forensic NLP breakdown report
│   │   └── ModelEvaluation.cshtml  # ML.NET evaluation report & Confusion Matrix
│   └── Shared/
│       └── _Layout.cshtml          # Professional Navy/Slate enterprise sidebar layout
├── tests/
│   └── ConsultationSentiment.Tests/# 8 comprehensive xUnit unit tests verifying all benchmark cases
├── wwwroot/
│   └── css/site.css                # Polished enterprise CSS theme
└── ConsultationSentimentProject.csproj
```

---

## 🧪 Benchmark Test Cases (Verified & Passing)

| # | Test Feedback Input | Expected Sentiment | Key Identified Aspect / Emotion | Test Result |
|---|-------------------|-------------------|--------------------------------|-------------|
| 1 | *"The staff was excellent and helpful."* | **Positive** | Staff Behaviour: Positive, Satisfaction | ✅ Passed |
| 2 | *"The waiting time was terrible."* | **Negative** | Waiting Time: Negative, Frustration | ✅ Passed |
| 3 | *"The consultation happened yesterday."* | **Neutral** | General Consultation, Zero Polarity | ✅ Passed |
| 4 | *"The staff was helpful but the waiting time was terrible."* | **Mixed** | Staff: Positive \| Waiting: Negative | ✅ Passed |
| 5 | *"The service was not good."* | **Negative** | Service Quality: Negative (Negation handled) | ✅ Passed |
| 6 | *"I am very frustrated with the poor service."* | **Negative** | Service Quality: Negative, Frustration Emotion | ✅ Passed |

---

## ⚡ How to Run the Application

### Prerequisites
- Windows OS
- .NET 8 SDK or .NET 10 SDK
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)

### Execution Steps
1. Open PowerShell or Terminal in the project root:
   ```powershell
   cd D:\myproject
   ```
2. Build the project:
   ```powershell
   dotnet build
   ```
3. Run the unit test suite:
   ```powershell
   dotnet test tests/ConsultationSentiment.Tests/ConsultationSentiment.Tests.csproj
   ```
4. Launch the web application:
   ```powershell
   dotnet run
   ```
5. Open your browser and navigate to the displayed URL (e.g., `http://localhost:5000` or the port shown in the console).

---

## 👨‍🏫 Faculty & Placement Interview Defense Guide
See [`INTERVIEW_GUIDE.md`](./INTERVIEW_GUIDE.md) for detailed explanations of:
- Machine Learning vs Rule-Based NLP trade-offs
- Why keyword matching fails on negation and mixed sentences
- Mathematical formulation of Precision, Recall, F1, and Confusion Matrix
- Relational database schema design and EF Core migrations
- Calibrated confidence calculation
