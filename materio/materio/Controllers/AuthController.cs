using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace AspnetCoreMvcFull.Controllers;

public class AuthController : Controller
{
  private readonly SignInManager<IdentityUser> _signInManager;
  private readonly UserManager<IdentityUser> _userManager;

  public AuthController(SignInManager<IdentityUser> SignInManager, UserManager<IdentityUser> UserManager)
  {
    _signInManager = SignInManager;
    _userManager = UserManager;
  }

  public IActionResult ForgotPasswordBasic() => View();
  public IActionResult LoginBasic() => View();
  [HttpPost]
  public async Task<IActionResult> LoginBasic(LoginViewModel model)
  {
      if (!ModelState.IsValid) return View(model);

      var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

    if (result.Succeeded)
    {
      return RedirectToAction("Index", "Home");
    }

    ModelState.AddModelError("", "Invalid login attempt.");
    return View(model);

  }
  public IActionResult RegisterBasic() => View();
  [HttpPost]
  public async Task<IActionResult> RegisterBasic(RegisterViewModel model)
  {
    if (!ModelState.IsValid) return View(model);

    var user = new IdentityUser { UserName = model.Email, Email = model.Email };
    var result = await _userManager.CreateAsync(user, model.Password);

    if (result.Succeeded)
    {
      await _userManager.AddToRoleAsync(user, "User");
      await _signInManager.SignInAsync(user, isPersistent: false);
      return RedirectToAction("Index", "Home");
    }

    foreach (var error in result.Errors)
    {
      ModelState.AddModelError("", error.Description);
    }

    return View(model);
  }
}
