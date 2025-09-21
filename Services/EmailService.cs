using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Configuration;

namespace African_Beauty_Trading.Services
{
    public class EmailService
    {
        public async Task<bool> SendAssignmentEmail(string toEmail, string customerName, string orderId, string driverName, DateTime estimatedDelivery)
        {
            try
            {
                var fromEmail = ConfigurationManager.AppSettings["EmailFrom"];
                var fromName = ConfigurationManager.AppSettings["EmailName"];

                var subject = "🚚 Your Order Has Been Assigned a Driver!";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #8B4513; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ background: #ddd; padding: 15px; text-align: center; font-size: 12px; border-radius: 5px; margin-top: 20px; }}
        .info-box {{ background: #fff; padding: 15px; border-left: 4px solid #8B4513; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌍 African Beauty Trading</h1>
        </div>
        <div class='content'>
            <h2>Hello {customerName}!</h2>
            <p>Great news! Your order <strong>#{orderId}</strong> has been assigned to a driver and is on its way to you.</p>
            
            <div class='info-box'>
                <h3>🚗 Delivery Details:</h3>
                <p><strong>Driver:</strong> {driverName}</p>
                <p><strong>Estimated Delivery:</strong> {estimatedDelivery:dddd, dd MMMM yyyy 'at' hh:mm tt}</p>
                <p><strong>Order Status:</strong> Assigned to Driver</p>
            </div>
            
            <p>You will receive another notification when your driver is approaching your location.</p>
            
            <p>Thank you for choosing <strong>African Beauty Trading</strong>! 🌟</p>
        </div>
        <div class='footer'>
            <p>© 2024 African Beauty Trading. All rights reserved.</p>
            <p>Email: support@africanbeauty.com | Phone: +27 11 123 4567</p>
        </div>
    </div>
</body>
</html>";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromName);
                    message.To.Add(toEmail);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient())
                    {
                        // Outlook specific settings
                        smtpClient.EnableSsl = true;
                        smtpClient.UseDefaultCredentials = false;

                        await smtpClient.SendMailAsync(message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Trace.TraceError($"Email sending failed: {ex.Message}");
                return false;
            }
        }
    }
}