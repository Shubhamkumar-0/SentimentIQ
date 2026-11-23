using System.ComponentModel.DataAnnotations;

namespace Sentiment_Analysis.Models;

public class Comment
{
    public int Id { get; set; }

    [Required(ErrorMessage = "User name is required.")]
    [StringLength(80, ErrorMessage = "User name cannot exceed 80 characters.")]
    [Display(Name = "Your Name")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Comment text is required.")]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "Comment must be between 3 and 2000 characters.")]
    [Display(Name = "Consultation Feedback")]
    public string CommentText { get; set; } = string.Empty;

    [StringLength(20)]
    public string Sentiment { get; set; } = "Neutral"; // Positive, Negative, Neutral, Mixed

    public double SentimentScore { get; set; } = 0.0; // Scaled -1.0 (very negative) to +1.0 (very positive)

    public double Confidence { get; set; } = 0.50; // Calibrated probability 0.0 to 1.0

    [StringLength(50)]
    public string PrimaryEmotion { get; set; } = "Neutral"; // Satisfaction, Frustration, Happiness, Anger, Sadness, Neutral

    public bool IsMixed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Relational Navigation Properties
    public virtual ICollection<AspectResult> AspectResults { get; set; } = new List<AspectResult>();
    public virtual SentimentResult? SentimentResult { get; set; }
}
