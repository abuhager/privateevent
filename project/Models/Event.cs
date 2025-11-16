using System.ComponentModel.DataAnnotations;

namespace project.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        public string major { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        public int Limit { get; set; }

    }
}
