using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("adaptivelearningdata")]
    public class AdaptiveLearningData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("userid")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("chapterid")]
        public int ChapterId { get; set; }

        [Column("difficultypreference")]
        public string DifficultyPreference { get; set; } = "medium";

        [Column("weaktopics")]
        public string[]? WeakTopics { get; set; }

        [Column("strongtopics")]
        public string[]? StrongTopics { get; set; }

        [Column("recommendeddifficulty")]
        public string? RecommendedDifficulty { get; set; }

        [Column("performancetrend")]
        public string? PerformanceTrend { get; set; } // improving, declining, stable

        [Column("lastupdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ChapterId")]
        public virtual Chapter Chapter { get; set; } = null!;
    }
} 