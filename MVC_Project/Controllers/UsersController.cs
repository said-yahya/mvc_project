using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_Project.Data;
using MVC_Project.Models;

namespace MVC_Project.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AllUsers()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();
            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(int id, User user)
        {
            if (id != user.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(user);

            try
            {
                _context.Update(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Edit));
            }
            catch (Exception ex)
            {
                // Log ex
                ModelState.AddModelError("", "Error updating user.");
                return View(user);
            }
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(User user, string ConfirmPassword)
        {
            if (!ModelState.IsValid)
                return View(user);

            if (user.Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(user);
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(user);
            }

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(AllUsers));
            }
            catch (Exception ex)
            {
                // Log ex
                ModelState.AddModelError("", "Error saving user.");
                return View(user);
            }
        }

        [HttpGet]
        public IActionResult DeleteUser()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            try
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(DeleteUser));
            }
            catch (Exception ex)
            {
                // Log ex
                return View("Error");
            }
        }
    }
}