using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PROTOTYPE_backend.DTOs.Chat;
using PROTOTYPE_backend.Extensions;
using PROTOTYPE_backend.Services.Chat;

namespace PROTOTYPE_backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task JoinRoom(string roomId)
        {
            if (Guid.TryParse(roomId, out var roomGuid))
            {
                var userId = Context.User.GetUserId();
                var isMember = await _chatService.IsUserMemberOfRoomAsync(userId, roomGuid);

                if (isMember)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                }
            }
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        }

        public async Task SendMessage(SendMessageDto dto)
        {
            var userId = Context.User.GetUserId();
            var savedMessage = await _chatService.SaveMessageAsync(userId, dto);

            if (savedMessage != null)
            {
                await Clients.Group(dto.RoomId.ToString())
                    .SendAsync("ReceiveMessage", savedMessage);
            }
        }
    }
}