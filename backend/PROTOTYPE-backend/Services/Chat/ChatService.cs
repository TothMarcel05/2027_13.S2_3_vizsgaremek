using Microsoft.EntityFrameworkCore;
using PROTOTYPE_backend.Data;
using PROTOTYPE_backend.DTOs.Chat;
using PROTOTYPE_backend.Models;

namespace PROTOTYPE_backend.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        // 1. DIVERZ SZOBÁK LÉTREHOZÁSA (direct, group, project_channel)
        public async Task<ChatRoomDto> CreateRoomAsync(Guid userId, CreateChatRoomDto dto)
        {
            // Ha direct (1) csevegés, megnézzük létezik-e már a két fél között ebben a workspace-ben
            if (dto.Type == 1 && dto.MemberUserIds?.Count > 0)
            {
                return await GetOrCreateDirectRoomAsync(userId, dto.WorkspaceId, dto.MemberUserIds[0]);
            }

            var room = new ChatRoom
            {
                Id = Guid.NewGuid(),
                WorkspaceId = dto.WorkspaceId,
                ProjectId = dto.ProjectId,
                Name = dto.Name ?? (dto.Type == 3 ? "Projekt Csatorna" : "Csoportos Csevegés"),
                Type = dto.Type,
                IsPrivate = dto.IsPrivate,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatRooms.Add(room);

            // Létrehozó hozzáadása
            _context.ChatRoomMembers.Add(new ChatRoomMember
            {
                RoomId = room.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            // Tagok hozzáadása (ha group/direct)
            if (dto.MemberUserIds != null)
            {
                foreach (var memberId in dto.MemberUserIds.Distinct())
                {
                    if (memberId != userId)
                    {
                        _context.ChatRoomMembers.Add(new ChatRoomMember
                        {
                            RoomId = room.Id,
                            UserId = memberId,
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return new ChatRoomDto(
                room.Id,
                room.WorkspaceId,
                room.ProjectId,
                room.Name,
                room.Type,
                room.IsPrivate,
                room.CreatedBy,
                room.CreatedAt,
                null
            );
        }

        // 2. DIRECT (1:1) SZOBA KERESÉSE VAGY LÉTREHOZÁSA
        public async Task<ChatRoomDto> GetOrCreateDirectRoomAsync(Guid userId, Guid workspaceId, Guid targetUserId)
        {
            var existingRoomId = await _context.ChatRoomMembers
                .Where(m => m.Room.WorkspaceId == workspaceId && m.Room.Type == 1 && (m.UserId == userId || m.UserId == targetUserId))
                .GroupBy(m => m.RoomId)
                .Where(g => g.Count() == 2)
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            if (existingRoomId != Guid.Empty)
            {
                var existingRoom = await _context.ChatRooms.FirstAsync(r => r.Id == existingRoomId);
                return new ChatRoomDto(
                    existingRoom.Id,
                    existingRoom.WorkspaceId,
                    existingRoom.ProjectId,
                    existingRoom.Name,
                    existingRoom.Type,
                    existingRoom.IsPrivate,
                    existingRoom.CreatedBy,
                    existingRoom.CreatedAt,
                    null
                );
            }

            var targetUser = await _context.AppUsers.FindAsync(targetUserId);
            var newRoom = new ChatRoom
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Name = targetUser != null ? targetUser.Name : "Privát beszélgetés",
                Type = 1, // direct
                IsPrivate = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatRooms.Add(newRoom);

            _context.ChatRoomMembers.AddRange(
                new ChatRoomMember { RoomId = newRoom.Id, UserId = userId, JoinedAt = DateTime.UtcNow },
                new ChatRoomMember { RoomId = newRoom.Id, UserId = targetUserId, JoinedAt = DateTime.UtcNow }
            );

            await _context.SaveChangesAsync();

            return new ChatRoomDto(
                newRoom.Id,
                newRoom.WorkspaceId,
                newRoom.ProjectId,
                newRoom.Name,
                newRoom.Type,
                newRoom.IsPrivate,
                newRoom.CreatedBy,
                newRoom.CreatedAt,
                null
            );
        }

        // 3. SZOBÁK LISTÁZÁSA WORKSPACE ALAPJÁN
        public async Task<IEnumerable<ChatRoomDto>> GetUserRoomsByWorkspaceAsync(Guid userId, Guid workspaceId)
        {
            return await _context.ChatRoomMembers
                .AsNoTracking()
                .Where(m => m.UserId == userId && m.Room.WorkspaceId == workspaceId)
                .Select(m => new ChatRoomDto(
                    m.Room.Id,
                    m.Room.WorkspaceId,
                    m.Room.ProjectId,
                    m.Room.Name,
                    m.Room.Type,
                    m.Room.IsPrivate,
                    m.Room.CreatedBy,
                    m.Room.CreatedAt,
                    m.Room.ChatMessages
                        .OrderByDescending(msg => msg.CreatedAt)
                        .Select(msg => new ChatMessageDto(
                            msg.Id,
                            msg.RoomId,
                            msg.SenderId,
                            msg.Sender != null ? msg.Sender.Name : null,
                            msg.Sender != null ? msg.Sender.AvatarUrl : null,
                            msg.MessageText,
                            msg.ParentId,
                            msg.CreatedAt,
                            msg.ChatMessageAttachments.Select(a => new ChatMessageAttachmentDto(
                                a.Id, a.FileUrl, a.FileName, a.StorageNodeId
                            )).ToList()
                        ))
                        .FirstOrDefault()
                ))
                .ToListAsync();
        }

        // 4. ÜZENET MENTÉSE ÉS MELLÉKLETEK CSATOLÁSA
        public async Task<ChatMessageDto?> SaveMessageAsync(Guid userId, SendMessageDto dto)
        {
            var isMember = await IsUserMemberOfRoomAsync(userId, dto.RoomId);
            if (!isMember) return null;

            var user = await _context.AppUsers.FindAsync(userId);

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                RoomId = dto.RoomId,
                SenderId = userId,
                ParentId = dto.ParentId,
                MessageText = dto.MessageText,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);

            var attachmentDtos = new List<ChatMessageAttachmentDto>();

            // Mellékletek mentése (ha vannak)
            if (dto.Attachments != null && dto.Attachments.Count > 0)
            {
                foreach (var att in dto.Attachments)
                {
                    var attachment = new ChatMessageAttachment
                    {
                        Id = Guid.NewGuid(),
                        MessageId = message.Id,
                        FileUrl = att.FileUrl,
                        FileName = att.FileName,
                        StorageNodeId = att.StorageNodeId
                    };
                    _context.ChatMessageAttachments.Add(attachment);
                    attachmentDtos.Add(new ChatMessageAttachmentDto(attachment.Id, attachment.FileUrl, attachment.FileName, attachment.StorageNodeId));
                }
            }

            await _context.SaveChangesAsync();

            return new ChatMessageDto(
                message.Id,
                message.RoomId,
                message.SenderId,
                user?.Name,
                user?.AvatarUrl,
                message.MessageText,
                message.ParentId,
                message.CreatedAt,
                attachmentDtos
            );
        }

        // 5. ÜZENETHISTÓRIA LEKÉRDEZÉSE
        public async Task<IEnumerable<ChatMessageDto>> GetRoomMessagesAsync(Guid userId, Guid roomId, int page = 1, int limit = 50)
        {
            var isMember = await IsUserMemberOfRoomAsync(userId, roomId);
            if (!isMember) return Enumerable.Empty<ChatMessageDto>();

            return await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(m => new ChatMessageDto(
                    m.Id,
                    m.RoomId,
                    m.SenderId,
                    m.Sender != null ? m.Sender.Name : null,
                    m.Sender != null ? m.Sender.AvatarUrl : null,
                    m.MessageText,
                    m.ParentId,
                    m.CreatedAt,
                    m.ChatMessageAttachments.Select(a => new ChatMessageAttachmentDto(
                        a.Id, a.FileUrl, a.FileName, a.StorageNodeId
                    )).ToList()
                ))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // 6. TAGSÁG ELLENŐRZÉSE
        public async Task<bool> IsUserMemberOfRoomAsync(Guid userId, Guid roomId)
        {
            return await _context.ChatRoomMembers
                .AnyAsync(m => m.UserId == userId && m.RoomId == roomId);
        }
    }
}