#if NET7_0
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using African_Beauty_Trading.Models; // reuse existing model classes

namespace African_Beauty_Trading.CoreApp
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);

            // Ensure DateTime columns use datetime2
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(System.DateTime) || p.PropertyType == typeof(System.DateTime?));
                foreach (var prop in properties)
                {
                    builder.Entity(entityType.Name).Property(prop.Name).HasColumnType("datetime2");
                }
            }

            // Configure ChatRoom relationships to ApplicationUser without cascade delete to avoid multiple cascade paths
            builder.Entity<ChatRoom>(cr =>
            {
                cr.HasKey(c => c.Id);

                cr.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(c => c.AdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(c => c.CustomerId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                cr.HasMany(c => c.Messages)
                  .WithOne(m => m.ChatRoom)
                  .HasForeignKey(m => m.ChatRoomId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ChatMessage
            builder.Entity<ChatMessage>(cm =>
            {
                cm.HasKey(m => m.Id);

                cm.Property(m => m.Message).IsRequired().HasMaxLength(1000);
                cm.Property(m => m.SenderName).IsRequired().HasMaxLength(256);

                cm.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
#endif