using Microsoft.AspNetCore.Mvc;
using Spice.Models;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
            IndexViewModel IndexVM = new IndexViewModel()
            {
                MenuItems = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync(),
                Categories = await _db.Category.ToListAsync(),
                Coupons = await _db.Coupon.Where(c => c.IsActivated == true).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity) this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var userShoppingCartCount = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, userShoppingCartCount);
            }

            return View(IndexVM);
        }

        //GET - Details
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var menuItemFromDb = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory)
                .Where(m => m.Id == id).FirstOrDefaultAsync();
            ShoppingCart shoppingCart = new ShoppingCart()
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };

            return View(shoppingCart);

        }

        //POST - Details
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart cartObject)
        {
            cartObject.Id = 0;

            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cartObject.ApplicationUserId = claim.Value;

                ShoppingCart cartFromDb = await _db.ShoppingCart.Where(c =>
                        c.ApplicationUserId == cartObject.ApplicationUserId && c.MenuItemId == cartObject.MenuItemId)
                    .FirstOrDefaultAsync();

                if (cartFromDb == null)
                {
                    await _db.ShoppingCart.AddAsync(cartObject);
                }
                else
                {
                    cartFromDb.Count = cartFromDb.Count + cartObject.Count;
                }

                await _db.SaveChangesAsync();
                var count = _db.ShoppingCart.Where(c => c.ApplicationUserId == cartObject.ApplicationUserId).ToList()
                    .Count;

                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

                return RedirectToAction("Index");
            }
            else
            {
                var menuItemFromDb = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory)
                    .Where(m => m.Id == cartObject.MenuItemId).FirstOrDefaultAsync();
                ShoppingCart shoppingCart = new ShoppingCart()
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };

                return View(cartObject);
            }

        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
