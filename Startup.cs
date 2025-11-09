using African_Beauty_Trading.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Diagnostics;

[assembly: OwinStartup(typeof(African_Beauty_Trading.Startup))]
namespace African_Beauty_Trading
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Authentication FIRST
            ConfigureAuth(app);
            app.MapSignalR();

            // Then create roles and users
            CreateRolesAndUsers();
        }

        public void ConfigureAuthLegacy(IAppBuilder app)
        {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                },
                LogoutPath = new PathString("/Account/Logout"),
                ExpireTimeSpan = TimeSpan.FromDays(30),
                SlidingExpiration = true
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
        }

        // Method to create default roles and admin user
        private void CreateRolesAndUsers()
        {
            try
            {
                ApplicationDbContext context = new ApplicationDbContext();

                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

                // Use the SAME password validator that works for customers
                userManager.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 6,
                    RequireNonLetterOrDigit = true,  // Match customer requirement
                    RequireDigit = true,             // Match customer requirement  
                    RequireLowercase = true,         // Match customer requirement
                    RequireUppercase = true,         // Match customer requirement
                };

                Debug.WriteLine("=== STARTING USER SEEDING ===");

                // 1. FIRST Create Admin Role
                if (!roleManager.RoleExists("Admin"))
                {
                    Debug.WriteLine("Creating Admin role...");
                    var roleResult = roleManager.Create(new IdentityRole("Admin"));
                    if (roleResult.Succeeded)
                        Debug.WriteLine("✓ Admin role created successfully");
                    else
                        Debug.WriteLine($"✗ Failed to create Admin role: {string.Join(", ", roleResult.Errors)}");
                }
                else
                {
                    Debug.WriteLine("Admin role already exists");
                }

                // 2. THEN Create Customer Role  
                if (!roleManager.RoleExists("Customer"))
                {
                    Debug.WriteLine("Creating Customer role...");
                    var roleResult = roleManager.Create(new IdentityRole("Customer"));
                    if (roleResult.Succeeded)
                        Debug.WriteLine("✓ Customer role created successfully");
                    else
                        Debug.WriteLine($"✗ Failed to create Customer role: {string.Join(", ", roleResult.Errors)}");
                }
                else
                {
                    Debug.WriteLine("Customer role already exists");
                }

                // 3. CREATE ADMIN USER - Using EXACT same pattern as customers
                var adminUser = userManager.FindByEmail("admin@eleganzeblend.com");
                if (adminUser == null)
                {
                    Debug.WriteLine("Creating admin user...");

                    // Use EXACT same structure as working customers
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin@eleganzeblend.com",    // Same as email like customers
                        Email = "admin@eleganzeblend.com",       // Professional email
                        FirstName = "System",                    // Required field
                        LastName = "Administrator",              // Required field  
                        PhoneNumber = "0123456789",              // Required field
                        EmailConfirmed = true,                   // Same as customers
                        DateCreated = DateTime.Now,              // Same as customers
                        IsActive = true                          // Same as customers
                    };

                    // Use EXACT same password pattern as working customers
                    string adminPassword = "Admin@123"; // Meets all requirements: 8+ chars, digit, upper, lower, special char

                    var adminResult = userManager.Create(adminUser, adminPassword);

                    if (adminResult.Succeeded)
                    {
                        Debug.WriteLine("✓ Admin user created successfully");

                        // Add to Admin role
                        var roleResult = userManager.AddToRole(adminUser.Id, "Admin");
                        if (roleResult.Succeeded)
                            Debug.WriteLine("✓ Admin user added to Admin role");
                        else
                            Debug.WriteLine($"✗ Failed to add admin to role: {string.Join(", ", roleResult.Errors)}");
                    }
                    else
                    {
                        Debug.WriteLine($"✗ Failed to create admin user: {string.Join(", ", adminResult.Errors)}");

                        // Try alternative if first attempt fails
                        adminUser = new ApplicationUser
                        {
                            UserName = "admin",
                            Email = "admin@eleganzeblend.com",
                            FirstName = "System",
                            LastName = "Administrator",
                            PhoneNumber = "0123456789",
                            EmailConfirmed = true,
                            DateCreated = DateTime.Now,
                            IsActive = true
                        };

                        adminResult = userManager.Create(adminUser, "Admin@123");
                        if (adminResult.Succeeded)
                        {
                            Debug.WriteLine("✓ Admin user created with alternative username");
                            userManager.AddToRole(adminUser.Id, "Admin");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Admin user already exists");

                    // Ensure existing admin is in Admin role
                    if (!userManager.IsInRole(adminUser.Id, "Admin"))
                    {
                        Debug.WriteLine("Adding existing admin user to Admin role...");
                        userManager.AddToRole(adminUser.Id, "Admin");
                    }
                }

                // 4. CREATE CUSTOMER USERS (Your working code)
                Debug.WriteLine("Creating customer users...");

                var customers = new[]
                {
                    new
                    {
                        Email = "customer1@example.com",
                        Password = "Customer@123", // Same pattern as admin password
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

                foreach (var customer in customers)
                {
                    var existingUser = userManager.FindByEmail(customer.Email);
                    if (existingUser == null)
                    {
                        var newUser = new ApplicationUser
                        {
                            UserName = customer.Email,
                            Email = customer.Email,
                            FirstName = customer.FirstName,
                            LastName = customer.LastName,
                            PhoneNumber = customer.PhoneNumber,
                            EmailConfirmed = true,
                            DateCreated = DateTime.Now,
                            IsActive = true
                        };

                        var result = userManager.Create(newUser, customer.Password);
                        if (result.Succeeded)
                        {
                            userManager.AddToRole(newUser.Id, "Customer");
                            Debug.WriteLine($"✓ Customer {customer.Email} created successfully");
                        }
                        else
                        {
                            Debug.WriteLine($"✗ Failed to create customer {customer.Email}: {string.Join(", ", result.Errors)}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Customer {customer.Email} already exists");
                    }
                }

                Debug.WriteLine("=== USER SEEDING COMPLETED ===");

                // Final verification
                Debug.WriteLine("=== FINAL VERIFICATION ===");
                var finalAdmin = userManager.FindByEmail("admin@eleganzeblend.com");
                if (finalAdmin != null)
                {
                    Debug.WriteLine($"Admin user found: {finalAdmin.Email}");
                    var isInRole = userManager.IsInRole(finalAdmin.Id, "Admin");
                    Debug.WriteLine($"Admin user in Admin role: {isInRole}");
                }
                else
                {
                    Debug.WriteLine("Admin user NOT found after seeding!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! ERROR in CreateRolesAndUsers: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}