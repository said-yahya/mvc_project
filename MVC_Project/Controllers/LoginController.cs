using Microsoft.AspNetCore.Mvc;
using MVC_Project.Data;
using MVC_Project.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
        public async Task<IActionResult> Login(LoginViewModel model)
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
            else if (user.Password != model.Password)
            {
                return BadRequest("Invalid password");
            }

            // Create claims for the signed-in user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Surname, user.Lastname),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            Console.WriteLine($"Login success: {user.Email}");
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

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Login");
        }
    }
}