using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    // Heredar de IdentityUser<int> para usar int como tipo de clave
    public class User : IdentityUser<int>
    {
        // Id, Email, PasswordHash, UserName, etc., son heredados de IdentityUser

        [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
        [Display(Name = "Nombres")]
        [StringLength(100, ErrorMessage = "El campo Nombres no puede exceder los 100 caracteres.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
        [Display(Name = "Apellidos")]
        [StringLength(100, ErrorMessage = "El campo Apellidos no puede exceder los 100 caracteres.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "El campo Fecha de Nacimiento es obligatorio.")]
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        
        // Propiedades personalizadas pueden ser añadidas aquí
        [Display(Name = "Es Administrador")]
        public bool IsAdmin { get; set; } = false;
    }
} 