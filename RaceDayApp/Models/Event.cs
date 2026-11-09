using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApp.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        public int OrganizerID { get; set; }
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string Distance { get; set; }
        public string EventType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("OrganizerID")]
        public User Organizer { get; set; }
    }
}