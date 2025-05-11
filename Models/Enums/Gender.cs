using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models.Enums
{
    public enum Gender
    {
        [Display(Name = "Masculino")]
        Masculino,
        [Display(Name = "Femenino")]
        Femenino,
        [Display(Name = "Otro")]
        Otro,
        [Display(Name = "Prefiero no decirlo")]
        PrefieroNoDecirlo
    }
} 