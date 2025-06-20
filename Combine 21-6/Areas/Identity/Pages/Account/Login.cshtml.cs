// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using AspnetCoreMvcFull.Models; // Ensure this is the correct namespace for ApplicationUser
using System.Security.Claims;

namespace AspnetCoreMvcFull.Areas.Identity.Pages.Account
{
  public class LoginModel : PageModel
  {
    private readonly SignInManager<ApplicationUser> _signInManager; // Use ApplicationUser
    private readonly UserManager<ApplicationUser> _userManager;     // Use ApplicationUser
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
    {
      _signInManager = signInManager;
      _userManager = userManager;
      _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public class InputModel
    {
      [Required]
      [EmailAddress]
      public string Email { get; set; }

      [Required]
      [DataType(DataType.Password)]
      public string Password { get; set; }

      [Display(Name = "Remember me?")]
      public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string returnUrl = null)
    {
      if (!string.IsNullOrEmpty(ErrorMessage))
      {
        ModelState.AddModelError(string.Empty, ErrorMessage);
      }

      returnUrl ??= Url.Content("~/");

      // Clear the existing external cookie to ensure a clean login process
      await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

      ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
      returnUrl ??= Url.Content("~/"); // Default redirect, can be overridden by successful login below.

      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

      if (ModelState.IsValid)
      {
        // This doesn't count login failures towards account lockout
        // To enable password failures to trigger account lockout, set lockoutOnFailure: true
        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
          _logger.LogInformation("User logged in.");

          var user = await _userManager.FindByEmailAsync(Input.Email);
          if (user != null) // User should not be null if PasswordSignInAsync succeeded
          {
            // --- Claims-based approach (from first snippet) ---
            // Get the current claims principal after successful login
            // This principal might not yet have the TenantId or Role claims.
            var currentPrincipal = HttpContext.User;

            // Create a new list of claims including existing ones and the TenantId/Roles
            var claims = new List<Claim>();
            claims.AddRange(currentPrincipal.Claims); // Add existing claims from the initial sign-in

            // Add the TenantId claim if it's not already present
            if (!string.IsNullOrEmpty(user.TenantId) && !claims.Any(c => c.Type == "TenantId"))
            {
              claims.Add(new Claim("TenantId", user.TenantId));
            }

            // Add Role Claims
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
              // Only add the role claim if it's not already present
              if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role))
              {
                claims.Add(new Claim(ClaimTypes.Role, role));
              }
            }

            // Create a new ClaimsIdentity and ClaimsPrincipal with all collected claims
            // Use the authentication scheme that SignInManager uses for cookies
            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var newPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Sign out the existing principal and then sign in the new one
            // This ensures the new principal with all custom claims is used for subsequent requests
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme); // Sign out the existing cookie
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, newPrincipal, new AuthenticationProperties { IsPersistent = Input.RememberMe });

            _logger.LogInformation($"User {user.UserName} re-signed in with TenantId: {user.TenantId} and Roles: {string.Join(", ", userRoles)}");

            // --- Session-based approach (from second snippet) ---
            HttpContext.Session.SetString("Username", user.UserName);
            HttpContext.Session.SetString("Role", userRoles.FirstOrDefault() ?? "User");
            // --- End Session-based approach ---
          }

          // Redirect to the default route (Dashboards/Index) as per your Program.cs or the returnUrl
          return RedirectToAction("Index", "Dashboards");
        }
        if (result.RequiresTwoFactor)
        {
          return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
        }
        if (result.IsLockedOut)
        {
          _logger.LogWarning("User account locked out.");
          return RedirectToPage("./Lockout");
        }
        else
        {
          ModelState.AddModelError(string.Empty, "Invalid login attempt.");
          return Page();
        }
      }

      // If we got this far, something failed, redisplay form
      return Page();
    }
  }
}

//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//#nullable disable

//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.UI.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Extensions.Logging;
//using AspnetCoreMvcFull.Models;
//using System.Security.Claims; // Add this using statement

//namespace AspnetCoreMvcFull.Areas.Identity.Pages.Account
//{
//  public class LoginModel : PageModel
//  {
//    private readonly SignInManager<ApplicationUser> _signInManager;
//    private readonly ILogger<LoginModel> _logger;
//    private readonly UserManager<ApplicationUser> _userManager; // Add UserManager

//    // Update constructor to inject UserManager
//    public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, UserManager<ApplicationUser> userManager)
//    {
//      _signInManager = signInManager;
//      _logger = logger;
//      _userManager = userManager; // Assign UserManager
//    }

//    [BindProperty]
//    public InputModel Input { get; set; }

//    public IList<AuthenticationScheme> ExternalLogins { get; set; }

//    public string ReturnUrl { get; set; }

//    [TempData]
//    public string ErrorMessage { get; set; }

//    public class InputModel
//    {
//      [Required]
//      [EmailAddress]
//      public string Email { get; set; }

//      [Required]
//      [DataType(DataType.Password)]
//      public string Password { get; set; }

//      [Display(Name = "Remember me?")]
//      public bool RememberMe { get; set; }
//    }

//    public async Task OnGetAsync(string returnUrl = null)
//    {
//      if (!string.IsNullOrEmpty(ErrorMessage))
//      {
//        ModelState.AddModelError(string.Empty, ErrorMessage);
//      }

//      returnUrl ??= Url.Content("~/");

//      await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

//      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

//      ReturnUrl = returnUrl;
//    }

//    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
//    {
//      returnUrl ??= Url.Content("~/");

//      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

//      if (ModelState.IsValid)
//      {
//        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
//        if (result.Succeeded)
//        {
//          _logger.LogInformation("User logged in.");

//          // Find the user to get their TenantId and Roles
//          var user = await _userManager.FindByEmailAsync(Input.Email);
//          if (user != null) // User should not be null if PasswordSignInAsync succeeded
//          {
//            // Get the current claims principal after successful login
//            // This principal might not yet have the TenantId or Role claims.
//            var currentPrincipal = HttpContext.User;

//            // Create a new list of claims including existing ones and the TenantId/Roles
//            var claims = new List<Claim>();
//            claims.AddRange(currentPrincipal.Claims); // Add existing claims from the initial sign-in

//            // Add the TenantId claim if it's not already present
//            if (!string.IsNullOrEmpty(user.TenantId) && !claims.Any(c => c.Type == "TenantId"))
//            {
//              claims.Add(new Claim("TenantId", user.TenantId));
//            }

//            // --- NEW CODE: Add Role Claims ---
//            var userRoles = await _userManager.GetRolesAsync(user);
//            foreach (var role in userRoles)
//            {
//              // Only add the role claim if it's not already present
//              // (SignInManager might add some default role claims, so check to avoid duplicates)
//              if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role))
//              {
//                claims.Add(new Claim(ClaimTypes.Role, role));
//              }
//            }
//            // --- END NEW CODE: Add Role Claims ---

//            // Create a new ClaimsIdentity and ClaimsPrincipal with all collected claims
//            // Use the authentication scheme that SignInManager uses for cookies
//            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
//            var newPrincipal = new ClaimsPrincipal(claimsIdentity);

//            // Sign out the existing principal and then sign in the new one
//            // This ensures the new principal with all custom claims is used for subsequent requests
//            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme); // Sign out the existing cookie
//            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, newPrincipal, new AuthenticationProperties { IsPersistent = Input.RememberMe });

//            _logger.LogInformation($"User {user.UserName} re-signed in with TenantId: {user.TenantId} and Roles: {string.Join(", ", userRoles)}");
//          }

//          // Redirect to the default route (Dashboards/Index) as per your Program.cs
//          return RedirectToAction("Index", "Dashboards");
//        }
//        if (result.RequiresTwoFactor)
//        {
//          return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
//        }
//        if (result.IsLockedOut)
//        {
//          _logger.LogWarning("User account locked out.");
//          return RedirectToPage("./Lockout");
//        }
//        else
//        {
//          ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//          return Page();
//        }
//      }

//      // If we got this far, something failed, redisplay form
//      return Page();
//    }
//  }
//}
