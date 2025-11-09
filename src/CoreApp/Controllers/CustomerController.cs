#if NET7_0
using Microsoft.AspNetCore.Mvc;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Browse(int? departmentId)
        {
            return View("~/Views/Customer/Browse.cshtml");
        }

        public IActionResult Dashboard()
        {
            return View("~/Views/Customer/Dashboard.cshtml");
        }

        public IActionResult Details(int id)
        {
            return View("~/Views/Customer/Details.cshtml");
        }

        public IActionResult Track()
        {
            return View("~/Views/Customer/Track.cshtml");
        }
    }
}
#endif