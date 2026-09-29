using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Workspace
{
    public record SelectWorkspaceDto ([Required(ErrorMessage = "Munkaterület azonosítója kötelező!")] Guid workspaceId);
}
