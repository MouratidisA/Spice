using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
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


            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value).ToList();

            DetailCart.CartList = cart.Any() ? cart.ToList() : new List<ShoppingCart>();

            foreach (var cartItem in DetailCart.CartList)
            {
                cartItem.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == cartItem.MenuItemId);
                DetailCart.OrderHeader.OrderTotal =
                    DetailCart.OrderHeader.OrderTotal + (cartItem.MenuItem.Price * cartItem.Count);
                cartItem.MenuItem.Description = SD.ConvertToRawHtml(cartItem.MenuItem.Description);
                if (cartItem.MenuItem.Description.Length > 100)
                {
                    cartItem.MenuItem.Description = cartItem.MenuItem.Description.Substring(0, 99) + "...";
                }
            }

            DetailCart.OrderHeader.OrderTotalOriginal = DetailCart.OrderHeader.OrderTotal;


            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                DetailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon
                    .Where(c => c.Name.ToLower() == DetailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailCart.OrderHeader.OrderTotal =
                    SD.DiscountedPrice(couponFromDb, DetailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(DetailCart);
        }


        public async Task<IActionResult> Summary()
        {
            DetailCart = new OrderDetailsCart()
            {
                OrderHeader = new OrderHeader()
            };
            DetailCart.OrderHeader.OrderTotal = 0;


            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser =
                await _db.ApplicationUser.Where(u => u.Id == claim.Value).FirstOrDefaultAsync();

            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value).ToList();

            DetailCart.CartList = cart.Any() ? cart.ToList() : new List<ShoppingCart>();

            foreach (var cartItem in DetailCart.CartList)
            {
                cartItem.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == cartItem.MenuItemId);
                DetailCart.OrderHeader.OrderTotal = DetailCart.OrderHeader.OrderTotal + (cartItem.MenuItem.Price * cartItem.Count);

            }

            DetailCart.OrderHeader.OrderTotalOriginal = DetailCart.OrderHeader.OrderTotal;

            DetailCart.OrderHeader.PickupName = applicationUser.Name;
            DetailCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            DetailCart.OrderHeader.PickUpTime = DateTime.Now;

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                DetailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon
                    .Where(c => c.Name.ToLower() == DetailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailCart.OrderHeader.OrderTotal =
                    SD.DiscountedPrice(couponFromDb, DetailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(DetailCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            DetailCart.CartList = await _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value).ToListAsync();

            DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            DetailCart.OrderHeader.OrderDate = DateTime.Now;
            DetailCart.OrderHeader.UserId = claim.Value;
            DetailCart.OrderHeader.Status = SD.PaymentStatusPending;
            DetailCart.OrderHeader.PickUpTime = Convert.ToDateTime(DetailCart.OrderHeader.PickUpDate.ToShortDateString() + " " + DetailCart.OrderHeader.PickUpTime.ToShortTimeString());

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();

            _db.OrderHeader.Add(DetailCart.OrderHeader);
            await _db.SaveChangesAsync();

            DetailCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var cartItem in DetailCart.CartList)
            {
                cartItem.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == cartItem.MenuItemId);
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = cartItem.MenuItemId,
                    OrderId = DetailCart.OrderHeader.Id,
                    Description = cartItem.MenuItem.Description,
                    Name = cartItem.MenuItem.Name,
                    Price = cartItem.MenuItem.Price,
                    Count = cartItem.Count
                };
                DetailCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);

            }

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                DetailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == DetailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, DetailCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                DetailCart.OrderHeader.OrderTotal = DetailCart.OrderHeader.OrderTotalOriginal;
            }

            DetailCart.OrderHeader.CouponDiscount =
                DetailCart.OrderHeader.OrderTotalOriginal - DetailCart.OrderHeader.OrderTotal;


            _db.ShoppingCart.RemoveRange(DetailCart.CartList);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);

            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
            //return RedirectToAction("Confirm", "Order", new { id = DetailCart.OrderHeader.Id });
        }

        public IActionResult AddCoupon()
        {
            if (DetailCart.OrderHeader.CouponCode == null)
            {
                DetailCart.OrderHeader.CouponCode = "";
            }

            HttpContext.Session.SetString(SD.ssCouponCode, DetailCart.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {

            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart != null)
            {
                cart.Count += 1;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Minus(int cartId)
        {

            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);


            if (cart.Count == 1)
            {
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();

                var cartCount = _db.ShoppingCart.Where(c => c.ApplicationUserId == cart.ApplicationUserId).ToList()
                    .Count();

                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cartCount);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Remove(int cartId)
        {

            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(c => c.Id == cartId);

            _db.ShoppingCart.Remove(cart);
            await _db.SaveChangesAsync();

            var cartCount = _db.ShoppingCart.Where(c => c.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cartCount);

            return RedirectToAction(nameof(Index));
        }
    }
}