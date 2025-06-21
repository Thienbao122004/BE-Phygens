using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("explanations")]
    public class Explanation
    {
        [Key]
        [Column("explanationid")]
        public string ExplanationId { get; set; } = string.Empty;

        [Required]
        [Column("questionid")]
        public string QuestionId { get; set; } = string.Empty;

        [Required]
        [Column("explanationtext")]
        public string ExplanationText { get; set; } = string.Empty;

        [Required]
        [Column("createdby")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; } = null!;
    }
} 