using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitasEPS.Models
{
    public class Doctor
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

        [Required(ErrorMessage = "La especialidad es requerida.")]
        [Display(Name = "Especialidad")]
        public int SpecialtyId { get; set; } // Llave foránea para Specialty
        public Specialty? Specialty { get; set; } // Propiedad de navegación

        [StringLength(20)]
        [Display(Name = "Número de Registro Médico")]
        public string? MedicalLicenseNumber { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        // --- Link to User Identity ---
        public int? UserId { get; set; } // Foreign key to the User table (AspNetUsers)

        [ForeignKey("UserId")]
        public virtual User? User { get; set; } // Navigation property
        // ---------------------------

        // Colección de citas asociadas
        public ICollection<Appointment>? Appointments { get; set; }
    }
} 