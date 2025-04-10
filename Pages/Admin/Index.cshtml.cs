using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitasEPS.Pages.Admin
{
    [Authorize(Policy = "Admin")] // Apply the Admin policy
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
} 