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

        // AI-related properties
        [Column("isaigenerated")]
        public bool IsAiGenerated { get; set; } = false;

        [Column("aigenerationconfig", TypeName = "jsonb")]
        public string? AiGenerationConfig { get; set; } // JSON string

        [Column("autogradingenabled")]
        public bool AutoGradingEnabled { get; set; } = true;

        [Column("adaptivedifficulty")]
        public bool AdaptiveDifficulty { get; set; } = false;

        [Column("ExamMatrixMatrixId")]
        public string? ExamMatrixMatrixId { get; set; }

        // Navigation properties
        [ForeignKey("CreatedBy")]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual User Creator { get; set; } = null!;

        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
    }
} 