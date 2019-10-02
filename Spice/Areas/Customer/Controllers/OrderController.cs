using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;

namespace Spice.Areas.Customer.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrderController(ApplicationDbContext db)
        {
            _db = db;            
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity) this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader= await _db.OrderHeader.Include(h=>h.ApplicationUser).FirstOrDefaultAsync(h=>h.Id==id && h.UserId == claim.Value),
                OrderDetails = await _db.OrderDetails.Where(d=>d.OrderId == id).ToListAsync()
            };

            return View(orderDetailsViewModel);

        }

        public IActionResult Index()
        {
            return View();
        }
    }
}