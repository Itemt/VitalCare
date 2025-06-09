using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CitasEPS.Models.Modules.Common;
using CitasEPS.Models.Modules.Appointments;

namespace CitasEPS.Models.Modules.Users
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(100)]
        [Display(Name = "Nombres")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido.")]
        [StringLength(100)]
        [Display(Name = "Apellidos")]
        public string LastName { get; set; } = string.Empty;

        [NotMapped] // Esta propiedad no se almacena en la base de datos
        [Display(Name = "Nombre Completo")]
        public string FullName => $"{FirstName} {LastName}";

        [StringLength(20)]
        [Display(Name = "Documento de Identidad")]
        public string? DocumentId { get; set; }

        [Required(ErrorMessage = "El género es requerido.")]
        [Display(Name = "Género")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es requerida.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        [MinimumAge(18, ErrorMessage = "El paciente debe ser mayor de edad (18 años).")]
        public DateTime DateOfBirth { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        // Clave foránea para el User de Identity
        [Required]
        public int UserId { get; set; } 
        public virtual User User { get; set; } = null!;

        // Colección de citas asociadas
        public ICollection<Appointment>? Appointments { get; set; }
    }
} 



