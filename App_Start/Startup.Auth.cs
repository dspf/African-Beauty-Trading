using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using African_Beauty_Trading.Models;
using System.Web;
using Microsoft.Owin.Host.SystemWeb;

namespace African_Beauty_Trading
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
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
                CookieManager = new SystemWebCookieManager(),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager)),
                    OnApplyRedirect = context =>
                    {
                        var loginPath = new PathString("/Account/Login");
                        // Avoid redirecting when already on the login path
                        if (context.Request.Path.StartsWithSegments(loginPath))
                        {
                            return;
                        }

                        try
                        {
                            var uri = new Uri(context.RedirectUri);
                            var query = HttpUtility.ParseQueryString(uri.Query);
                            var returnUrl = query["ReturnUrl"];

                            if (!string.IsNullOrEmpty(returnUrl))
                            {
                                // Only allow local, non-login return URLs
                                bool isLocal = returnUrl.StartsWith("/");
                                bool isLogin = returnUrl.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase);
                                if (!isLocal || isLogin)
                                {
                                    // Strip invalid ReturnUrl
                                    query.Remove("ReturnUrl");
                                    var builder = new UriBuilder(uri) { Query = query.ToString() };
                                    context.RedirectUri = builder.Uri.ToString();
                                }
                            }
                        }
                        catch
                        {
                            // Fallback to plain login path on parse failures
                            context.RedirectUri = "/Account/Login";
                        }

                        context.Response.Redirect(context.RedirectUri);
                    }
                }
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
        }
    }
}