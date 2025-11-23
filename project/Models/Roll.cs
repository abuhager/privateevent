namespace project.Models
{
    public class Roll
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int EventId { get; set; }
        public Event Event { get; set; }
        public string States { get; set; }
    }
}
