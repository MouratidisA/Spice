using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Customer.Controllers
{
    public class CartController : Controller
    {

        private readonly ApplicationDbContext _db;

        [BindProperty]
        public OrderDetailsCart DetailCart { get; set; }

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            DetailCart = new OrderDetailsCart()
            {
                OrderHeader = new OrderHeader()
            };
            DetailCart.OrderHeader.OrderTotal = 0;


            var claimsIdentity = (ClaimsIdentity) this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value);

            if (cart != null)
            {
                DetailCart.CartList = cart.ToList(); 
            }

            foreach (var cartItem in DetailCart.CartList)
            {
                cartItem.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == cartItem.MenuItemId);
                DetailCart.OrderHeader.OrderTotal =
                    DetailCart.OrderHeader.OrderTotal + (cartItem.MenuItem.Price * cartItem.Count);
                cartItem.MenuItem.Description = SD.ConvertToRawHtml(cartItem.MenuItem.Description);
                if (cartItem.MenuItem.Description.Length > 100)
                {
                    cartItem.MenuItem.Description = cartItem.MenuItem.Description.Substring(0,99) + "...";
                }
            }

            DetailCart.OrderHeader.OrderTotalOriginal = DetailCart.OrderHeader.OrderTotal;

            return View(DetailCart);
        }
    }
}