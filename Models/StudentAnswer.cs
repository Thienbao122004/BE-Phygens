using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("studentanswer")]
    public class StudentAnswer
    {
        [Key]
        [Column("answerid")]
        public string AnswerId { get; set; } = string.Empty;

        [Required]
        [Column("attemptid")]
        public string AttemptId { get; set; } = string.Empty;

        [Required]
        [Column("questionid")]
        public string QuestionId { get; set; } = string.Empty;

        [Column("selectedchoiceid")]
        public string? SelectedChoiceId { get; set; }

        [Column("studenttextanswer")]
        public string? StudentTextAnswer { get; set; }

        [Column("iscorrect")]
        public bool IsCorrect { get; set; } = false;

        [Column("pointsearned")]
        public decimal PointsEarned { get; set; } = 0;

        [Column("answeredat")]
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AttemptId")]
        public virtual StudentAttempt Attempt { get; set; } = null!;

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;

        [ForeignKey("SelectedChoiceId")]
        public virtual AnswerChoice? SelectedChoice { get; set; }
    }
} 