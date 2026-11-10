namespace PROTOTYPE_backend.DTOs.Notification
{
    public record NotificationResultDto
        (
            string content,
            bool isRead,
            DateTime? createdAt
        );
}
