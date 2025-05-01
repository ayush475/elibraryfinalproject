

namespace FinalProject.ViewModels
{
    // ViewModel for displaying Review information.
    public class ReviewViewModel
    {
        // Unique identifier for the review.
        public int ReviewId { get; set; }

        // Identifier of the member who wrote the review.
        public int MemberId { get; set; }

        // Identifier of the book being reviewed.
        public int BookId { get; set; }

        // Rating given by the member (1 to 5).
        public int Rating { get; set; }

        // Text comment of the review.
        public required string Comment { get; set; }

        // Review date, formatted for display.
        public  required string ReviewDateDisplay { get; set; }

        // Optional: Include basic info about the member and book
        public required string MemberFullName { get; set; }
        public required string BookTitle { get; set; }
    }
}