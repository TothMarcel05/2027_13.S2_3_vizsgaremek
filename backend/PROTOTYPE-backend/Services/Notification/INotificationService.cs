using PROTOTYPE_backend.DTOs.Notification;

namespace PROTOTYPE_backend.Services.Notification
{
    public interface INotificationService
    {
        /// <summary>
        /// Értesítések lekérése lapozással és opció szerinti szűréssel.
        /// </summary>
        Task<PagedResultDto<NotificationResultDto>> GetNotificationsAsync(Guid userId, int page = 1, int limit = 20, bool unreadOnly = false);

        /// <summary>
        /// Olvasatlan értesítések számának lekérése (jelvény/badge ikonhoz).
        /// </summary>
        Task<int> GetUnreadNotificationsCountAsync(Guid userId);

        /// <summary>
        /// Egy konkrét értesítés olvasottá jelölése.
        /// </summary>
        Task<bool> MarkNotificationAsReadAsync(Guid userId, Guid notificationId);

        /// <summary>
        /// Az összes értesítés olvasottá jelölése.
        /// </summary>
        Task<bool> MarkAllNotificationsAsReadAsync(Guid userId);
    }
}