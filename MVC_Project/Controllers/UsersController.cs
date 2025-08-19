using Microsoft.AspNetCore.Mvc;
using MVC_Project.Data;
using MVC_Project.Models;

namespace MVC_Project.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            context = _context;
        }
        
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }
    }
}