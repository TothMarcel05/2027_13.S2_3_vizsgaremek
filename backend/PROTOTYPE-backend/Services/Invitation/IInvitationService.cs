using PROTOTYPE_backend.DTOs.Invitations;

namespace PROTOTYPE_backend.Services.Invitations
{
    public interface IInvitationService
    {
        // Meghívó létrehozása és kiküldése
        Task<InvitationResponseDto> CreateInvitationAsync(Guid currentUserId, CreateInvitationDto dto);

        // A bejelentkezett felhasználó saját meghívóinak lekérése (email alapján)
        Task<IEnumerable<InvitationResponseDto>> GetMyInvitationsAsync(Guid currentUserId);

        // Egy célpont (Workspace vagy Project) függőben lévő meghívói
        Task<IEnumerable<InvitationResponseDto>> GetPendingInvitationsByTargetAsync(Guid targetId, sbyte type);

        // Meghívó részleteinek lekérése Token alapján (pl. emailben lévő linkből)
        Task<InvitationDetailDto?> GetInvitationByTokenAsync(string token);

        // Meghívó elfogadása
        Task<bool> AcceptInvitationAsync(Guid currentUserId, string token);

        // Meghívó elutasítása
        Task<bool> RefuseInvitationAsync(Guid currentUserId, string token);

        // Meghívó visszavonása/törlése (a meghívó által)
        Task<bool> CancelInvitationAsync(Guid currentUserId, Guid invitationId);
    }
}