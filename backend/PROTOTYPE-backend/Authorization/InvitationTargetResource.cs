namespace PROTOTYPE_backend.Authorization
{
    // Erőforrás-alapú ellenőrzéshez: a meghívó célja (1 = Workspace, 2 = Project)
    public record InvitationTargetResource(sbyte Type, Guid TargetId);
}
