using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Notification;

namespace PROTOTYPE_backend.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET /notifications?page=1&limit=20&unreadOnly=true
        public async Task<PagedResultDto<NotificationResultDto>> GetNotificationsAsync(Guid userId, int page = 1, int limit = 20, bool unreadOnly = false)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new KeyNotFoundException("A felhasználó nem található!");

            // Alap lekérdezés felépítése
            var query = _context.UserNotifications
                .Where(n => n.UserId == userId); //[cite: 1]

            // Szűrés opció szerint: ha unreadOnly = true, csak az olvasatlanokat kérjük le[cite: 2]
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            // Összes találat megszámlálása a lapozási metaadathoz[cite: 2]
            var totalCount = await query.CountAsync();

            // Lapozás végrehajtása és DTO-vá alakítás
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
                throw new KeyNotFoundException("A felhasználó nem található!");

            return await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead) //[cite: 1]
                .CountAsync(); //[cite: 2]
        }

        // 3. PUT /notifications/{id}/read
        public async Task<bool> MarkNotificationAsReadAsync(Guid userId, Guid notificationId)
        {
            // Biztonság: Ellenőrizzük, hogy a megadott értesítés az adott felhasználóé-e[cite: 1]
            var rowsAffected = await _context.UserNotifications
                .Where(n => n.Id == notificationId && n.UserId == userId) //[cite: 1]
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true)); //[cite: 2]

            if (rowsAffected == 0)
                throw new KeyNotFoundException("Az értesítés nem található vagy nem ehhez a felhasználóhoz tartozik!");

            return true;
        }

        // 4. PUT /notifications/read-all
        public async Task<bool> MarkAllNotificationsAsReadAsync(Guid userId)
        {
            var userExists = await _context.AppUsers.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new KeyNotFoundException("A felhasználó nem található!");

            // Egyetlen SQL UPDATE utasítással az összes olvasatlant true-ra állítjuk[cite: 2]
            await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead) //[cite: 1]
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true)); //[cite: 2]

            return true;
        }
    }
}