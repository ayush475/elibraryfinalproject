using System.Threading.Tasks;

namespace FinalProject.Services // Place this file in a 'Services' folder
{
    // Interface for the email sending service
    // Defines the contract for sending emails.
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The HTML body of the email.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}