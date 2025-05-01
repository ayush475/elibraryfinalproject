
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Author information.
    public class AuthorViewModel
    {
        // Unique identifier for the author.
        public int AuthorId { get; set; }

        // Author's full name.
        public required string FullName { get; set; }

        // Biography of the author (might be truncated for lists).
        public  required string Biography { get; set; }

        // Optional: Collection of Book ViewModels written by this author if needed for display
        // public List<BookViewModel> Books { get; set; }
    }
}