#if NET7_0
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace African_Beauty_Trading.CoreApp.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Implement a real email sender or integrate with existing EmailService
            return Task.CompletedTask;
        }
    }
}
#endif