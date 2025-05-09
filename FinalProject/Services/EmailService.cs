using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options; // Required for accessing configuration

// Assuming your EmailSettings class is in the 'configuration' folder
using FinalProject.Configuration;// Adjust namespace if your EmailSettings are elsewhere

namespace FinalProject.Services // Place this file in the 'Services' folder
{
    // Implementation of the IEmailService
    // This class contains the logic to send emails using SMTP.
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        /// <summary>
        /// Constructor for the EmailService.
        /// Injects IOptions<EmailSettings> to access email configuration.
        /// </summary>
        /// <param name="emailSettings">The email configuration settings loaded from appsettings.json.</param>
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            // Basic check to ensure configuration was loaded
            // It's good practice to validate that required settings are present
            if (emailSettings?.Value == null)
            {
                 // In a real application, you would use a logger here
                 Console.Error.WriteLine("FATAL ERROR: Email settings are not configured. Check appsettings.json and Program.cs.");
                 // Throwing an exception here prevents the application from starting
                 // without necessary email configuration, which might be desired.
                 throw new ArgumentNullException(nameof(emailSettings), "Email settings are not configured.");
            }

            // Assign the loaded settings
            _emailSettings = emailSettings.Value;

            // Optional: Basic validation of critical settings
            if (string.IsNullOrEmpty(_emailSettings.SmtpServer) || _emailSettings.SmtpPort <= 0 || string.IsNullOrEmpty(_emailSettings.SmtpUsername) || string.IsNullOrEmpty(_emailSettings.SmtpPassword) || string.IsNullOrEmpty(_emailSettings.FromEmail))
            {
                 Console.Error.WriteLine("FATAL ERROR: Incomplete email settings provided in appsettings.json.");
                 throw new InvalidOperationException("Incomplete email settings provided.");
            }
        }

        /// <summary>
        /// Sends an email asynchronously using the configured SMTP settings.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The HTML body of the email.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Basic validation for required email fields for this specific email
            if (string.IsNullOrEmpty(toEmail) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                // Log this as a warning in a real application, but don't throw an exception
                // as it might be a valid scenario (e.g., a user without an email).
                Console.WriteLine("Warning: Attempted to send email with missing recipient, subject, or body.");
                return; // Do not attempt to send an incomplete email
            }

            // Create the MailMessage object
            using (MailMessage mail = new MailMessage())
            {
                try
                {
                    // Set the sender's email address and display name (if configured)
                    if (!string.IsNullOrEmpty(_emailSettings.FromDisplayName))
                    {
                         mail.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromDisplayName);
                    }
                    else
                    {
                         mail.From = new MailAddress(_emailSettings.FromEmail);
                    }

                    // Add the recipient(s)
                    mail.To.Add(toEmail);

                    // Set the subject and body
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true; // Indicate that the body is HTML

                    // Create the SmtpClient object with the configured server and port
                    using (SmtpClient smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                    {
                        // Set the credentials for authentication
                        smtp.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                        // Configure SSL/TLS encryption
                        smtp.EnableSsl = _emailSettings.EnableSsl;

                        // Optional: Add timeout (default is 100 seconds, might be too long)
                        // smtp.Timeout = 10000; // 10 seconds

                        // Send the email asynchronously
                        // Using SendMailAsync is preferred in async controller actions
                        await smtp.SendMailAsync(mail);

                        // Optional: Log successful email send
                        Console.WriteLine($"Email sent successfully to {toEmail} with subject: {subject}");
                    }
                }
                catch (SmtpException smtpEx)
                {
                    // Handle specific SMTP errors (e.g., authentication failure, server unavailable)
                    Console.Error.WriteLine($"SMTP Error sending email to {toEmail}: {smtpEx.StatusCode} - {smtpEx.Message}");
                    // In a real application, you would log this exception
                    throw; // Re-throw the exception so the caller can handle it (e.g., log it in the controller)
                }
                catch (Exception ex)
                {
                    // Handle other potential exceptions during email sending
                    Console.Error.WriteLine($"General Error sending email to {toEmail}: {ex.Message}");
                    // In a real application, you would log this exception
                    throw; // Re-throw the exception
                }
            }
        }
    }
}
