namespace FinalProject.Configuration // Or adjust to your preferred namespace
{
    // This class will hold the email configuration settings
    public class EmailSettings
    {
        // The address of your SMTP server (e.g., smtp.gmail.com, smtp.office365.com)
        public string SmtpServer { get; set; }

        // The port number for your SMTP server (commonly 587 for TLS, 465 for SSL)
        public int SmtpPort { get; set; }

        // The username for authenticating with the SMTP server (usually your email address)
        public string SmtpUsername { get; set; }

        // The password or app-specific password for authenticating with the SMTP server
        // Be very careful with how you store this in production!
        public string SmtpPassword { get; set; }

        // Whether SSL/TLS encryption is required by the SMTP server
        public bool EnableSsl { get; set; }

        // The email address that the emails will be sent from
        public string FromEmail { get; set; }

        // Optional: Display name for the sender
        public string FromDisplayName { get; set; }
    }
}