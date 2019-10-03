using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Spice.Utility;
using Stripe;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private int PageSize = 2;

        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(h => h.ApplicationUser).FirstOrDefaultAsync(h => h.Id == id && h.UserId == claim.Value),
                OrderDetails = await _db.OrderDetails.Where(d => d.OrderId == id).ToListAsync()
            };

            return View(orderDetailsViewModel);

        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);


            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };



            List<OrderHeader> OrderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(u => u.UserId == claim.Value).ToListAsync();

            foreach (OrderHeader item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual);
            }
            //TODO Fix paging issue below
            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                //.Skip((productPage - 1) * PageSize)
                //.Take(PageSize).ToList();
                .ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItems = count,
                urlParam = "/Customer/Order/OrderHistory?productPage=:"
            };

            return View(orderListVM);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            List<OrderDetailsViewModel> orderDetailsViewModelList = new List<OrderDetailsViewModel>();
            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Where(u => u.Status == SD.StatusSubmitted || u.Status == SD.StatusInProcess).OrderByDescending(u => u.PickUpTime).ToListAsync();

            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderDetailsViewModelList.Add(individual);
            }


            return View(orderDetailsViewModelList.OrderBy(o => o.OrderHeader.PickUpTime).ToList());
        }

        public IActionResult GetOrderStatus(int Id)
        {
            return PartialView("_OrderStatus", _db.OrderHeader.FirstOrDefault(m => m.Id == Id).Status);

        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel
            {
                OrderHeader = await _db.OrderHeader.FirstOrDefaultAsync(h => h.Id == id),
                OrderDetails = await _db.OrderDetails.Where(d => d.OrderId == id).ToListAsync()
            };
            orderDetailsViewModel.OrderHeader.ApplicationUser =
                await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == orderDetailsViewModel.OrderHeader.UserId);

            return PartialView("_IndividualOrderDetails", orderDetailsViewModel);

        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int id)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(id);
            orderHeader.Status = SD.StatusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int id)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(id);
            orderHeader.Status = SD.StatusReady;
            await _db.SaveChangesAsync();
            //TODO Email logic to notify user that order is ready for pickup
            return RedirectToAction("ManageOrder", "Order");
        }
        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int id)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(id);
            orderHeader.Status = SD.StatusCancelled;
            await _db.SaveChangesAsync();
            return RedirectToAction("ManageOrder", "Order");
        }


        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchEmail = null, string searchPhone = null, string searchName = null)
        {
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");

            param.Append("&searchName=");
            if (searchName != null)
                param.Append(searchName);

            param.Append("&searchPhone=");
            if (searchPhone != null)
                param.Append(searchPhone);

            param.Append("&searchEmail=");
            if (searchEmail != null)
                param.Append(searchEmail);

            List<OrderHeader> orderHeaderList = new List<OrderHeader>();

            if (searchName != null || searchPhone != null || searchEmail != null)
            {
                var user = new ApplicationUser();

                if (searchName != null)
                {
                    orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                        .Where(u => u.PickupName.ToLower().Contains(searchName.ToLower()))
                        .OrderByDescending(o => o.OrderDate)
                        .ToListAsync();
                }
                else
                {
                    if (searchEmail != null)
                    {
                        user = await _db.ApplicationUser.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower()))
                            .FirstOrDefaultAsync();
                        orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                            .Where(u => u.UserId == user.Id)
                            .OrderByDescending(o => o.OrderDate)
                            .ToListAsync();
                    }                  
                    else
                    {
                        if (searchPhone != null)
                        {
                            orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                .Where(u => u.PhoneNumber.Contains(searchPhone))
                                .OrderByDescending(o => o.OrderDate)
                                .ToListAsync();
                        }
                    }
                }
            }
            else
            {
                orderHeaderList = await _db.OrderHeader
                    .Include(o => o.ApplicationUser)
                    .Where(u => u.Status == SD.StatusReady).ToListAsync();
            }

            foreach (OrderHeader item in orderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual);
            }

            //TODO Fix paging issue below
            var count = orderListVM.Orders.Count;
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                //.Skip((productPage - 1) * PageSize)
                //.Take(PageSize).ToList();
                .ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItems = count,
                urlParam = param.ToString()
            };

            return View(orderListVM);
        }


    }
}