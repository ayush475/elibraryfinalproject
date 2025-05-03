using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a book in the library system.
    public class Book
    {
        // Primary key for the Book entity.
        [Key]
        public int BookId { get; set; }

        // International Standard Book Number (ISBN). Can be optional for initial testing.
        // [Required] // <-- Comment out or remove for temporary relaxation
        [StringLength(255)]
        public string? Isbn { get; set; } // Made nullable

        // Title of the book. Remains required as it's essential.
        [Required]
        [StringLength(255)]
        public required string Title { get; set; } // Still required

        // Description or summary of the book. Can be optional for initial testing.
        // [Required] // <-- Comment out or remove for temporary relaxation
        public string? Description { get; set; } // Made nullable

        // ** Suggestion: Add a Cover Image URL for UI **
        // Placeholder for the book cover image URL. Nullable for initial testing.
        [StringLength(1000)] // Allow longer URLs
        public string? CoverImageUrl { get; set; } // Added this property

        // Publication date of the book. Nullable. (Already nullable)
        [Column(TypeName = "Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? PublicationDate { get; set; }

        // The list price of the book. Required, as books usually have a price.
        [Required] // Keep required
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal ListPrice { get; set; }

        // Foreign key for the Author. Nullable. (Already nullable)
        public int? AuthorId { get; set; }

        // Foreign key for the Publisher. Nullable. (Already nullable)
        public int? PublisherId { get; set; }

        // Foreign key for the Genre. Nullable. (Already nullable)
        public int? GenreId { get; set; }

        // Language of the book. Can be optional for initial testing.
        // [Required] // <-- Comment out or remove for temporary relaxation
        [StringLength(50)]
        public string? Language { get; set; } // Made nullable

        // Format of the book (e.g., Hardcover, Paperback, Ebook). Can be optional.
        // [Required] // <-- Comment out or remove for temporary relaxation
        [StringLength(50)]
        public string? Format { get; set; } // Made nullable

        // Number of copies available in stock for purchase.
        public int AvailabilityStock { get; set; } = 0;

        // Indicates if the book is available for borrowing from the library.
        public bool AvailabilityLibrary { get; set; } = false;

        // Average rating of the book (out of 5). Nullable. (Already nullable)
        [Column(TypeName = "decimal(3, 2)")]
        public decimal? Rating { get; set; }

        // Total number of ratings received.
        public int RatingCount { get; set; } = 0;

        // Indicates if the book is currently on sale.
        public bool OnSale { get; set; } = false;

        // Discount percentage if the book is on sale. Nullable. (Already nullable)
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? SaleDiscount { get; set; }

        // Start date of the sale. Nullable. (Already nullable)
        [Column(TypeName = "Date")]
        [DataType(DataType.Date)]
        public DateTime? SaleStartDate { get; set; }

        // End date of the sale. Nullable. (Already nullable)
        [Column(TypeName = "Date")]
        [DataType(DataType.Date)]
        public DateTime? SaleEndDate { get; set; }

        // Date and time the book was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the book information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        // IMPORTANT for testing without related data: Make these nullable or remove 'required'.
        // Using 'virtual' is good practice for lazy loading, but the 'required' keyword enforces existence.

        // The Author of the book. Made nullable for temporary testing ease.
        [ForeignKey("AuthorId")]
        public virtual Author? Author { get; set; } // Made nullable

        // The Publisher of the book. Made nullable for temporary testing ease.
        [ForeignKey("PublisherId")]
        public virtual Publisher? Publisher { get; set; } // Made nullable

        // The Genre of the book. Made nullable for temporary testing ease.
        [ForeignKey("GenreId")]
        public virtual Genre? Genre { get; set; } // Made nullable

        // Collection of available formats for this book. Made nullable/non-required.
        public virtual ICollection<BookFormat>? BookFormats { get; set; } // Made nullable

        // Collection of reviews for this book. Made nullable/non-required.
        public virtual ICollection<Review>? Reviews { get; set; } // Made nullable

        // Collection of bookmarks for this book. Made nullable/non-required.
        public virtual ICollection<Bookmark>? Bookmarks { get; set; } // Made nullable

        // Collection of shopping cart items containing this book. Made nullable/non-required.
        public virtual ICollection<ShoppingCartItem>? ShoppingCartItems { get; set; } // Made nullable

        // Collection of order items containing this book. Made nullable/non-required.
        public virtual ICollection<OrderItem>? OrderItems { get; set; } // Made nullable
    }
}