using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("answerchoice")]
    public class AnswerChoice
    {
        [Key]
        [Column("choiceid")]
        public string ChoiceId { get; set; } = string.Empty;

        [Required]
        [Column("questionid")]
        public string QuestionId { get; set; } = string.Empty;

        [Required]
        [Column("choicelabel")]
        public string ChoiceLabel { get; set; } = string.Empty;

        [Required]
        [Column("choicetext")]
        public string ChoiceText { get; set; } = string.Empty;

        [Column("iscorrect")]
        public bool IsCorrect { get; set; } = false;

        [Column("displayorder")]
        public int? DisplayOrder { get; set; }

        // Navigation properties
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;

        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
} 