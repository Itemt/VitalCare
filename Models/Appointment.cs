using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitasEPS.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La fecha y hora de la cita son requeridas.")]
        [Display(Name = "Fecha y Hora")]
        public DateTime AppointmentDateTime { get; set; }

        [Required(ErrorMessage = "El paciente es requerido.")]
        [Display(Name = "Paciente")]
        public int PatientId { get; set; }
        public virtual Patient? Patient { get; set; }

        [Required(ErrorMessage = "El médico es requerido.")]
        [Display(Name = "Médico")]
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder los 500 caracteres.")]
        [Display(Name = "Notas / Motivo")]
        public string? Notes { get; set; }

        [Display(Name = "Confirmada")]
        public bool IsConfirmed { get; set; } = false;

        // Navigation property for prescriptions related to this appointment
        public virtual ICollection<Prescription>? Prescriptions { get; set; }
    }
} 