using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("examquestion")]
    public class ExamQuestion
    {
        [Key]
        [Column("examquestionid")]
        public string ExamQuestionId { get; set; } = string.Empty;

        [Required]
        [Column("examid")]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        [Column("questionid")]
        public string QuestionId { get; set; } = string.Empty;

        [Column("questionorder")]
        public int? QuestionOrder { get; set; }

        [Column("pointsweight")]
        public decimal PointsWeight { get; set; } = 1.0m;

        [Column("addedat")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;
    }
} 