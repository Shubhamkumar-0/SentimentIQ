using System.ComponentModel.DataAnnotations;

namespace Sentiment_Analysis.Models;

public class AspectResult
{
    public int Id { get; set; }

    public int CommentId { get; set; }
    public virtual Comment Comment { get; set; } = null!;

    [Required]
    [StringLength(60)]
    public string Aspect { get; set; } = string.Empty; // Staff Behaviour, Waiting Time, Service Quality, Facilities, Communication

    [Required]
    [StringLength(20)]
    public string Sentiment { get; set; } = "Neutral"; // Positive, Negative, Neutral

    public double Score { get; set; } // -1.0 to 1.0

    [StringLength(250)]
    public string? MatchedPhrase { get; set; }
}
