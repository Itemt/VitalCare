using CitasEPS.Data;
using CitasEPS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CitasEPS.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class MyRatingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MyRatingsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Rating> DoctorRatings { get; set; } = new List<Rating>();
        public Models.Doctor? CurrentDoctor { get; set; }
        public RatingStats Stats { get; set; } = new RatingStats();

        public class RatingStats
        {
            public double AverageRating { get; set; }
            public int TotalRatings { get; set; }
            public int FiveStars { get; set; }
            public int FourStars { get; set; }
            public int ThreeStars { get; set; }
            public int TwoStars { get; set; }
            public int OneStar { get; set; }
            public double SatisfactionPercentage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            CurrentDoctor = await _context.Doctors
                .Include(d => d.Specialty)
                .FirstOrDefaultAsync(d => d.Email == user.Email);
            
            if (CurrentDoctor == null) 
                return Forbid("Registro de Doctor no encontrado.");

            // Obtener todas las calificaciones del doctor
            DoctorRatings = await _context.Ratings
                .Include(r => r.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(r => r.Appointment.Doctor)
                    .ThenInclude(d => d.Specialty)
                .Where(r => r.Appointment.DoctorId == CurrentDoctor.Id && r.PatientRating > 0)
                .OrderByDescending(r => r.PatientRatingDate)
                .ToListAsync();

            // Calcular estadísticas
            if (DoctorRatings.Any())
            {
                Stats.TotalRatings = DoctorRatings.Count;
                Stats.AverageRating = Math.Round(DoctorRatings.Average(r => r.PatientRating), 2);
                Stats.SatisfactionPercentage = Math.Round((Stats.AverageRating / 5.0) * 100, 1);

                Stats.FiveStars = DoctorRatings.Count(r => r.PatientRating == 5);
                Stats.FourStars = DoctorRatings.Count(r => r.PatientRating == 4);
                Stats.ThreeStars = DoctorRatings.Count(r => r.PatientRating == 3);
                Stats.TwoStars = DoctorRatings.Count(r => r.PatientRating == 2);
                Stats.OneStar = DoctorRatings.Count(r => r.PatientRating == 1);
            }

            return Page();
        }
    }
} 