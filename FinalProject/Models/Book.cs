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

        // International Standard Book Number (ISBN). Required and unique.
        [Required]
        [StringLength(255)]
        public required string  Isbn { get; set; }

        // Title of the book. Required.
        [Required]
        [StringLength(255)]
        public required string Title { get; set; }

        // Description or summary of the book.
        public required string Description { get; set; }

        // Publication date of the book. Nullable.
        [Column(TypeName = "Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? PublicationDate { get; set; }

        // The list price of the book. Required.
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal ListPrice { get; set; }

        // Foreign key for the Author. Nullable if author is unknown.
        public int? AuthorId { get; set; }

        // Foreign key for the Publisher. Nullable if publisher is unknown.
        public int? PublisherId { get; set; }

        // Foreign key for the Genre. Nullable if genre is unknown.
        public int? GenreId { get; set; }

        // Language of the book.
        [StringLength(50)]
        public required string Language { get; set; }

        // Format of the book (e.g., Hardcover, Paperback, Ebook).
        [StringLength(50)]
        public required string Format { get; set; }

        // Number of copies available in stock for purchase.
        public int AvailabilityStock { get; set; } = 0;

        // Indicates if the book is available for borrowing from the library.
        public bool AvailabilityLibrary { get; set; } = false;

        // Average rating of the book (out of 5). Nullable.
        [Column(TypeName = "decimal(3, 2)")]
        public decimal? Rating { get; set; }

        // Total number of ratings received.
        public int RatingCount { get; set; } = 0;

        // Indicates if the book is currently on sale.
        public bool OnSale { get; set; } = false;

        // Discount percentage if the book is on sale. Nullable.
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? SaleDiscount { get; set; }

        // Start date of the sale. Nullable.
        [Column(TypeName = "Date")]
        [DataType(DataType.Date)]
        public DateTime? SaleStartDate { get; set; }

        // End date of the sale. Nullable.
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

        // The Author of the book.
        [ForeignKey("AuthorId")]
        public virtual  required Author Author { get; set; }

        // The Publisher of the book.
        [ForeignKey("PublisherId")]
        public virtual required Publisher Publisher { get; set; }

        // The Genre of the book.
        [ForeignKey("GenreId")]
        public virtual required Genre Genre { get; set; }

        // Collection of available formats for this book.
        public virtual required ICollection<BookFormat> BookFormats { get; set; }

        // Collection of reviews for this book.
        public  virtual  required ICollection<Review> Reviews { get; set; }

        // Collection of bookmarks for this book.
        public virtual required ICollection<Bookmark> Bookmarks { get; set; }

        // Collection of shopping cart items containing this book.
        public virtual required ICollection<ShoppingCartItem> ShoppingCartItems { get; set; }

        // Collection of order items containing this book.
        public virtual  required ICollection<OrderItem> OrderItems { get; set; }
    }
}

