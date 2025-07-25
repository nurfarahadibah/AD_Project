// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace AspnetCoreMvcFull.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
          return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                  // Don't reveal that the user does not exist or is not confirmed
                  //return RedirectToPage("./ForgotPasswordConfirmation");
                  ModelState.AddModelError(string.Empty, "User not found.");
                  return Page();
                }

        // First, remove the existing password hash
        var removePasswordResult = await _userManager.RemovePasswordAsync(user);
        if (!removePasswordResult.Succeeded)
        {
          foreach (var error in removePasswordResult.Errors)
          {
            ModelState.AddModelError(string.Empty, error.Description);
          }
          return Page();
        }

        // Then, add the new password
        var addPasswordResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
        if (!addPasswordResult.Succeeded)
        {
          foreach (var error in addPasswordResult.Errors)
          {
            ModelState.AddModelError(string.Empty, error.Description);
          }
          // This is a problematic state: password was removed but new one couldn't be set.
          // In a real app, you'd need robust error handling here (e.g., log, notify admin, force password change).
          return Page();
        }

        // Optional: Sign out the user if they were already logged in (important for password changes)
        await _signInManager.SignOutAsync();

        // Set a message to display on the login page
        TempData["StatusMessage"] = "Your password has been reset successfully. Please log in with your new password.";

        // Redirect to the login page
        return RedirectToPage("./Login");

        //// For more information on how to enable account confirmation and password reset please
        //// visit https://go.microsoft.com/fwlink/?LinkID=532713
        //var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        //code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        //var callbackUrl = Url.Page(
        //    "/Account/ResetPassword",
        //    pageHandler: null,
        //    values: new { area = "Identity", code },
        //    protocol: Request.Scheme);

        //await _emailSender.SendEmailAsync(
        //    Input.Email,
        //    "Reset Password",
        //    $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

        //return RedirectToPage("./ForgotPasswordConfirmation");
      }

            return Page();
        }
    }
}
