using African_Beauty_Trading.Models; // <-- Make sure this namespace matches your project
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;
using System;
using System.Security.Claims;

[assembly: OwinStartupAttribute(typeof(African_Beauty_Trading.Startup))]
namespace African_Beauty_Trading
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            CreateRolesAndUsers(); // Call role creation
        }

        // Method to create default roles and admin user
        private void CreateRolesAndUsers()
        {
            ApplicationDbContext context = new ApplicationDbContext();

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // Create Admin role and default Admin user
            if (!roleManager.RoleExists("Admin"))
            {
                var role = new IdentityRole("Admin");
                roleManager.Create(role);

                var user = new ApplicationUser
                {
                    UserName = "admin@system.com",
                    Email = "admin@system.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    PhoneNumber = "0123456789",
                    EmailConfirmed = true,
                    DateCreated = DateTime.Now,
                    IsActive = true
                };

                string userPWD = "Admin@123";
                var chkUser = userManager.Create(user, userPWD);

                if (chkUser.Succeeded)
                {
                    userManager.AddToRole(user.Id, "Admin");
                }
            }

            // Create Customer role
            if (!roleManager.RoleExists("Customer"))
            {
                var role = new IdentityRole("Customer");
                roleManager.Create(role);

                // Seed 3 customers with complete information
                var customers = new[]
                {
            new
            {
                Email = "customer1@example.com",
                Password = "Customer@123",
                FirstName = "John",
                LastName = "Customer",
                PhoneNumber = "0831111111"
            },
            new
            {
                Email = "customer2@example.com",
                Password = "Customer@123",
                FirstName = "Jane",
                LastName = "Customer",
                PhoneNumber = "0832222222"
            },
            new
            {
                Email = "customer3@example.com",
                Password = "Customer@123",
                FirstName = "Mike",
                LastName = "Customer",
                PhoneNumber = "0833333333"
            }
        };

                foreach (var c in customers)
                {
                    var user = userManager.FindByEmail(c.Email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = c.Email,
                            Email = c.Email,
                            FirstName = c.FirstName,
                            LastName = c.LastName,
                            PhoneNumber = c.PhoneNumber,
                            EmailConfirmed = true,
                            DateCreated = DateTime.Now,
                            IsActive = true
                        };

                        var result = userManager.Create(user, c.Password);
                        if (result.Succeeded)
                        {
                            userManager.AddToRole(user.Id, "Customer");
                        }
                    }
                }
            }

            // Create Driver role
            if (!roleManager.RoleExists("Driver"))
            {
                var role = new IdentityRole("Driver");
                roleManager.Create(role);

                // Seed 3 drivers with complete information
                var drivers = new[]
                {
            new
            {
                Email = "driver1@absa.com",
                Password = "Driver@123",
                FirstName = "Thabo",
                LastName = "Mbeki",
                PhoneNumber = "0821234567"
            },
            new
            {
                Email = "driver2@absa.com",
                Password = "Driver@123",
                FirstName = "Nomsa",
                LastName = "Zulu",
                PhoneNumber = "0839876543"
            },
            new
            {
                Email = "driver3@absa.com",
                Password = "Driver@123",
                FirstName = "Sipho",
                LastName = "Khumalo",
                PhoneNumber = "0715551234"
            }
        };

                foreach (var d in drivers)
                {
                    var user = userManager.FindByEmail(d.Email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = d.Email,
                            Email = d.Email,
                            FirstName = d.FirstName,
                            LastName = d.LastName,
                            PhoneNumber = d.PhoneNumber,
                            EmailConfirmed = true,
                            DateCreated = DateTime.Now,
                            IsActive = true
                        };

                        var result = userManager.Create(user, d.Password);
                        if (result.Succeeded)
                        {
                            userManager.AddToRole(user.Id, "Driver");

                            // Optional: Add claims for easier access to driver information
                            userManager.AddClaim(user.Id, new Claim("FullName", $"{d.FirstName} {d.LastName}"));
                            userManager.AddClaim(user.Id, new Claim("PhoneNumber", d.PhoneNumber));
                        }
                    }
                    else
                    {
                        // Update existing driver with missing fields if needed
                        var needsUpdate = false;
                        if (string.IsNullOrEmpty(user.FirstName))
                        {
                            user.FirstName = d.FirstName;
                            needsUpdate = true;
                        }
                        if (string.IsNullOrEmpty(user.LastName))
                        {
                            user.LastName = d.LastName;
                            needsUpdate = true;
                        }
                        if (string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            user.PhoneNumber = d.PhoneNumber;
                            needsUpdate = true;
                        }

                        if (needsUpdate)
                        {
                            userManager.Update(user);
                        }
                    }
                }
            }
        }
    }
}
