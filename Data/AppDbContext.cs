using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringConfigurator.Models;

namespace MonitoringConfigurator.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // USUNIÊTO: Products, Orders, OrderDetails
        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<UserDocument> UserDocuments { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // USUNIÊTO: builder.Entity<Product>().Property(p => p.Price)...
        }
    }
}