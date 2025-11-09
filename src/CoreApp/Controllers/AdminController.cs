#if NET7_0
using Microsoft.AspNetCore.Mvc;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View("~/Views/Admin/Dashboard.cshtml");
        }

        public IActionResult Orders()
        {
            return View("~/Views/Admin/Orders.cshtml");
        }

        public IActionResult Users()
        {
            return View("~/Views/Admin/Users.cshtml");
        }

        public IActionResult OrderDetails(int id)
        {
            return View("~/Views/Admin/OrderDetails.cshtml");
        }
    }
}
#endif