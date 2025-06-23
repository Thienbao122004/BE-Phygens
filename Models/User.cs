using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        [Column("userid")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("fullname")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Column("role")]
        public string Role { get; set; } = string.Empty; //  student, admin

        [Required]
        [Column("passwordhash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Exam> CreatedExams { get; set; } = new List<Exam>();
        public virtual ICollection<Question> CreatedQuestions { get; set; } = new List<Question>();
        public virtual ICollection<Explanation> CreatedExplanations { get; set; } = new List<Explanation>();
        public virtual ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
        public virtual ICollection<LearningProgress> LearningProgresses { get; set; } = new List<LearningProgress>();
    }
} 