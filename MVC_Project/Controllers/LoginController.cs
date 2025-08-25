using Microsoft.AspNetCore.Mvc;
using MVC_Project.Data;
using MVC_Project.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace MVC_Project.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View("Index", model);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user is null)
            {
                ModelState.AddModelError("", "User not found");
                return View("Index", model);
            }
            else if (user.Password != model.Password)
            {
                ModelState.AddModelError("", "Invalid password");
                return View("Index", model);
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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Signup()
        {
            return View();
        }

        // POST: /Login/Signup - Sadece signup formu buraya gelir
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Signup(RegisterViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name) ||
                    string.IsNullOrWhiteSpace(model.Lastname) ||
                    string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.Password) ||
                    string.IsNullOrWhiteSpace(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "All fields are required");
                    return View("Signup", model);
                }

                if (model.Name.StartsWith(" ") || model.Lastname.StartsWith(" "))
                {
                    ModelState.AddModelError("", "Name and Lastname cannot start with a space.");
                    return View("Signup", model);
                }

                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    return View("Signup", model);
                }

                var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser is not null)
                {
                    ModelState.AddModelError("", "User with this email already exists");
                    return View("Signup", model);
                }

                var newUser = new User
                {
                    Name = model.Name,
                    Lastname = model.Lastname,
                    Email = model.Email,
                    Password = model.Password
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                Console.WriteLine($"Signup successful: {model.Name} {model.Lastname} saved to database");
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Signup error: {ex.Message}");
                ModelState.AddModelError("", "Error during signup");
                return View("Signup", model);
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