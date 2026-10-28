using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Chat
{
    // Szoba létrehozási kérelem
    public record CreateChatRoomDto(
        [Required]
        Guid WorkspaceId,

        string? Name,

        [Required]
        sbyte Type, // 1 = direct, 2 = group, 3 = project_channel

        bool IsPrivate = false,
        Guid? ProjectId = null,
        List<Guid>? MemberUserIds = null
    );

    // Szoba adatai
    public record ChatRoomDto(
        Guid Id,
        Guid WorkspaceId,
        Guid? ProjectId,
        string? Name,
        sbyte Type,
        bool IsPrivate,
        Guid? CreatedBy,
        DateTime? CreatedAt,
        ChatMessageDto? LastMessage
    );

    // Melléklet DTO
    public record ChatMessageAttachmentDto(
        Guid Id,
        string FileUrl,
        string FileName,
        Guid? StorageNodeId
    );

    // Üzenet küldése (API & SignalR)
    public record SendMessageDto(
        [Required]
        Guid RoomId,

        [Required]
        [MinLength(1)]
        string MessageText,

        Guid? ParentId = null,
        List<ChatMessageAttachmentDto>? Attachments = null
    );

    // Üzenet válasz DTO
    public record ChatMessageDto(
        Guid Id,
        Guid RoomId,
        Guid? SenderId,
        string? SenderName,
        string? SenderAvatarUrl,
        string MessageText,
        Guid? ParentId,
        DateTime? CreatedAt,
        List<ChatMessageAttachmentDto> Attachments
    );
}