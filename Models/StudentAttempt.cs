using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("studentattempt")]
    public class StudentAttempt
    {
        [Key]
        [Column("attemptid")]
        public string AttemptId { get; set; } = string.Empty;

        [Required]
        [Column("userid")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("examid")]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        [Column("starttime")]
        public DateTime StartTime { get; set; }

        [Column("endtime")]
        public DateTime? EndTime { get; set; }

        [Column("totalscore")]
        public decimal TotalScore { get; set; } = 0;

        [Column("maxscore")]
        public decimal? MaxScore { get; set; }

        [Column("status")]
        public string Status { get; set; } = "in_progress"; // in_progress, completed, abandoned

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;

        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
} 