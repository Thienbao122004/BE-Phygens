using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("questionqualityfeedback")]
    public class QuestionQualityFeedback
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("questionid")]
        public string QuestionId { get; set; } = string.Empty;

        [Required]
        [Column("userid")]
        public string UserId { get; set; } = string.Empty;

        [Column("rating")]
        public int? Rating { get; set; }

        [Column("feedbacktext")]
        public string? FeedbackText { get; set; }

        [Column("feedbacktype")]
        public string? FeedbackType { get; set; } // quality, difficulty, clarity, accuracy

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
} 