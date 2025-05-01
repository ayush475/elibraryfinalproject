
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Genre information.
    public class GenreViewModel
    {
        // Unique identifier for the genre.
        public int GenreId { get; set; }

        // Name of the genre.
        public  required string Name { get; set; }

        // Description of the genre (might be truncated).
        public required string Description { get; set; }

        // Optional: Collection of Book ViewModels belonging to this genre if needed for display
        // public List<BookViewModel> Books { get; set; }
    }
}