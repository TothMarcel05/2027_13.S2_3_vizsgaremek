using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Users
{
    public record UserModifyRequestDto
        (
        string Name,
        string AvatarURL
        );
}
