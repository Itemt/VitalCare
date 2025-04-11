using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitasEPS.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Cita")]
        public int AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; }

        [Required]
        [Display(Name = "Medicamento")]
        public int MedicationId { get; set; }

        [ForeignKey("MedicationId")]
        public Medication Medication { get; set; }

        [Required(ErrorMessage = "La dosis es requerida.")]
        [StringLength(100)]
        [Display(Name = "Dosis")]
        public string Dosage { get; set; } // e.g., "10mg", "1 tablet"

        [Required(ErrorMessage = "Las instrucciones son requeridas.")]
        [StringLength(500)]
        [Display(Name = "Instrucciones")]
        public string Instructions { get; set; } // e.g., "Tomar una tableta cada 8 horas con comida."

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Prescripción")]
        public DateTime PrescriptionDate { get; set; }

        // Optional: Add DoctorId and PatientId for easier querying, though they can be derived via Appointment
        [Required]
        [Display(Name = "Médico")]
        public int DoctorId { get; set; } // Assuming the Doctor who prescribed it is the one assigned to the Appointment

        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; }

        [Required]
        [Display(Name = "Paciente")]
        public int PatientId { get; set; } // Assuming the Patient is the one associated with the Appointment

        [ForeignKey("PatientId")]
        public Patient Patient { get; set; }
    }
} 