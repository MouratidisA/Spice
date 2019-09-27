using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Spice.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }

        public ShoppingCart()
        {
            Count = 1;
        }

        [NotMapped]
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser ApplicationUser { set; get; }

        public int MenuItemId { get; set; }

        [NotMapped]
        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { set; get; }

        [Range(1,int.MaxValue,ErrorMessage = "Please enter a value greater thn or equal to {1}")]
        public int Count { get; set; }
    }
}
