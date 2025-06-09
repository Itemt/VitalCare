using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using CitasEPS.Models.Modules.Common; // Required for Gender enum

namespace CitasEPS.Models.Modules.Users
{
    // Heredar de IdentityUser<int> para usar int como tipo de clave
    public class User : IdentityUser<int>
    {
        // Id, Email, PasswordHash, UserName, etc., son heredados de IdentityUser

        [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
        [Display(Name = "Nombres")]
        [StringLength(100, ErrorMessage = "El campo Nombres no puede exceder los 100 caracteres.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
        [Display(Name = "Apellidos")]
        [StringLength(100, ErrorMessage = "El campo Apellidos no puede exceder los 100 caracteres.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo Fecha de Nacimiento es obligatorio.")]
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Documento de Identidad")]
        [StringLength(20, ErrorMessage = "El Documento de Identidad no puede exceder los 20 caracteres.")]
        public string? DocumentId { get; set; } // Assuming it can be optional or set later

        [Display(Name = "Género")]
        public Gender? Gender { get; set; } // Assuming it can be optional
        
        // Propiedades personalizadas pueden ser añadidas aquí
        [Display(Name = "Es Administrador")]
        public bool IsAdmin { get; set; } = false;

        // Propiedad de navegación al Paciente asociado
        public virtual Patient? Patient { get; set; }
    }
} 



