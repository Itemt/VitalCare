using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    public class Specialty
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre Especialidad")]
        public string Name { get; set; } = string.Empty;

        // Navigation property: Collection of Doctors that have this Specialty
        public ICollection<Doctor>? Doctors { get; set; }
    }
} 