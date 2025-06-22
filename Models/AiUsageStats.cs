using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("aiusagestats")]
    public class AiUsageStats
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("date")]
        public DateOnly Date { get; set; }

        [Required]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        [Column("totalrequests")]
        public int TotalRequests { get; set; } = 0;

        [Column("successfulrequests")]
        public int SuccessfulRequests { get; set; } = 0;

        [Column("failedrequests")]
        public int FailedRequests { get; set; } = 0;

        [Column("totaltokens")]
        public int TotalTokens { get; set; } = 0;

        [Column("totalcost")]
        public decimal TotalCost { get; set; } = 0;

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 