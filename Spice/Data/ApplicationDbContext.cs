using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Spice.Models;

namespace Spice.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Category { set; get; }
        public DbSet<SubCategory> SubCategory { set; get; }
        public DbSet<MenuItem> MenuItem { set; get; }
        public DbSet<Coupon> Coupon { set; get; }
        public DbSet<ApplicationUser> ApplicationUser { set; get; }
        public DbSet<ShoppingCart> ShoppingCart { set; get; }
    }
}
