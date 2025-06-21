using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("exammatrix")]
    public class ExamMatrix
    {
        [Key]
        [Column("matrixid")]
        public string MatrixId { get; set; } = string.Empty;

        [Required]
        [Column("examname")]
        [StringLength(200)]
        public string ExamName { get; set; } = string.Empty; // "Kiểm tra 15 phút - Cơ học"

        [Required]
        [Column("examtype")]
        [StringLength(50)]
        public string ExamType { get; set; } = string.Empty; // "15p", "1tiet", "giuaki", "cuoiki"

        [Required]
        [Column("grade")]
        public int Grade { get; set; } // 10, 11, 12

        [Column("duration")]
        public int Duration { get; set; } = 45; // Thời gian làm bài (phút)

        [Column("totalquestions")]
        public int TotalQuestions { get; set; } = 0;

        [Column("totalpoints")]
        public decimal TotalPoints { get; set; } = 10.0m; // Tổng điểm

        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("createdby")]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<ExamMatrixDetail> ExamMatrixDetails { get; set; } = new List<ExamMatrixDetail>();
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
} 