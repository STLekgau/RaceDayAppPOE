using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApp.Models
{
    public class Result
    {
        [Key]
        public int ResultID { get; set; }
        public int EnrollmentID { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int FinishingPosition { get; set; }

        [ForeignKey("EnrollmentID")]
        public Enrollment Enrollment { get; set; }
    }
}