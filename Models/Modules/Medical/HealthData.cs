using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CitasEPS.Models.Modules.Users;

namespace CitasEPS.Models.Modules.Medical
{
    public class HealthData
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Paciente")]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        [Display(Name = "Peso (kg)")]
        [Range(1, 300, ErrorMessage = "El peso debe estar entre 1 y 300 kg")]
        public decimal Weight { get; set; }

        [Required]
        [Display(Name = "Altura (m)")]
        [Range(0.5, 2.5, ErrorMessage = "La altura debe estar entre 0.5 y 2.5 metros")]
        public decimal Height { get; set; }

        [Required]
        [Display(Name = "Presión Sistólica (mmHg)")]
        [Range(50, 300, ErrorMessage = "La presión sistólica debe estar entre 50 y 300 mmHg")]
        public int? SystolicPressure { get; set; }

        [Display(Name = "Presión Diastólica (mmHg)")]
        [Range(30, 200, ErrorMessage = "La presión diastólica debe estar entre 30 y 200 mmHg")]
        public int? DiastolicPressure { get; set; }

        [Display(Name = "Frecuencia Cardíaca (bpm)")]
        [Range(40, 200, ErrorMessage = "La frecuencia cardíaca debe estar entre 40 y 200 bpm")]
        public int? HeartRate { get; set; }

        [Display(Name = "Nivel de Actividad")]
        [StringLength(50)]
        public string? ActivityLevel { get; set; }

        [Required]
        [Display(Name = "Fecha de Registro")]
        public DateTime RecordedDate { get; set; }

        [Display(Name = "IMC Calculado")]
        public decimal BMI => Math.Round(Weight / (Height * Height), 2);

        [Display(Name = "Estado de Salud")]
        [StringLength(50)]
        public string? HealthStatus { get; set; }

        [Display(Name = "Presión Arterial")]
        public string BloodPressure => SystolicPressure.HasValue && DiastolicPressure.HasValue 
            ? $"{SystolicPressure}/{DiastolicPressure}" 
            : "No registrada";
    }
} 