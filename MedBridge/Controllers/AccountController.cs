using MedBridge.Models;
using MedBridge.Services;
using MedBridge.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedBridge.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailService _email;
    private readonly ITokenUrlService _tokenUrl;

    public AccountController(SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        IEmailService email,
        ITokenUrlService tokenUrl)
    {
        _signIn   = signIn;
        _users    = users;
        _email    = email;
        _tokenUrl = tokenUrl;
    }

    // ── Login ──────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signIn.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _users.FindByEmailAsync(model.Email);
            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await _users.UpdateAsync(user);

                if (user.MustChangePassword)
                    return RedirectToAction("ChangePassword");
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
            ModelState.AddModelError("", "Account locked. Try again in 15 minutes.");
        else
            ModelState.AddModelError("", "Invalid username or password.");

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ── Forgot Password ────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user != null && user.IsActive)
        {
            var token     = await _users.GeneratePasswordResetTokenAsync(user);
            var resetLink = _tokenUrl.BuildResetLink(model.Email, token);
            await _email.SendPasswordResetAsync(model.Email, user.FullName, resetLink);
        }

        TempData["Info"] = "If that email is registered, a reset link has been sent. Check your inbox.";
        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ── Reset Password ─────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user == null)
        {
            TempData["Error"] = "Invalid reset request.";
            return RedirectToAction("Login");
        }

        var result = await _users.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (result.Succeeded)
        {
            user.PasswordChangedAt  = DateTime.UtcNow;
            user.MustChangePassword = false;
            await _users.UpdateAsync(user);
            TempData["Success"] = "Password reset successfully. Please sign in with your new password.";
            return RedirectToAction("Login");
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError("", err.Description);

        return View(model);
    }

    // ── Change Password (forced on first login / 30-day expiry) ───────────
    [HttpGet, Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            user.PasswordChangedAt  = DateTime.UtcNow;
            user.MustChangePassword = false;
            await _users.UpdateAsync(user);
            await _signIn.RefreshSignInAsync(user);
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError("", err.Description);

        return View(model);
    }
}
