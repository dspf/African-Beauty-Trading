using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace African_Beauty_Trading.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        // Additional properties you might want
        public DateTime DateCreated { get; set; }
        public bool IsActive { get; set; }
        public string Address { get; set; }
        // Add these properties for customer profile
       
        public string City { get; set; }
        public string PostalCode { get; set; }

     

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }

        public ApplicationUser()
        {
            DateCreated = DateTime.Now;
            IsActive = true;
        }
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<ApplicationDbContext>());
        }

        public ApplicationDbContext(string connectionString)
        : base(connectionString, throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        // Existing DbSets
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }

        // New Chat DbSets
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Properties<DateTime>()
                .Configure(c => c.HasColumnType("datetime2"));

            modelBuilder.Properties<DateTime?>()
                .Configure(c => c.HasColumnType("datetime2"));

            // ChatRoom configurations
            modelBuilder.Entity<ChatRoom>()
                .HasKey(cr => cr.Id);

            modelBuilder.Entity<ChatRoom>()
                .HasOptional(cr => cr.Admin)
                .WithMany()
                .HasForeignKey(cr => cr.AdminId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ChatRoom>()
                .HasRequired(cr => cr.Customer)
                .WithMany()
                .HasForeignKey(cr => cr.CustomerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ChatRoom>()
                .HasMany(cr => cr.Messages)
                .WithRequired(m => m.ChatRoom)
                .HasForeignKey(m => m.ChatRoomId)
                .WillCascadeOnDelete(true);

            // ChatMessage configurations
            modelBuilder.Entity<ChatMessage>()
                .HasKey(cm => cm.Id);

            modelBuilder.Entity<ChatMessage>()
                .HasRequired(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ChatMessage>()
                .Property(cm => cm.Message)
                .IsRequired()
                .HasMaxLength(1000);

            modelBuilder.Entity<ChatMessage>()
                .Property(cm => cm.SenderName)
                .IsRequired()
                .HasMaxLength(256);

            // REMOVE ALL INDEX CONFIGURATIONS - They are causing the error
            // Entity Framework will create indexes automatically for foreign keys

            // Optional: Configure decimal precision for existing entities if needed
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasPrecision(18, 2);
        }
    }
}