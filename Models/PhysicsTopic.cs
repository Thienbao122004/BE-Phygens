using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("physicstopic")]
    public class PhysicsTopic
    {
        [Key]
        [Column("topicid")]
        public string TopicId { get; set; } = string.Empty;

        [Required]
        [Column("topicname")]
        public string TopicName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("gradelevel")]
        public string GradeLevel { get; set; } = string.Empty;

        [Column("displayorder")]
        public int DisplayOrder { get; set; }

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<LearningProgress> LearningProgresses { get; set; } = new List<LearningProgress>();
    }
} 