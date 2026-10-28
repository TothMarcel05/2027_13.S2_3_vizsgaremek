using PROTOTYPE_backend.DTOs.Chat;

namespace PROTOTYPE_backend.Services.Chat
{
    public interface IChatService
    {
        Task<ChatRoomDto> CreateRoomAsync(Guid userId, CreateChatRoomDto dto);

        Task<ChatRoomDto> GetOrCreateDirectRoomAsync(Guid userId, Guid workspaceId, Guid targetUserId);

        Task<IEnumerable<ChatRoomDto>> GetUserRoomsByWorkspaceAsync(Guid userId, Guid workspaceId);

        Task<ChatMessageDto?> SaveMessageAsync(Guid userId, SendMessageDto dto);

        Task<IEnumerable<ChatMessageDto>> GetRoomMessagesAsync(Guid userId, Guid roomId, int page = 1, int limit = 50);

        Task<bool> IsUserMemberOfRoomAsync(Guid userId, Guid roomId);
    }
}