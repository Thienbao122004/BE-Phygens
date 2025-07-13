using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("notificationid")]
        public string NotificationId { get; set; } = string.Empty;

        [Column("userid")]
        public string? UserId { get; set; } // NULL means broadcast to all users

        [Required]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("type")]
        public string Type { get; set; } = string.Empty; // exam_created, exam_updated, exam_deleted, system, info, warning, error

        [Column("priority")]
        public int Priority { get; set; } = 1; // 1=low, 5=high

        [Column("isread")]
        public bool IsRead { get; set; } = false;

        [Column("data", TypeName = "jsonb")]
        public string? Data { get; set; } // JSON data

        [Column("createdat")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("readat")]
        public DateTime? ReadAt { get; set; }

        [Column("expiresat")]
        public DateTime? ExpiresAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    public enum NotificationType
    {
        ExamCreated,
        ExamUpdated,
        ExamDeleted,
        System,
        Info,
        Warning,
        Error
    }

    public enum NotificationPriority
    {
        Low = 1,
        Normal = 2,
        Medium = 3,
        High = 4,
        Critical = 5
    }
} 