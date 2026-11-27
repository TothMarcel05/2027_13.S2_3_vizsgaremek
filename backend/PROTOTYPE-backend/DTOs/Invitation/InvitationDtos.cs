using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Invitations
{
    // Meghívó kiküldése
    public record CreateInvitationDto(
        [Required]
        sbyte Type, // 1 = workspace, 2 = project

        [Required]
        Guid TargetId, // workspace_id vagy project_id

        [Required]
        [EmailAddress]
        string InvitedEmail,

        Guid? RoleId
    );

    // Meghívó válasz DTO
    public record InvitationResponseDto(
        Guid Id,
        sbyte Type,
        Guid TargetId,
        string InvitedEmail,
        Guid? InvitedUserId,
        Guid? RoleId,
        Guid InvitedBy,
        sbyte Status, // 1 = pending, 2 = accepted, 3 = refused, 4 = expired
        string Token,
        DateTime ExpiresAt,
        DateTime CreatedAt,
        DateTime? RespondedAt
    );

    // Token alapján történő publikus ellenőrzés
    public record InvitationDetailDto(
        Guid Id,
        sbyte Type,
        Guid TargetId,
        string TargetName,
        string InvitedEmail,
        string InvitedByName,
        DateTime ExpiresAt,
        sbyte Status
    );
}