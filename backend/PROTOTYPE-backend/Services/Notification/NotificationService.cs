using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Notification;

namespace PROTOTYPE_backend.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. GET /notifications?page=1&limit=20&unreadOnly=true
        public async Task<PagedResultDto<NotificationResultDto>> GetNotificationsAsync(Guid userId, int page = 1, int limit = 20, bool unreadOnly = false)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Értesítések lekérdezése sikertelen: nem létező felhasználó. UserId: {UserId}", userId);
                throw new KeyNotFoundException("A felhasználó nem található!");
            }

            var query = _context.UserNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(n => new NotificationResultDto(n.Content, n.IsRead, n.CreatedAt))
                .ToListAsync();

            return new PagedResultDto<NotificationResultDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = limit
            };
        }

        // 2. GET /notifications/unread-count
        public async Task<int> GetUnreadNotificationsCountAsync(Guid userId)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Olvasatlan értesítések számának lekérdezése sikertelen: nem létező felhasználó. UserId: {UserId}", userId);
                throw new KeyNotFoundException("A felhasználó nem található!");
            }

            return await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        // 3. PUT /notifications/{id}/read
        public async Task<bool> MarkNotificationAsReadAsync(Guid userId, Guid notificationId)
        {
            var rowsAffected = await _context.UserNotifications
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            if (rowsAffected == 0)
            {
                _logger.LogWarning(
                    "Értesítés olvasottnak jelölése sikertelen: nem található vagy idegen felhasználóhoz tartozik. UserId: {UserId}, NotificationId: {NotificationId}",
                    userId, notificationId);
                throw new KeyNotFoundException("Az értesítés nem található vagy nem ehhez a felhasználóhoz tartozik!");
            }

            _logger.LogDebug("Értesítés olvasottnak jelölve. UserId: {UserId}, NotificationId: {NotificationId}", userId, notificationId);

            return true;
        }

        // 4. PUT /notifications/read-all
        public async Task<bool> MarkAllNotificationsAsReadAsync(Guid userId)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Összes értesítés olvasottnak jelölése sikertelen: nem létező felhasználó. UserId: {UserId}", userId);
                throw new KeyNotFoundException("A felhasználó nem található!");
            }

            var updatedCount = await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            _logger.LogDebug(
                "Összes olvasatlan értesítés olvasottnak jelölve. UserId: {UserId}, FrissítettDarabszám: {UpdatedCount}",
                userId, updatedCount);

            return true;
        }
    }
}