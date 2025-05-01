
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Book information.
    public class BookViewModel
    {
        // Unique identifier for the book.
        public int BookId { get; set; }

        // International Standard Book Number (ISBN).
        public required string Isbn { get; set; }

        // Title of the book.
        public required string Title { get; set; }

        // Description or summary of the book (might be truncated for lists).
        public required string Description { get; set; }

        // Publication date, formatted for display.
        public  required string PublicationDateDisplay { get; set; } // Formatted date string

        // The list price, formatted as currency.
        public required string ListPriceDisplay { get; set; } // Formatted currency string

        // Author's full name.
        public required string AuthorName { get; set; }

        // Publisher's name.
        public required string PublisherName { get; set; }

        // Genre name.
        public required string GenreName { get; set; }

        // Language of the book.
        public  required string Language { get; set; }

        // Format of the book (e.g., Hardcover, Paperback).
        public  required string Format { get; set; }

        // Indicates if the book is available in stock.
        public bool IsAvailableInStock { get; set; }

        // Indicates if the book is available for library borrowing.
        public bool IsAvailableForLibrary { get; set; }

        // Average rating of the book (out of 5).
        public decimal? Rating { get; set; }

        // Total number of ratings received.
        public int RatingCount { get; set; }

        // Indicates if the book is currently on sale.
        public bool IsOnSale { get; set; }

        // Discount percentage if the book is on sale.
        public decimal? SaleDiscount { get; set; }

        // Calculated sale price (derived property).
        public required string SalePriceDisplay { get; set; } // Formatted sale price string

        // Date and time the book was added, formatted for display.
        public required string DateAddedDisplay { get; set; } // Formatted date/time string

        // Optional: Collection of BookFormat ViewModels if needed for display
        public  required List<BookFormatViewModel> BookFormats { get; set; }

        // Optional: Collection of Review ViewModels if needed for display
        public  required List<ReviewViewModel> Reviews { get; set; }
    }
}
