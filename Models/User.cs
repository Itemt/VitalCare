using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    // Inherit from IdentityUser<int> to use int as the key type
    public class User : IdentityUser<int>
    {
        // Id, Email, PasswordHash, UserName, etc., are inherited from IdentityUser

        // Custom properties can be added here
        public bool IsAdmin { get; set; } = false;
    }
} 