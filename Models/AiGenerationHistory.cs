using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("aigenerationhistory")]
    public class AiGenerationHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("sessionid")]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [Column("userid")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [Column("modelname")]
        public string ModelName { get; set; } = string.Empty;

        [Required]
        [Column("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [Column("response")]
        public string? Response { get; set; }

        [Column("tokensused")]
        public int? TokensUsed { get; set; }

        [Column("generationtimems")]
        public int? GenerationTimeMs { get; set; }

        [Column("success")]
        public bool Success { get; set; } = false;

        [Column("errormessage")]
        public string? ErrorMessage { get; set; }

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
} 