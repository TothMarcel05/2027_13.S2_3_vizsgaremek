using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Auth
{
    public record RegisterDto
    (
        [Required]
        [StringLength(30, MinimumLength =5)]
        string Username,
        [Required]
        [StringLength (30, MinimumLength = 5)]
        string Password,
        [Required]
        [StringLength(100,  MinimumLength = 5)]
        [EmailAddress(ErrorMessage = "Az email cím formátum nem megfelelő!")]
        string Email
    );
}
