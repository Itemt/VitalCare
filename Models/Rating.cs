using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitasEPS.Models
{
    public class Rating
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Cita")]
        public int AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment Appointment { get; set; } = null!;

        [Required]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas.")]
        [Display(Name = "Calificación del Paciente")]
        public int PatientRating { get; set; }

        [StringLength(500, ErrorMessage = "El comentario del paciente no puede exceder los 500 caracteres.")]
        [Display(Name = "Comentario del Paciente")]
        public string? PatientComment { get; set; }

        [Required]
        [Display(Name = "Fecha de Calificación del Paciente")]
        public DateTime PatientRatingDate { get; set; }

        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas.")]
        [Display(Name = "Calificación del Doctor")]
        public int? DoctorRating { get; set; }

        [StringLength(500, ErrorMessage = "El comentario del doctor no puede exceder los 500 caracteres.")]
        [Display(Name = "Comentario del Doctor")]
        public string? DoctorComment { get; set; }

        [Display(Name = "Fecha de Calificación del Doctor")]
        public DateTime? DoctorRatingDate { get; set; }

        [Display(Name = "Calificación Completada")]
        public bool IsCompleted => PatientRating > 0 && DoctorRating.HasValue && DoctorRating > 0;
    }
} 