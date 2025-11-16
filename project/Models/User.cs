using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public string? UImage { get; set; }
        [Required]
        public string Role { get; set; }
    }
}
