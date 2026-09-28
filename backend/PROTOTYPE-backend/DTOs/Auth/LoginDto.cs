using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Auth
{
    public record LoginDto
    (
        [Required]
        [EmailAddress(ErrorMessage = "Az email cím formátuma nem megfelelő!")]
        string Email,
        [Required(ErrorMessage ="A jelszó megadása kötelező!")]
        string Password
    );
}
