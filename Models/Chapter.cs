using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("chapter")]
    public class Chapter
    {
        [Key]
        [Column("chapterid")]
        public int ChapterId { get; set; }

        [Required]
        [Column("chaptername")]
        [StringLength(100)]
        public string ChapterName { get; set; } = string.Empty;

        [Required]
        [Column("grade")]
        public int Grade { get; set; } // 10, 11, 12

        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("displayorder")]
        public int DisplayOrder { get; set; } // Thứ tự hiển thị

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ExamMatrixDetail> ExamMatrixDetails { get; set; } = new List<ExamMatrixDetail>();
    }

    // Bảng trung gian cho ExamMatrix - Chapter (n-n)
    public class ExamMatrixDetail
    {
        [Key]
        public int Id { get; set; }
        
        [Column("exammatrixid")]
        public string ExamMatrixId { get; set; } = string.Empty;
        
        [Column("chapterid")]
        public int ChapterId { get; set; }
        
        [Column("questioncount")]
        public int QuestionCount { get; set; } // Số câu hỏi của chapter này trong đề
        
        [Column("difficultylevel")]
        [StringLength(50)]
        public string DifficultyLevel { get; set; } = "medium"; // easy, medium, hard
        
        // Navigation properties
        [ForeignKey("ExamMatrixId")]
        public virtual ExamMatrix ExamMatrix { get; set; } = null!;
        
        [ForeignKey("ChapterId")]
        public virtual Chapter Chapter { get; set; } = null!;
    }
} 