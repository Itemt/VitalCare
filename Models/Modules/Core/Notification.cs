using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Appointments;

namespace CitasEPS.Models.Modules.Core
{
    public enum NotificationType
    {
        NewAppointment,
        AppointmentConfirmed,
        AppointmentModified,
        AppointmentCancelled,
        AppointmentRescheduled, // Doctor proposes, patient needs to accept/reject
        RescheduleRequestedByPatient, // Patient requests, doctor needs to review
        RescheduleRequestSent, // Confirmation to patient that their request was sent
        RescheduleAcceptedByPatient, // Patient accepts doctor's proposal
        RescheduleRejectedByPatient, // Patient rejects doctor's proposal
        RescheduleAcceptedByDoctor, // If a doctor accepts a patient's proposal (if that flow exists)
        RescheduleRejectedByDoctor, // Doctor rejects patient's proposal
        RescheduleProposedByDoctor, // Doctor proposes a new time
        RescheduleProposalSent, // Confirmation to doctor that their proposal was sent
        AppointmentReminder,
        PaymentProcessed
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign key to User
        [ForeignKey("UserId")]
        public User? User { get; set; } // Navigation property

        public int? AppointmentId { get; set; } // Optional: Link to a specific appointment
        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }

        [Required]
        [StringLength(500)]
        public string? Message { get; set; } // Notification content - Made Nullable
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Type of notification (e.g., "NewAppointment", "Cancellation", "Reminder")
        public NotificationType NotificationType { get; set; } = NotificationType.NewAppointment;
    }
} 



