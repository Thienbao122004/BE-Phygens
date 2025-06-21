using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("question")]
    public class Question
    {
        [Key]
        [Column("questionid")]
        public string QuestionId { get; set; } = string.Empty;

        [Required]
        [Column("topicid")]
        public string TopicId { get; set; } = string.Empty;

        [Required]
        [Column("questiontext")]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [Column("questiontype")]
        public string QuestionType { get; set; } = string.Empty; // multiple_choice, true_false, essay

        [Required]
        [Column("difficultylevel")]
        public string DifficultyLevel { get; set; } = string.Empty; // easy, medium, hard

        [Column("imageurl")]
        public string? ImageUrl { get; set; }

        [Required]
        [Column("createdby")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("TopicId")]
        public virtual PhysicsTopic Topic { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;

        public virtual ICollection<AnswerChoice> AnswerChoices { get; set; } = new List<AnswerChoice>();
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        public virtual ICollection<Explanation> Explanations { get; set; } = new List<Explanation>();
        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
} 