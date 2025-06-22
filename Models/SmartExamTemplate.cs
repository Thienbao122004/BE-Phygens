using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("smartexamtemplates")]
    public class SmartExamTemplate
    {
        [Key]
        [Column("templateid")]
        public string TemplateId { get; set; } = string.Empty;

        [Required]
        [Column("templatename")]
        public string TemplateName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("targetgrade")]
        public int TargetGrade { get; set; }

        [Required]
        [Column("examtype")]
        public string ExamType { get; set; } = string.Empty;

        [Column("difficultydistribution")]
        public string? DifficultyDistribution { get; set; } // JSON string

        [Column("chapterweights")]
        public string? ChapterWeights { get; set; } // JSON string

        [Required]
        [Column("totalquestions")]
        public int TotalQuestions { get; set; }

        [Required]
        [Column("durationminutes")]
        public int DurationMinutes { get; set; }

        [Required]
        [Column("createdby")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;
    }
} 