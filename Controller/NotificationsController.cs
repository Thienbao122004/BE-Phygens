using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly PhygensContext _context;

        public NotificationsController(PhygensContext context)
        {
            _context = context;
        }

        // GET: notifications - Get user notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] string? userId = null,
            [FromQuery] bool includeRead = true,
            [FromQuery] string? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();

                // Filter by user (if null, get broadcast notifications)
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.UserId == userId || n.UserId == null);
                }
                else
                {
                    query = query.Where(n => n.UserId == null); // Only broadcast
                }

                // Filter by read status
                if (!includeRead)
                {
                    query = query.Where(n => !n.IsRead);
                }

                // Filter by type
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(n => n.Type == type);
                }

                // Filter out expired notifications
                query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

                // Order by priority and creation date
                query = query.OrderByDescending(n => n.Priority)
                           .ThenByDescending(n => n.CreatedAt);

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        notificationId = n.NotificationId,
                        title = n.Title,
                        message = n.Message,
                        type = n.Type,
                        priority = n.Priority,
                        isRead = n.IsRead,
                        data = n.Data,
                        createdAt = n.CreatedAt,
                        readAt = n.ReadAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Notifications retrieved successfully",
                    data = notifications,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving notifications",
                    error = ex.Message
                });
            }
        }

        // GET: notifications/unread-count - Get unread notifications count
        [HttpGet("unread-counts")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] string? userId = null)
        {
            try
            {
                var query = _context.Notifications.Where(n => !n.IsRead);

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.UserId == userId || n.UserId == null);
                }
                else
                {
                    query = query.Where(n => n.UserId == null);
                }

                // Filter out expired notifications
                query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

                var count = await query.CountAsync();

                return Ok(new
                {
                    success = true,
                    data = new { unreadCount = count }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting unread count",
                    error = ex.Message
                });
            }
        }

        // POST: notifications - Create notification
        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var notification = new Notification
                {
                    NotificationId = $"notif_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Random.Shared.Next(1000, 9999)}",
                    UserId = request.UserId, // null for broadcast
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    Priority = request.Priority,
                    Data = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                    ExpiresAt = request.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Notification created successfully",
                    data = new
                    {
                        notificationId = notification.NotificationId,
                        title = notification.Title,
                        message = notification.Message,
                        type = notification.Type,
                        createdAt = notification.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating notification",
                    error = ex.Message
                });
            }
        }

        // PUT: notifications/{id}/mark-read - Mark notification as read
        [HttpPut("{id}/read-status")]
        public async Task<IActionResult> MarkAsRead(string id, [FromQuery] string? userId = null)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == id);

                if (notification == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Notification not found"
                    });
                }

                // Check if user has permission to mark this notification as read
                if (!string.IsNullOrEmpty(notification.UserId) && 
                    !string.IsNullOrEmpty(userId) && 
                    notification.UserId != userId)
                {
                    return Forbid("You don't have permission to mark this notification as read");
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Notification marked as read",
                    data = new { notificationId = id, readAt = notification.ReadAt }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error marking notification as read",
                    error = ex.Message
                });
            }
        }

        // DELETE: notifications/{id} - Delete notification
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(string id)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == id);

                if (notification == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Notification not found"
                    });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Notification deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting notification",
                    error = ex.Message
                });
            }
        }
    }

    // DTOs
    public class CreateNotificationRequest
    {
        public string? UserId { get; set; } // null for broadcast
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info";
        public int Priority { get; set; } = 1;
        public object? Data { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
} 