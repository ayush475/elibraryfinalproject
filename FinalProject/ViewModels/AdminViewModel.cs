
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Admin information.
    public class AdminViewModel
    {
        // Unique identifier for the admin.
        public int AdminId { get; set; }

        // Username for the admin.
        public required string Username { get; set; }

        // Email address of the admin.
        public required string Email { get; set; }

        // Admin's full name.
        public required string FullName { get; set; }

        // Date of last login, formatted for display (nullable).
        public required string LastLoginDisplay { get; set; }

        // Note: Avoid including sensitive information like PasswordHash in ViewModels.
    }
}