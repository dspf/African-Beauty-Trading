#if NET7_0
using Microsoft.AspNetCore.Mvc;

namespace African_Beauty_Trading.CoreApp.Controllers
{
    public class AdminChatController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/AdminChat/Index.cshtml");
        }

        public IActionResult ChatRoom(int id)
        {
            return View("~/Views/AdminChat/ChatRoom.cshtml");
        }
    }
}
#endif