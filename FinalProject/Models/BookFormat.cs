using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a specific format available for a book (e.g., Hardcover, Ebook).
    public class BookFormat
    {
        // Primary key for the BookFormat entity.
        [Key]
        public int BookFormatId { get; set; }

        // Foreign key for the Book. Required.
        [Required]
        public int BookId { get; set; }

        // Type of format (e.g., "Hardcover", "Ebook", "Audiobook"). Required.
        [Required]
        [StringLength(100)]
        public required string FormatType { get; set; }

        // Additional details about the format (e.g., page count, file size).
        [StringLength(255)]
        public required string Details { get; set; }

        // Navigation property

        // The Book associated with this format.
        [ForeignKey("BookId")]
        public virtual  required Book Book { get; set; }
    }
}