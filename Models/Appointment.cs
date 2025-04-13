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
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required(ErrorMessage = "El médico es requerido.")]
        [Display(Name = "Médico")]
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [StringLength(500, ErrorMessage = "Las notas no pueden exceder los 500 caracteres.")]
        [Display(Name = "Notas / Motivo")]
        public string? Notes { get; set; }

        // Navigation property for prescriptions related to this appointment
        public virtual ICollection<Prescription>? Prescriptions { get; set; }

        // Doctor's clinical notes for the appointment
        [DataType(DataType.MultilineText)]
        [Display(Name = "Notas Clínicas")]
        public string? ClinicalNotes { get; set; } // Nullable string

        [Display(Name = "Confirmada")]
        public bool IsConfirmed { get; set; } = false; // Default a no confirmada

        [Display(Name = "Completada")]
        public bool IsCompleted { get; set; } = false; // Default a no completada

        [Display(Name = "Reagendamiento Solicitado")]
        public bool RescheduleRequested { get; set; } = false; // Nueva propiedad
    }
} 