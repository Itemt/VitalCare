using System.ComponentModel.DataAnnotations;

namespace CitasEPS.Models.Modules.Common
{
    /// <summary>
    /// Atributo de validación para verificar la edad mínima basada en la fecha de nacimiento
    /// </summary>
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        /// <summary>
        /// Constructor que define la edad mínima requerida
        /// </summary>
        /// <param name="minimumAge">Edad mínima requerida en años</param>
        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            ErrorMessage = $"Debe ser mayor de {minimumAge} años para registrarse.";
        }

        /// <summary>
        /// Valida que la fecha de nacimiento corresponda a una edad mayor o igual a la mínima requerida
        /// </summary>
        /// <param name="value">Fecha de nacimiento</param>
        /// <param name="validationContext">Contexto de validación</param>
        /// <returns>Resultado de la validación</returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult("La fecha de nacimiento es obligatoria.");
            }

            if (value is DateTime dateOfBirth)
            {
                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;

                // Ajustar si todavía no ha llegado el cumpleaños este año
                if (dateOfBirth.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age < _minimumAge)
                {
                    return new ValidationResult(ErrorMessage ?? $"Debe tener al menos {_minimumAge} años. Edad actual: {age} años.");
                }
            }
            else
            {
                return new ValidationResult("Formato de fecha inválido.");
            }

            return ValidationResult.Success;
        }
    }
} 