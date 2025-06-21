using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("learningprogress")]
    public class LearningProgress
    {
        [Key]
        [Column("progressid")]
        public string ProgressId { get; set; } = string.Empty;

        [Required]
        [Column("userid")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("topicid")]
        public string TopicId { get; set; } = string.Empty;

        [Column("attempts")]
        public int Attempts { get; set; } = 0;

        [Column("avgscore")]
        public decimal AvgScore { get; set; } = 0;

        [Column("lastupdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("TopicId")]
        public virtual PhysicsTopic Topic { get; set; } = null!;
    }
} 