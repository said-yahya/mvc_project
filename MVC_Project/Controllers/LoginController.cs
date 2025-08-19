using Microsoft.AspNetCore.Mvc;
using MVC_Project.Data;
using MVC_Project.Models;

namespace MVC_Project.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Login/
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (model.Email == null || model.Password == null)
            {
                return BadRequest("Email and password are required");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user == null)
            {
                return BadRequest("User not found");
            }
            else if ((user.Password != model.Password) || (user.Name != model.Name))
            {
                return BadRequest("Invalid password or name");
            }

            Console.WriteLine($"Login attempt: {model.Email}");
            return RedirectToAction("Index", "Home");
        }

        // POST: /Login/Signup - Sadece signup formu buraya gelir
        [HttpPost]
        public IActionResult Signup(RegisterViewModel model)
        {
            try
            {
                if (model.Name == null || model.Lastname == null || model.Email == null || model.Password == null)
                {
                    return BadRequest("All fields are required");
                }

                // Check if user with this email already exists
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    Console.WriteLine($"User with email {model.Email} already exists");
                    return RedirectToAction("Index", "Login");
                }

                // Create new user object
                var newUser = new User
                {
                    Name = model.Name,
                    Lastname = model.Lastname,
                    Email = model.Email,
                    Password = model.Password // Note: In production, you should hash this password
                };

                // Add user to database
                _context.Users.Add(newUser);
                _context.SaveChanges();

                Console.WriteLine($"Signup successful: {model.Name} {model.Lastname} saved to database");
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Signup error: {ex.Message}");
                return RedirectToAction("Index", "Login");
            }
        }
    }
}