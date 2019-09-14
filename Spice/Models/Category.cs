using System.ComponentModel.DataAnnotations;

namespace Spice.Models
{
    public class Category
    {
        [Key]
        public int Id  { get; set; }
        [Required]
        public int Name  { get; set; }
    }
}
