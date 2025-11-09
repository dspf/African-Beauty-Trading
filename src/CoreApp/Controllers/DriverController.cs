#if NET7_0
using Microsoft.AspNetCore.Mvc;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class DriverController : Controller
    {
        public IActionResult Dashboard()
        {
            return View("~/Views/Driver/Dashboard.cshtml");
        }

        public IActionResult Schedule()
        {
            return View("~/Views/Driver/Schedule.cshtml");
        }

        public IActionResult Earnings()
        {
            return View("~/Views/Driver/Earnings.cshtml");
        }
    }
}
#endif