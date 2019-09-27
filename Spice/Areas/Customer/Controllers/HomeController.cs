using Microsoft.AspNetCore.Mvc;
using Spice.Models;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;

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
                MenuItems = await _db.MenuItem.Include(m=>m.Category).Include(m => m.SubCategory).ToListAsync(),
                Categories = await _db.Category.ToListAsync(),
                Coupons = await _db.Coupon.Where(c=>c.IsActivated==true).ToListAsync()
            };
            return View(IndexVM);
        }

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
