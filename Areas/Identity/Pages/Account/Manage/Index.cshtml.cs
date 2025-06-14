// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CitasEPS.Models; using CitasEPS.Models.Modules.Users; using CitasEPS.Models.Modules.Medical; using CitasEPS.Models.Modules.Appointments; using CitasEPS.Models.Modules.Core;
using CitasEPS.Models.Modules.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CitasEPS.Data;

namespace CitasEPS.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        public bool IsPatient { get; set; }
        public bool IsDoctor { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "El campo Nombres es obligatorio.")]
            [Display(Name = "Nombres")]
            [StringLength(100, ErrorMessage = "El campo Nombres no puede exceder los 100 caracteres.")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "El campo Apellidos es obligatorio.")]
            [Display(Name = "Apellidos")]
            [StringLength(100, ErrorMessage = "El campo Apellidos no puede exceder los 100 caracteres.")]
            public string LastName { get; set; }

            [Phone(ErrorMessage = "El formato del número de teléfono no es válido.")]
            [Display(Name = "Número de teléfono")]
            [Required(ErrorMessage = "El campo Número de teléfono es obligatorio.")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "El campo Fecha de Nacimiento es obligatorio.")]
            [Display(Name = "Fecha de Nacimiento")]
            [DataType(DataType.Date)]
            [MinimumAge(18, ErrorMessage = "Debes tener al menos 18 años.")]
            public DateTime DateOfBirth { get; set; }

            [Display(Name = "Documento de Identidad")]
            [StringLength(20, ErrorMessage = "El Documento de Identidad no puede exceder los 20 caracteres.")]
            public string? DocumentId { get; set; }

            [Display(Name = "Género")]
            public Gender? Gender { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = phoneNumber,
                DateOfBirth = user.DateOfBirth,
                DocumentId = user.DocumentId,
                Gender = user.Gender
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);

            IsPatient = await _userManager.IsInRoleAsync(user, "Paciente");
            IsDoctor = await _userManager.IsInRoleAsync(user, "Doctor");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"No se pudo cargar el usuario con ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Validaciones de unicidad antes de actualizar
            
            // Verificar si el número de teléfono ya existe (excluyendo el usuario actual)
            if (Input.PhoneNumber != user.PhoneNumber)
            {
                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == Input.PhoneNumber && u.Id != user.Id);
                if (existingUserByPhone != null)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una cuenta con este número de teléfono.");
                    await LoadAsync(user);
                    return Page();
                }
            }

            // Verificar si el documento de identidad ya existe (excluyendo el usuario actual)
            if (Input.DocumentId != user.DocumentId)
            {
                var existingUserByDocument = await _context.Users
                    .FirstOrDefaultAsync(u => u.DocumentId == Input.DocumentId && u.Id != user.Id);
                if (existingUserByDocument != null)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una cuenta con este documento de identidad.");
                    await LoadAsync(user);
                    return Page();
                }
            }

            // Update FirstName, LastName, and DateOfBirth directly on the user object
            bool profileUpdated = false;
            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                profileUpdated = true;
            }

            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                profileUpdated = true;
            }

            if (Input.DateOfBirth != user.DateOfBirth)
            {
                // Ensure the new date is saved as UTC, preserving only the date part
                user.DateOfBirth = new DateTime(Input.DateOfBirth.Year, Input.DateOfBirth.Month, Input.DateOfBirth.Day, 0, 0, 0, DateTimeKind.Utc);
                profileUpdated = true;
            }

            if (Input.DocumentId != user.DocumentId)
            {
                user.DocumentId = Input.DocumentId;
                profileUpdated = true;
            }

            if (Input.Gender != user.Gender)
            {
                user.Gender = Input.Gender;
                profileUpdated = true;
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Error: Error inesperado al intentar establecer el número de teléfono.";
                    return RedirectToPage();
                }
                profileUpdated = true;
            }

            if (profileUpdated)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Error: Error inesperado al intentar actualizar el perfil.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Tu perfil ha sido actualizado";
            return RedirectToPage();
        }
    }
}





