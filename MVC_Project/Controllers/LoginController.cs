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
                // Field-specific validations
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    ModelState.AddModelError(nameof(model.Name), "The Name field is required.");
                }
                else if (model.Name.StartsWith(" "))
                {
                    ModelState.AddModelError(nameof(model.Name), "Name cannot start with a space.");
                }

                if (string.IsNullOrWhiteSpace(model.Lastname))
                {
                    ModelState.AddModelError(nameof(model.Lastname), "The Lastname field is required.");
                }
                else if (model.Lastname.StartsWith(" "))
                {
                    ModelState.AddModelError(nameof(model.Lastname), "Lastname cannot start with a space.");
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "The Email field is required.");
                }
                else
                {
                    // Basic email validation
                    if (!model.Email.Contains("@") || !model.Email.Contains("."))
                    {
                        ModelState.AddModelError(nameof(model.Email), "The Email field is not a valid e-mail address.");
                    }
                    else
                    {
                        var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                        if (existingUser is not null)
                        {
                            ModelState.AddModelError(nameof(model.Email), "User with this email already exists.");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.AddModelError(nameof(model.Password), "The Password field is required.");
                }
                else if (model.Password.StartsWith(" "))
                {
                    ModelState.AddModelError(nameof(model.Password), "Password cannot start with a space.");
                }
                else if (model.Password.Contains(" "))
                {
                    ModelState.AddModelError(nameof(model.Password), "Password cannot contain spaces.");
                }
                if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "The ConfirmPassword field is required.");
                }
                else if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
                }

                if (!ModelState.IsValid)
                {
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
                ModelState.AddModelError("", "An unexpected error occurred during signup.");
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