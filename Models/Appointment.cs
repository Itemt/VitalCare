using System;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Fecha y Hora")]
        public DateTime AppointmentDateTime { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsConfirmed { get; set; } = false;
    }
} 