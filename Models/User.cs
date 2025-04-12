using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    // Heredar de IdentityUser<int> para usar int como tipo de clave
    public class User : IdentityUser<int>
    {
        // Id, Email, PasswordHash, UserName, etc., son heredados de IdentityUser

        // Propiedades personalizadas pueden ser añadidas aquí
        [Display(Name = "Es Administrador")]
        public bool IsAdmin { get; set; } = false;
    }
} 