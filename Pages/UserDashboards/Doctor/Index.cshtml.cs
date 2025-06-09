using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CitasEPS.Data;
using CitasEPS.Models;
using CitasEPS.Models.Modules.Users;
using CitasEPS.Models.Modules.Medical;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class DoctorIndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DoctorIndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Estadísticas para mostrar en el dashboard
        public int TodayAppointments { get; set; }
        public int ConfirmedAppointments { get; set; }
        public int SatisfactionPercentage { get; set; }
        public int TotalPrescriptions { get; set; }
        public IList<Prescription> RecentPrescriptions { get; set; } = new List<Prescription>();

        public async Task OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var currentDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (currentDoctor != null)
                    {
                        // Obtener fecha actual en Bogotá de manera simple
                        var today = DateTime.Now.Date; // Usar fecha local del sistema
                        var tomorrow = today.AddDays(1);
                        
                        try
                        {
                            // Citas para hoy 
                            TodayAppointments = await _context.Appointments
                                .Where(a => a.DoctorId == currentDoctor.Id && !a.IsCancelled)
                                .CountAsync(a => a.AppointmentDateTime.Date == today);
                        }
                        catch
                        {
                            TodayAppointments = 0;
                        }

                        try
                        {
                            // Citas confirmadas (pendientes de completar)
                            ConfirmedAppointments = await _context.Appointments
                                .CountAsync(a => a.DoctorId == currentDoctor.Id && 
                                           a.IsConfirmed && 
                                           !a.IsCompleted && 
                                           !a.IsCancelled);
                        }
                        catch
                        {
                            ConfirmedAppointments = 0;
                        }

                        try
                        {
                            // Promedio de satisfacción - simplificado
                            var ratingsCount = await _context.Ratings
                                .Join(_context.Appointments,
                                      r => r.AppointmentId,
                                      a => a.Id,
                                      (r, a) => new { Rating = r, Appointment = a })
                                .Where(x => x.Appointment.DoctorId == currentDoctor.Id && 
                                           x.Appointment.IsCompleted &&
                                           x.Rating.PatientRating > 0)
                                .CountAsync();

                            if (ratingsCount > 0)
                            {
                                var ratingsSum = await _context.Ratings
                                    .Join(_context.Appointments,
                                          r => r.AppointmentId,
                                          a => a.Id,
                                          (r, a) => new { Rating = r, Appointment = a })
                                    .Where(x => x.Appointment.DoctorId == currentDoctor.Id && 
                                               x.Appointment.IsCompleted &&
                                               x.Rating.PatientRating > 0)
                                    .SumAsync(x => x.Rating.PatientRating);

                                SatisfactionPercentage = (int)Math.Round((ratingsSum / (double)(ratingsCount * 5)) * 100, 0);
                            }
                            else
                            {
                                SatisfactionPercentage = 0;
                            }
                        }
                        catch
                        {
                            SatisfactionPercentage = 0;
                        }

                        try
                        {
                            // Total de prescripciones
                            TotalPrescriptions = await _context.Prescriptions
                                .CountAsync(p => p.DoctorId == currentDoctor.Id && p.AppointmentId.HasValue);
                        }
                        catch
                        {
                            TotalPrescriptions = 0;
                        }

                        try
                        {
                            // Últimas prescripciones
                            RecentPrescriptions = await _context.Prescriptions
                                .Where(p => p.DoctorId == currentDoctor.Id && p.AppointmentId.HasValue)
                                .Include(p => p.Patient)
                                .Include(p => p.Medication)
                                .Include(p => p.Appointment)
                                .OrderByDescending(p => p.PrescriptionDate)
                                .Take(5)
                                .ToListAsync();
                        }
                        catch
                        {
                            RecentPrescriptions = new List<Prescription>();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Si todo falla, inicializar con valores por defecto
                TodayAppointments = 0;
                ConfirmedAppointments = 0;
                SatisfactionPercentage = 0;
                TotalPrescriptions = 0;
                RecentPrescriptions = new List<Prescription>();
            }
        }
    }
} 



