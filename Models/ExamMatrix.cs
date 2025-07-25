using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Models
{
    [Table("exammatrix")]
    public class ExamMatrix
    {
        [Key]
        [Column("matrixid")]
        public string MatrixId { get; set; } = string.Empty;

        [Required]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Column("topic")]
        public string Topic { get; set; } = string.Empty;

        [Column("numeasy")]
        public int NumEasy { get; set; } = 0;

        [Column("nummedium")]
        public int NumMedium { get; set; } = 0;

        [Column("numhard")]
        public int NumHard { get; set; } = 0;

        [Column("totalquestions")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int TotalQuestions { get; set; } = 0;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("examname")]
        public string? ExamName { get; set; }

        // Navigation properties
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ICollection<ExamMatrixDetail> ExamMatrixDetails { get; set; } = new List<ExamMatrixDetail>();
        
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
} 