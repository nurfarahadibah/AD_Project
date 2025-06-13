using Microsoft.AspNetCore.Identity.UI.Services; // Required for IEmailSender
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using System.Net.Mail; // For SmtpClient, MailMessage
using System.Net; // For NetworkCredential
using System.Threading.Tasks; // For Task

namespace AspnetCoreMvcFull.Services // <--- IMPORTANT: Update this namespace to match your project and folder
{
  public class EmailSender : IEmailSender
  {
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
      var smtpHost = _configuration["SmtpSettings:Host"];
      var smtpPort = int.Parse(_configuration["SmtpSettings:Port"]); // int.Parse expects a non-null string
      var smtpUser = _configuration["SmtpSettings:Username"];
      var smtpPass = _configuration["SmtpSettings:Password"];
      var fromEmail = _configuration["SmtpSettings:FromEmail"];
      var fromName = _configuration["SmtpSettings:FromName"];

      // Validate that none of the required settings are null
      if (string.IsNullOrEmpty(smtpHost) ||
          string.IsNullOrEmpty(smtpUser) ||
          string.IsNullOrEmpty(smtpPass) ||
          string.IsNullOrEmpty(fromEmail))
      {
        // Log an error or throw a more specific exception if settings are missing
        throw new InvalidOperationException("One or more SMTP settings are missing or null. Check appsettings.json.");
      }

      var client = new SmtpClient(smtpHost, smtpPort)
      {
        Credentials = new NetworkCredential(smtpUser, smtpPass),
        EnableSsl = true // Most modern SMTP servers require SSL/TLS
      };

      // The SendMailAsync method handles the actual sending
      return client.SendMailAsync(
          new MailMessage(from: fromEmail,
                          to: email,
                          subject: subject,
                          body: htmlMessage)
          { IsBodyHtml = true } // Set this if your email body is HTML
      );
    }
  }
}
