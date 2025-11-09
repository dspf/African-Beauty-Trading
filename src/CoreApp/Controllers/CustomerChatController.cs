#if NET7_0
using Microsoft.AspNetCore.Mvc;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class CustomerChatController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/CustomerChat/Index.cshtml");
        }
    }
}
#endif