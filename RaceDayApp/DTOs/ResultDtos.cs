namespace RaceDayApp.DTOs
{
    public class ResultCreateUpdateDto
    {
        public int EnrollmentID { get; set; }
        public string FinishTime { get; set; } = string.Empty; // "hh:mm:ss"
        public int FinishingPosition { get; set; }
    }
}