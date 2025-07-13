using BE_Phygens.Models;
using System.Text.Json;

namespace BE_Phygens.Services
{
    public interface INotificationService
    {
        Task CreateExamNotificationAsync(string examName, string eventType, object? data = null);
        Task CreateSystemNotificationAsync(string title, string message, int priority = 2);
        Task CreateUserNotificationAsync(string userId, string title, string message, string type = "info", object? data = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly PhygensContext _context;

        public NotificationService(PhygensContext context)
        {
            _context = context;
        }

        public async Task CreateExamNotificationAsync(string examName, string eventType, object? data = null)
        {
            string title = eventType switch
            {
                "exam_created" => "🎉 Đề thi mới được tạo",
                "exam_updated" => "✏️ Đề thi được cập nhật",
                "exam_deleted" => "🗑️ Đề thi đã bị xóa",
                _ => "📋 Thông báo đề thi"
            };

            string message = eventType switch
            {
                "exam_created" => $"Đề thi '{examName}' vừa được tạo thành công.",
                "exam_updated" => $"Đề thi '{examName}' vừa được cập nhật.",
                "exam_deleted" => $"Đề thi '{examName}' đã bị xóa khỏi hệ thống.",
                _ => $"Có thay đổi với đề thi '{examName}'"
            };

            var notification = new Notification
            {
                NotificationId = $"notif_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Random.Shared.Next(1000, 9999)}",
                UserId = null, // Broadcast to all users
                Title = title,
                Message = message,
                Type = eventType,
                Priority = 2, // Normal priority
                Data = data != null ? JsonSerializer.Serialize(data) : null,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Auto-expire after 7 days
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateSystemNotificationAsync(string title, string message, int priority = 2)
        {
            var notification = new Notification
            {
                NotificationId = $"notif_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Random.Shared.Next(1000, 9999)}",
                UserId = null, // Broadcast to all users
                Title = title,
                Message = message,
                Type = "system",
                Priority = priority,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30) // System notifications last longer
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateUserNotificationAsync(string userId, string title, string message, string type = "info", object? data = null)
        {
            var notification = new Notification
            {
                NotificationId = $"notif_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Random.Shared.Next(1000, 9999)}",
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = 2, // Normal priority
                Data = data != null ? JsonSerializer.Serialize(data) : null,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(14) // User notifications expire after 2 weeks
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
} 