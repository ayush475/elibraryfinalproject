
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Bookmark information.
    public class BookmarkViewModel
    {
        // Unique identifier for the bookmark.
        public int BookmarkId { get; set; }

        // Identifier of the member who created the bookmark.
        public int MemberId { get; set; }

        // Identifier of the book that was bookmarked.
        public int BookId { get; set; }

        // Date the bookmark was added, formatted for display.
        public required string DateAddedDisplay { get; set; }

        // Optional: Include basic info about the book if needed without the full BookViewModel
        public required string BookTitle { get; set; }
        public required string BookAuthorName { get; set; }
    }
}