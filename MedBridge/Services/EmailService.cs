using System.Net;
using System.Net.Mail;

namespace MedBridge.Services;

public interface IEmailService
{
    Task SendWelcomeAsync(string email, string fullName, string tempPassword);
    Task SendPasswordResetAsync(string email, string fullName, string resetLink);
    Task SendPasswordExpiryReminderAsync(string email, string fullName, string resetLink);
    Task SendHelpTicketNotificationAsync(string email, string fullName, string ticketRef, string subject, bool isUpdate = false);
}

public class EmailService : IEmailService
{
    private const string FromAddress  = "mamishimadire@gmail.com";
    private const string FromName     = "MedBridge EMR";
    private const string SmtpHost     = "smtp.gmail.com";
    private const int    SmtpPort     = 587;
    private const string SmtpUser     = "mamishimadire@gmail.com";
    private const string SmtpPassword = "Mntonnymadire980114@";

    public async Task SendWelcomeAsync(string email, string fullName, string tempPassword)
    {
        var body = $"""
            Hello {fullName},

            Welcome to MedBridge EMR. Your account has been created by the system administrator.

            Your login details:
              Email:             {email}
              Temporary Password: {tempPassword}

            IMPORTANT: You must change your password on first login.
            Your password must be at least 8 characters and include uppercase, lowercase, a digit, and a special character.

            Visit the system to sign in: https://medbridge.local/Account/Login

            If you did not expect this email, please contact your administrator immediately.

            Regards,
            MedBridge EMR System
            """;

        await SendAsync(email, $"Welcome to MedBridge — Your Account Details", body);
    }

    public async Task SendPasswordResetAsync(string email, string fullName, string resetLink)
    {
        var body = $"""
            Hello {fullName},

            A password reset was requested for your MedBridge account.

            Click the link below to reset your password (valid for 24 hours):
            {resetLink}

            If you did not request a password reset, please ignore this email — your password remains unchanged.

            Regards,
            MedBridge EMR System
            """;

        await SendAsync(email, "MedBridge — Password Reset Request", body);
    }

    public async Task SendPasswordExpiryReminderAsync(string email, string fullName, string resetLink)
    {
        var body = $"""
            Hello {fullName},

            Your MedBridge password is due for a mandatory reset (passwords expire every 30 days).

            Please click the link below to set a new password:
            {resetLink}

            This is a security requirement. If you need assistance, contact your system administrator.

            Regards,
            MedBridge EMR System
            """;

        await SendAsync(email, "MedBridge — Password Expiry Reminder", body);
    }

    public async Task SendHelpTicketNotificationAsync(string email, string fullName, string ticketRef, string subject, bool isUpdate = false)
    {
        var action = isUpdate ? "updated" : "received";
        var body = $"""
            Hello {fullName},

            Your help desk ticket has been {action}.

            Ticket:  {ticketRef}
            Subject: {subject}

            Log in to MedBridge to view the full ticket and any responses.

            Regards,
            MedBridge Help Desk
            """;

        var emailSubject = isUpdate
            ? $"MedBridge Help Desk — Ticket {ticketRef} Updated"
            : $"MedBridge Help Desk — Ticket {ticketRef} Received";

        await SendAsync(email, emailSubject, body);
    }

    private async Task SendAsync(string toAddress, string subject, string body)
    {
        var mail = new MailMessage
        {
            From       = new MailAddress(FromAddress, FromName),
            Subject    = subject,
            Body       = body,
            IsBodyHtml = false,
        };
        mail.To.Add(new MailAddress(toAddress));

        using var smtp = new SmtpClient(SmtpHost, SmtpPort)
        {
            Credentials = new NetworkCredential(SmtpUser, SmtpPassword),
            EnableSsl   = true
        };

        await smtp.SendMailAsync(mail);
    }
}
