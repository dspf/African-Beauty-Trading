using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace African_Beauty_Trading.Services
{
    public class PayFastService
    {
        private readonly string merchantId = "10033488";   // Sandbox ID
        private readonly string merchantKey = "3ybf0pzoq1e4n"; // Sandbox key
        private readonly string siteBaseUrl;

        public PayFastService(string siteBaseUrl)
        {
            this.siteBaseUrl = siteBaseUrl.TrimEnd('/');
        }

        public string GeneratePaymentUrl(string orderId, string itemName, decimal totalAmount) // ✅ Renamed parameter
        {
            // ✅ LOG the received value
            System.Diagnostics.Trace.TraceInformation($"PAYFAST SERVICE - Received Amount: {totalAmount}");

            var returnUrl = $"{siteBaseUrl}/Cart/PaymentSuccess?orderId={orderId}";
            var cancelUrl = $"{siteBaseUrl}/Cart/PaymentCancel?orderId={orderId}";
            var notifyUrl = $"{siteBaseUrl}/Cart/PaymentNotify";

            var query = HttpUtility.ParseQueryString(string.Empty);

            query["merchant_id"] = merchantId;
            query["merchant_key"] = merchantKey;
            query["return_url"] = returnUrl;
            query["cancel_url"] = cancelUrl;
            query["notify_url"] = notifyUrl;

            query["m_payment_id"] = orderId;
            query["amount"] = totalAmount.ToString("F2"); // ✅ Use the renamed parameter
            query["item_name"] = itemName;

            // ✅ Build the URL and LOG it
            string generatedUrl = "https://sandbox.payfast.co.za/eng/process?" + query.ToString();
            System.Diagnostics.Trace.TraceInformation($"PAYFAST SERVICE - Generated URL: {generatedUrl}");

            return generatedUrl;
        }
    }
}