using System.ComponentModel.DataAnnotations;

namespace PROTOTYPE_backend.DTOs.Auth
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(30, MinimumLength =5)]
        public string Username { get; set; }
        [Required]
        [StringLength (30, MinimumLength = 5)]
        public string Password { get; set; }
        [Required]
        [StringLength(100,  MinimumLength = 5)]
        [EmailAddress(ErrorMessage = "Az email cím formátum nem megfelő")]
        public string Email { get; set; }
    }
}
