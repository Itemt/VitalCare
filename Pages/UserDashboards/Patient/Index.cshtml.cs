using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace CitasEPS.Pages.UserDashboards.Patient
{
    [Authorize(Roles = "Paciente")]
    public class PatientIndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
} 



