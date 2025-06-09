using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models.Modules.Medical
{
    public class Medication
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        // Navigation property for prescriptions
        public ICollection<Prescription>? Prescriptions { get; set; }
    }
} 



