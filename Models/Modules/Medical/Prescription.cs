using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CitasEPS.Models.Modules.Appointments;
using CitasEPS.Models.Modules.Users;

namespace CitasEPS.Models.Modules.Medical
{
    public class Prescription
    {
        public int Id { get; set; }

        [Display(Name = "Cita (Opcional)")]
        public int? AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }

        [Required]
        [Display(Name = "Medicamento")]
        public int MedicationId { get; set; }

        [ForeignKey("MedicationId")]
        public Medication? Medication { get; set; }

        [Required(ErrorMessage = "La dosis es requerida.")]
        [StringLength(100)]
        [Display(Name = "Dosis")]
        public string Dosage { get; set; } = default!;

        [Required(ErrorMessage = "Las instrucciones son requeridas.")]
        [StringLength(500)]
        [Display(Name = "Instrucciones")]
        public string Instructions { get; set; } = default!;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Prescripción")]
        public DateTime PrescriptionDate { get; set; }

        // Optional: Add DoctorId and PatientId for easier querying, though they can be derived via Appointment
        [Required]
        [Display(Name = "Médico")]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; }

        [Required]
        [Display(Name = "Paciente")]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }
    }
} 



