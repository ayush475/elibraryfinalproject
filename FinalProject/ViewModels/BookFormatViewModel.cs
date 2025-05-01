namespace FinalProject.ViewModels
{
    // ViewModel for displaying Book Format information.
    public class BookFormatViewModel
    {
        // Unique identifier for the book format.
        public int BookFormatId { get; set; }

        // Type of format (e.g., "Hardcover", "Ebook").
        public required string FormatType { get; set; }

        // Additional details about the format.
        public required string Details { get; set; }

        // Note: Typically, you might not include the full Book ViewModel here
        // to avoid circular references or excessive data loading, but you could
        // include a BookId or BookTitle if necessary.
        // public int BookId { get; set; }
        // public string BookTitle { get; set; }
    }
}