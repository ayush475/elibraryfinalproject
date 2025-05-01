
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Member information.
    public class MemberViewModel
    {
        // Unique identifier for the member.
        public int MemberId { get; set; }

        // Unique membership identifier.
        public required string MembershipId { get; set; }

        // Member's full name.
        public required string FullName { get; set; }

        // Email address of the member.
        public required string Email { get; set; }

        // Registration date, formatted for display.
        public required string RegistrationDateDisplay { get; set; }

        // Date of last login, formatted for display (nullable).
        public required string LastLoginDisplay { get; set; }

        // Number of orders placed by the member.
        public int OrderCount { get; set; }

        // Stackable discount percentage, formatted for display.
        public  required string StackableDiscountDisplay { get; set; }

        // Note: Avoid including sensitive information like PasswordHash in ViewModels.
    }
}