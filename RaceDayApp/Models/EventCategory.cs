using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApp.Models
{
    [Table("EventCategories")]
    public class EventCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventCategoryID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [ForeignKey("EventID")]
        public Event Event { get; set; } = null!;

        [ForeignKey("CategoryID")]
        public Category Category { get; set; } = null!;
    }
}