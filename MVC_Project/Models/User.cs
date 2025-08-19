using System.ComponentModel.DataAnnotations;

namespace MVC_Project.Models
{
    public class User
    {
        public int Id { get; set; }
        
        public required string Name { get; set; }
        public required string Lastname { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; } // Changed to public for EF
    }
}