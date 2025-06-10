using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitasEPS.Pages.UserDashboards.Patient
{
    [Authorize(Roles = "Paciente")]
    public class HealthStatusModel : PageModel
    {
        public void OnGet()
        {
        }
    }
} 