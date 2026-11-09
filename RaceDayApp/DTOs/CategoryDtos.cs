namespace RaceDayApp.DTOs
{
    public class CategoryCreateUpdateDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class EventCategoryLinkDto
    {
        public int CategoryID { get; set; }
    }
}