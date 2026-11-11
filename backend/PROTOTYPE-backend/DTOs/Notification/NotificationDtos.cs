namespace PROTOTYPE_backend.DTOs.Notification
{

    #region Request DTOs

    #endregion

    #region Response DTOs
    public record NotificationResultDto
    (
        string content,
        bool isRead,
        DateTime? createdAt
    );

    public record NotificationUnreadResponseDto(
        Guid notificationId,
        string content
    );
    #endregion

}
