using MedBridge.Data;
using MedBridge.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Services;

public class PasswordExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PasswordExpiryService> _logger;

    public PasswordExpiryService(IServiceScopeFactory scopeFactory, ILogger<PasswordExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiryRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PasswordExpiryService");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessExpiryRemindersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var users    = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email    = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var tokenUrlSvc = scope.ServiceProvider.GetRequiredService<ITokenUrlService>();

        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Users whose password was last changed more than 30 days ago (or never)
        var expiredUsers = await users.Users
            .Where(u => u.IsActive
                && !u.MustChangePassword
                && (u.PasswordChangedAt == null || u.PasswordChangedAt < cutoff))
            .ToListAsync();

        foreach (var user in expiredUsers)
        {
            if (string.IsNullOrEmpty(user.Email)) continue;

            var token     = await users.GeneratePasswordResetTokenAsync(user);
            var resetLink = tokenUrlSvc.BuildResetLink(user.Email, token);

            await email.SendPasswordExpiryReminderAsync(user.Email, user.FullName, resetLink);
            user.MustChangePassword = true;
            await users.UpdateAsync(user);

            _logger.LogInformation("Password expiry reminder sent to {Email}", user.Email);
        }
    }
}

// Helper to build reset links without HttpContext (background service has none)
public interface ITokenUrlService
{
    string BuildResetLink(string email, string token);
}

public class TokenUrlService : ITokenUrlService
{
    private readonly IConfiguration _config;

    public TokenUrlService(IConfiguration config) => _config = config;

    public string BuildResetLink(string email, string token)
    {
        var baseUrl = _config["AppBaseUrl"] ?? "https://medbridge.local";
        var encoded = Uri.EscapeDataString(token);
        var encEmail = Uri.EscapeDataString(email);
        return $"{baseUrl}/Account/ResetPassword?email={encEmail}&token={encoded}";
    }
}
