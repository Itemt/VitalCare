using System;

namespace CitasEPS.Services
{
    public static class ColombiaTimeZoneService
    {
        private static readonly TimeZoneInfo ColombiaTimeZone;

        static ColombiaTimeZoneService()
        {
            try
            {
                // "SA Pacific Standard Time" is the ID for Colombia (Bogota, Lima, Quito)
                ColombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback for non-Windows environments if the ID is not found
                // Or for systems where the full time zone database is not available.
                // This is the previous implementation, kept as a fallback.
                ColombiaTimeZone = TimeZoneInfo.CreateCustomTimeZone("Colombia", TimeSpan.FromHours(-5), "Colombia Standard Time", "Colombia Standard Time");
            }
        }
        
        /// <summary>
        /// Convierte una fecha UTC a hora local de Colombia (UTC-5)
        /// </summary>
        /// <param name="utcDateTime">Fecha en UTC</param>
        /// <returns>Fecha en hora de Colombia</returns>
        public static DateTime ConvertUtcToColombia(DateTime utcDateTime)
        {
            // Si la fecha no es UTC, se asume que ya está en hora local y se devuelve tal cual para evitar errores.
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                return utcDateTime; // O lanzar una excepción, dependiendo del caso de uso.
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ColombiaTimeZone);
        }
        
        /// <summary>
        /// Convierte una fecha de hora local de Colombia a UTC
        /// </summary>
        /// <param name="colombiaDateTime">Fecha en hora de Colombia (se asume Unspecified o Local)</param>
        /// <returns>Fecha en UTC</returns>
        public static DateTime ConvertColombiaToUtc(DateTime colombiaDateTime)
        {
            if (colombiaDateTime.Kind == DateTimeKind.Utc)
            {
                return colombiaDateTime; // Ya es UTC, no se necesita conversión.
            }

            // Si es Unspecified, la tratamos como si fuera hora de Colombia.
            // Si es Local, se asume que el servidor está en Colombia. Para más robustez, se podría validar la zona horaria del servidor.
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(colombiaDateTime, DateTimeKind.Unspecified), ColombiaTimeZone);
        }
        
        /// <summary>
        /// Obtiene la hora actual de Colombia
        /// </summary>
        /// <returns>Hora actual en Colombia</returns>
        public static DateTime GetColombiaTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaTimeZone);
        }
        
        /// <summary>
        /// Formatea una fecha UTC para mostrar en hora de Colombia
        /// </summary>
        /// <param name="utcDateTime">Fecha en UTC</param>
        /// <param name="format">Formato de fecha (por defecto dd/MM/yyyy hh:mm tt)</param>
        /// <returns>Cadena formateada en hora de Colombia</returns>
        public static string FormatInColombia(DateTime utcDateTime, string format = "dd/MM/yyyy hh:mm tt")
        {
            var colombiaTime = ConvertUtcToColombia(utcDateTime);
            return colombiaTime.ToString(format);
        }
    }
} 