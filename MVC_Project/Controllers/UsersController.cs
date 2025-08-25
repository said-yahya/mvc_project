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
            _context = context;
        }

        public IActionResult AllUsers()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // GET: /Users/Edit - Show all users with edit options
        public IActionResult Edit()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // GET: /Users/Edit/{id} - Show edit form for specific user
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: /Users/Edit/{id} - Save changes for specific user
        [HttpPost]
        public IActionResult EditUser(int id, User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Edit));
                }
                catch
                {
                    // Handle error
                    return View(user);
                }
            }
            return View(user);
        }

        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(User user, string ConfirmPassword)
        {
            if (ModelState.IsValid)
            {
                if (user.Password != ConfirmPassword)
                {
                    ModelState.AddModelError("", "Password do not match");
                    return View(user);
                }
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existingUser != null)
            {
                Console.WriteLine($"User with email {user.Email} already exists");
                return RedirectToAction("AddUser", "Users");
            }

            try
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Error saving user");
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
            {
                return NotFound();
            }
            try
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(DeleteUser));
            }
            catch
            {
                // Handle error
                return View("Error");
            }
        }
    }
}