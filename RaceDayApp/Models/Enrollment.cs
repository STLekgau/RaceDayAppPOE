using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApp.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentID { get; set; }
        public int ParticipantID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [ForeignKey("ParticipantID")]
        public User Participant { get; set; }

        [ForeignKey("EventID")]
        public Event Event { get; set; }

        [ForeignKey("CategoryID")]
        public Category Category { get; set; }
    }
}