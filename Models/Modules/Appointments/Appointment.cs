using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;

namespace CitasEPS.Models.Modules.Appointments
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

        // Navigation property for rating related to this appointment
        public virtual Rating? Rating { get; set; }

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

        [Display(Name = "Nueva Fecha Propuesta")]
        public DateTime? ProposedNewDateTime { get; set; } // Para propuesta de reagendamiento del paciente

        [Display(Name = "No Se Presentó")]
        public bool WasNoShow { get; set; } = false; // Marcar si el paciente no asistió

        [Display(Name = "Cancelada")]
        public bool IsCancelled { get; set; } = false; // Nueva propiedad para cancelación

        [Display(Name = "Reagendamiento Propuesto por Doctor")]
        public bool DoctorProposedReschedule { get; set; } = false; // New flag for doctor-initiated reschedule
        
        [Display(Name = "Motivo del Reagendamiento (Doctor)")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder los 500 caracteres.")]
        public string? DoctorRescheduleReason { get; set; } // Reason from doctor for their proposal
    }
} 



