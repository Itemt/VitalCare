using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    public class Medication
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        // Navigation property for related prescriptions
        public ICollection<Prescription>? Prescriptions { get; set; }
    }
} 