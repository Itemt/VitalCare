using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitasEPS.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombres")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Apellidos")]
        public string LastName { get; set; } = string.Empty;

        [NotMapped] // This property is not stored in the database
        [Display(Name = "Nombre Completo")]
        public string FullName => $"{FirstName} {LastName}";

        [Required]
        [StringLength(50)]
        [Display(Name = "Especialidad")]
        public string Specialty { get; set; } = string.Empty; // e.g., Medicina General, Pediatría

        [StringLength(20)]
        [Display(Name = "Número de Registro Médico")]
        public string? MedicalLicenseNumber { get; set; }

        [Phone]
        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }
    }
} 