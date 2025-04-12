using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models
{
    public class Specialty
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la especialidad es requerido.")]
        [StringLength(100)]
        [Display(Name = "Nombre Especialidad")]
        public string Name { get; set; } = string.Empty;

        // Propiedad de navegación: Colección de Médicos que tienen esta Especialidad
        public ICollection<Doctor>? Doctors { get; set; }
    }
} 