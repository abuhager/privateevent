using System.ComponentModel.DataAnnotations.Schema;

namespace project.Models
{
    public class Roll
    {
        public int Id { get; set; }

        public string States { get; set; }
        public DateTime BookingTime { get; set; }
        public int UserId { get; set; } 
        [ForeignKey("UserId")] 
        public User User { get; set; } 

        public int EventId { get; set; } 
        [ForeignKey("EventId")]
        public Event Event { get; set; } 
    }
}
