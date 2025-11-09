#if NET7_0
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Home/Index.cshtml");
        }

        public IActionResult About()
        {
            return View("~/Views/Home/About.cshtml");
        }

        public IActionResult Contact()
        {
            return View("~/Views/Home/Contact.cshtml");
        }
    }
}
#endif