using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CitasEPS.Models.Modules.Users;

namespace CitasEPS.Models.Modules.Medical
{
    public class Specialty
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        // Colección de doctores con esta especialidad
        public ICollection<Doctor>? Doctors { get; set; }
    }
} 



