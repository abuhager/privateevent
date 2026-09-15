using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public string? UImage { get; set; }
        [Required]
        public string Role { get; set; } = string.Empty;
        public ICollection<Roll> Rolls { get; set; } = new List<Roll>();
    }
}
