using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("exam")]
    public class Exam
    {
        [Key]
        [Column("examid")]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        [Column("examname")]
        public string ExamName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("durationminutes")]
        public int? DurationMinutes { get; set; }

        [Required]
        [Column("examtype")]
        public string ExamType { get; set; } = string.Empty; // 15p, 1tiet, cuoiky

        [Required]
        [Column("createdby")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("ispublished")]
        public bool IsPublished { get; set; } = false;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;

        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        public virtual ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
    }
} 