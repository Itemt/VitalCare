using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitasEPS.Pages.UserDashboards.Admin
{
    [Authorize(Policy = "RequireAdminRole")] // Ensure only Admins can access
    public class ManageUsersModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public ManageUsersModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public IList<string> Roles { get; set; } = new List<string>();
        }

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Users.Add(new UserViewModel
                {
                    Id = user.Id.ToString(), // Assuming Id is int
                    UserName = user.UserName ?? "N/A",
                    Email = user.Email ?? "N/A",
                    Roles = roles
                });
            }
            // Optional: Order the list
            Users = Users.OrderBy(u => u.UserName).ToList();
        }
    }
} 




