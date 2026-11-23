# System Architecture Documentation
## AI-Powered Consultation Feedback Intelligence System

### 1. High-Level Architecture Overview
The system follows the **Clean MVC & Layered Architecture** with distinct separation of concerns:
- **Presentation Layer (ASP.NET Core MVC & Razor)**: Responsive enterprise dashboard, interactive Chart.js visualizations, live typing prediction via fetch API.
- **Service Layer (NLP & ML Core)**: Orchestrates preprocessing, negation resolution, contrastive clause parsing, ML.NET classification, aspect extraction, and emotion profiling.
- **Persistence Layer (Entity Framework Core & SQL Server LocalDB)**: Normalized relational schema with index optimization and cascading foreign keys.

```
+---------------------------------------------------------------------------------+
|                               Presentation Layer                                |
|  - Executive Dashboard (Chart.js: Distribution, 7-Day Trend, Aspects, Radar)    |
|  - Feedback Repository (Full-text search, multi-filters, sorting, pagination)   |
|  - Create Feedback Form (Live AJAX preview & faculty benchmark shortcuts)       |
|  - Forensic NLP Details View & Model Transparency Dashboard                     |
+---------------------------------------------------------------------------------+
                                       │ (HTTP / JSON)
                                       ▼
+---------------------------------------------------------------------------------+
|                            CommentsController                                   |
|  [Index, History, Create, Details, Delete, LiveAnalyze, ExportCsv, ModelEval]   |
+---------------------------------------------------------------------------------+
                                       │ (Dependency Injection)
                                       ▼
+---------------------------------------------------------------------------------+
|                        ISentimentAnalysisService                                |
|  - SentimentAnalysisService (Hybrid Intelligence Orchestrator)                  |
|     ├── TextPreprocessor (Contraction expansion, tokenization, clause splitter) |
|     ├── NegationHandler (3-token sliding window, phrase-level negation)         |
|     ├── AspectExtractor (Staff, Waiting Time, Service, Facilities, Comms)       |
|     ├── EmotionDetector (Happiness, Anger, Sadness, Frustration, Satisfaction)  |
|     └── MLNetSentimentEngine (SDCA Multiclass Classifier & Evaluation Pipeline) |
+---------------------------------------------------------------------------------+
                                       │
                                       ▼
+---------------------------------------------------------------------------------+
|                           ApplicationDbContext                                  |
|  - Comments (Id, UserName, CommentText, Sentiment, SentimentScore, Confidence)  |
|  - AspectResults (1-to-many: Aspect, Sentiment, Score, MatchedPhrase)           |
|  - SentimentResults (1-to-1: ModelEngine, Probabilities, Emotions, Diagnostics)|
+---------------------------------------------------------------------------------+
                                       │
                                       ▼
+---------------------------------------------------------------------------------+
|                       SQL Server LocalDB (ConsultationSentimentDB)              |
+---------------------------------------------------------------------------------+
```

---

### 2. NLP Pipeline Deep Dive

#### Stage 1: Text Preprocessing & Contraction Expansion
1. Contractions like *"wasn't"*, *"don't"*, *"can't"*, *"isn't"* are systematically expanded to *"was not"*, *"do not"*, *"cannot"*, *"is not"*.
2. Whitespace is normalized and tokens are extracted using Unicode word boundary regex `[^\p{L}\p{N}']+`.

#### Stage 2: Clause Segmentation & Mixed Sentiment Detection
1. Compound feedback containing contrastive markers (*but, however, although, yet, nevertheless, while, whereas*) is segmented into separate clauses.
2. Example: `"The staff was helpful but the waiting time was terrible."`
   - Clause 1: `"The staff was helpful"` &rarr; Evaluated as **Positive** (+0.80) &rarr; Aspect: **Staff Behaviour** (+0.80).
   - Clause 2: `"the waiting time was terrible"` &rarr; Evaluated as **Negative** (-0.90) &rarr; Aspect: **Waiting Time** (-0.90).
   - Overall Feedback: Marked as **Mixed** (`IsMixed = true`), preventing inaccurate binary averaging.

#### Stage 3: Negation-Aware Scoring
1. Traditional keyword lookups fail when a negation precedes a positive term (e.g., *"not good"*).
2. The system employs a 3-token backward sliding window.
3. Positive terms inverted by negation add directly to the negative polarity score (`negativeScore += 1.2 * multiplier`).
4. Idiomatic terms like *"no issue"* or *"no problem"* are recognized as positive indicators.

#### Stage 4: Aspect-Based Sentiment Analysis (ABSA)
Extracts clinical and operational insights across 5 dimensions:
1. **Staff Behaviour**: Staff, doctor, nurse, receptionist, advisor, polite, rude, attitude.
2. **Waiting Time**: Wait, queue, delay, late, schedule, turnaround, appointment time.
3. **Service Quality**: Service, consultation, treatment, diagnosis, advice, care, outcome.
4. **Facilities**: Room, clinic, hospital, seating, cleanliness, clean, dirty, hygiene, parking.
5. **Communication**: Explanation, listened, clarity, clear, instructions, guidance.

#### Stage 5: Emotion Detection
Detects 5 human emotional states:
- *Happiness*: delighted, glad, joyful, wonderful, smiling.
- *Satisfaction*: satisfied, content, met expectations, recommended, professional.
- *Frustration*: frustrated, annoying, waste of time, fed up, tired of waiting.
- *Anger*: furious, outraged, disgusted, unacceptable, rude, rip-off.
- *Sadness*: depressed, sad, heartbroken, unfortunate, regret.

#### Stage 6: ML.NET SDCA Classifier & Confidence Calibration
- **Algorithm**: Stochastic Dual Coordinate Ascent (SDCA) Maximum Entropy for multiclass classification.
- **Features**: Word & Character n-grams extracted via `FeaturizeText`.
- **Confidence Calibration**: Combines Softmax probabilities with clause polarity certainty, mapping between 0.50 and 0.99.
