using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        [Required]

        public string Location { get; set; } = string.Empty;
        [Required]
        public string major { get; set; } = string.Empty;
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        public int Limit { get; set; }
        public ICollection<Roll> Rolls { get; set; } = new List<Roll>();
    }
}
