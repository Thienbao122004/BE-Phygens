using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("aimodelconfigs")]
    public class AiModelConfig
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [Column("modelname")]
        public string ModelName { get; set; } = string.Empty;

        [Column("isactive")]
        public bool IsActive { get; set; } = true;

        [Column("maxtokens")]
        public int MaxTokens { get; set; } = 2048;

        [Column("temperature")]
        public decimal Temperature { get; set; } = 0.7m;

        [Column("costper1ktokens")]
        public decimal? CostPer1kTokens { get; set; }

        [Column("ratelimitperminute")]
        public int RateLimitPerMinute { get; set; } = 60;

        [Column("qualityrating")]
        public decimal? QualityRating { get; set; }

        [Column("specialties")]
        public string[]? Specialties { get; set; }

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 