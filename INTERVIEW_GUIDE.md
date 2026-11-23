# Technical Interview & Faculty Viva Defense Guide
## Project: AI-Powered Consultation Feedback Intelligence System

This document is prepared to help you confidently answer questions from faculty evaluators, external viva examiners, and placement technical interviewers.

---

### TOP 10 FREQUENTLY ASKED QUESTIONS & ANSWERS

#### Q1: "Why can't simple keyword lookup handle real sentiment analysis?"
> **Answer:**
> "A naive keyword algorithm matches static words in isolation without syntactic context. It fails critically on three major linguistic phenomena:
> 1. **Negation**: In *'The service was not good'*, a keyword matcher sees the word *'good'* and falsely classifies it as Positive.
> 2. **Contrastive Mixed Polarity**: In *'The staff was helpful but the waiting time was terrible'*, summing words either averages out to neutral or misrepresents one half of the user's experience.
> 3. **Idioms & Modifiers**: In *'There was no problem'*, *'problem'* is negative, yet the phrase means positive.
> 
> Our system solves this with:
> - Contraction expansion (*wasn't &rarr; was not*)
> - A 3-token backward sliding negation window
> - Conjunction-based clause segmentation
> - A trained ML.NET SDCA multiclass machine learning classifier."

---

#### Q2: "What machine learning algorithm did you use, and why?"
> **Answer:**
> "We implemented **ML.NET with Stochastic Dual Coordinate Ascent (SDCA) Maximum Entropy**.
> - **Why SDCA?** SDCA is an efficient convex optimization algorithm for linear classification models that operates on dual formulation coordinates. It converges faster than traditional Stochastic Gradient Descent (SGD) on sparse text features.
> - **Why Maximum Entropy (Multinomial Logistic Regression)?** Because sentiment in consultation feedback is naturally three-class (*Negative, Neutral, Positive*). Maximum entropy produces calibrated posterior probabilities via the Softmax function:
>   $$P(y = c \mid x) = \frac{e^{w_c^T x}}{\sum_{k} e^{w_k^T x}}$$
> - **Features**: Featurized with word and character n-grams using TF-IDF weighting and L1/L2 regularization."

---

#### Q3: "What is Aspect-Based Sentiment Analysis (ABSA), and how is it implemented?"
> **Answer:**
> "Standard sentiment analysis gives one generic label to an entire document. In consultations, a client often has different opinions on different dimensions. 
> ABSA deconstructs the feedback into 5 domain-specific aspects:
> 1. **Staff Behaviour**
> 2. **Waiting Time**
> 3. **Service Quality**
> 4. **Facilities**
> 5. **Communication**
> 
> When a sentence like *'The doctor was caring but clinic seating was dirty'* is submitted, the system segments the text by the conjunction *'but'*. It evaluates *'The doctor was caring'* against *Staff Behaviour* (+0.80) and *'clinic seating was dirty'* against *Facilities* (-0.80), attributing exact clause excerpts."

---

#### Q4: "How do you evaluate your model? Explain Precision, Recall, F1-Score, and the Confusion Matrix."
> **Answer:**
> "We evaluate the model on an independent test split (20%) using standard multi-class metrics:
> - **Precision**: What proportion of positive identifications was actually correct?
>   $$\text{Precision} = \frac{TP}{TP + FP}$$
> - **Recall**: What proportion of actual positives was identified correctly?
>   $$\text{Recall} = \frac{TP}{TP + FN}$$
> - **F1-Score**: The harmonic mean of Precision and Recall:
>   $$F_1 = 2 \cdot \frac{\text{Precision} \cdot \text{Recall}}{\text{Precision} + \text{Recall}}$$
> - **Confusion Matrix**: A $3 \times 3$ grid displaying True Positive counts along the diagonal $(i=j)$ and classification errors (False Positives and False Negatives) in off-diagonal cells.
> - **Micro vs Macro Accuracy**: Micro-accuracy is global correct prediction rate; Macro-accuracy is the unweighted average across classes, ensuring smaller classes aren't overshadowed."

---

#### Q5: "How is the confidence score calculated?"
> **Answer:**
> "Rather than generating arbitrary random numbers, our confidence scoring is calibrated:
> 1. The ML.NET model outputs Softmax probabilities for each class.
> 2. The rule engine calculates clause polarity density.
> 3. For unambiguous inputs, confidence reaches 90-95%. For borderline statements or mixed sentiments, the confidence reflects the calculated probability spread, bounded between 50% and 99%."

---

#### Q6: "Explain your database design and Entity Framework Core setup."
> **Answer:**
> "We designed a normalized relational database in SQL Server LocalDB:
> - `Comments`: Main entity storing client info, comment text, overall sentiment, score, calibrated confidence, emotion, and mixed flag.
> - `AspectResults`: 1-to-many relationship with `Comments` holding individual aspect scores and matched phrases.
> - `SentimentResults`: 1-to-1 relationship with `Comments` recording model engine, Softmax probabilities, negation counts, and execution trace.
> - Foreign keys have `ON DELETE CASCADE` so deleting a comment automatically cleans up its dependent aspect and sentiment logs.
> - Indexes are placed on `Comments.CreatedAt`, `Comments.Sentiment`, `AspectResults.Aspect`, and `AspectResults.Sentiment` for fast dashboard query aggregation."

---

#### Q7: "How did you ensure the system is secure and production-ready?"
> **Answer:**
> - **Server-Side Validation**: DataAnnotations (`[Required]`, `[StringLength]`) ensure input length and integrity before processing.
> - **Anti-Forgery Protection**: Every state-changing form (Create, Delete) uses `@Html.AntiForgeryToken()` with `[ValidateAntiForgeryToken]` to prevent CSRF attacks.
> - **SQL Injection Prevention**: All database queries are executed via Entity Framework Core parameterized LINQ queries.
> - **Exception Handling & Logging**: Global exception handling with `ILogger` captures issues without exposing stack traces to end users."

---

#### Q8: "How does the Live AI Preview work in the UI?"
> **Answer:**
> "When the user types in the comment textarea, a JavaScript event listener with a 300ms debounce timer sends the text asynchronously to `/Comments/LiveAnalyze` using `fetch()`. The controller runs the hybrid sentiment pipeline and returns a JSON payload with predicted sentiment, confidence, emotion, and detected aspects. The DOM updates dynamically in real-time before the user submits the form."

---

#### Q9: "What design pattern did you follow in ASP.NET Core?"
> **Answer:**
> - **Repository/DbContext Pattern**: EF Core acts as the Unit of Work.
> - **Dependency Injection (DI)**: In `Program.cs`, services are registered with appropriate lifecycles:
>   - `MLNetSentimentEngine`: `Singleton` (model is loaded in memory once).
>   - `AspectExtractor` & `EmotionDetector`: `Singleton`.
>   - `ISentimentAnalysisService`: `Scoped` per HTTP request.
> - **Adapter Pattern**: `SentimentAnalyzer.cs` wraps `ISentimentAnalysisService` so any legacy references continue to work seamlessly.
> - **ViewModel / DTO Pattern**: `DashboardViewModel`, `FeedbackHistoryViewModel`, `AnalysisResultDto` decouple the view from database entity representations."

---

#### Q10: "What are future enhancements for this project?"
> **Answer:**
> 1. Multi-lingual support using HuggingFace / ONNX Transformer models (e.g., XLM-RoBERTa for Hindi/English code-mixed reviews).
> 2. Voice-to-text feedback submission using Whisper API.
> 3. Automated email alerts to clinic administrators when negative sentiment is detected in high-severity aspects like *Staff Behaviour*."
