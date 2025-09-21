using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace African_Beauty_Trading.Services
{
    public class OtpService
    {
        public string GenerateOtp()
        {
            // Generate 6-digit OTP
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public bool IsOtpValid(string storedOtp, string enteredOtp, DateTime generatedAt)
        {
            // Check if OTP is not expired (24 hours expiry)
            if (DateTime.Now > generatedAt.AddHours(24))
                return false;

            // Check if OTP matches
            return storedOtp == enteredOtp;
        }
    }
}