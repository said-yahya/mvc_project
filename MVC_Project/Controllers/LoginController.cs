using Microsoft.AspNetCore.Mvc;

namespace MVC_Project.Controllers
{
    public class LoginController : Controller
    {
        // GET: /Login/
        public IActionResult Index()
        {
            return View();
        }
    }
}