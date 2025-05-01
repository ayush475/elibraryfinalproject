using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a review left by a member for a book.
    public class Review
    {
        // Primary key for the Review entity.
        [Key]
        public int ReviewId { get; set; }

        // Foreign key for the Member who wrote the review. Required.
        [Required]
        public int MemberId { get; set; }

        // Foreign key for the Book being reviewed. Required.
        [Required]
        public int BookId { get; set; }

        // Rating given by the member (1 to 5). Required.
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        // Text comment of the review.
        public required string  Comment { get; set; }

        // Date and time the review was written.
        [DataType(DataType.DateTime)]
        public DateTime ReviewDate { get; set; } = DateTime.Now;

        // Date and time the review was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation properties

        // The Member who wrote the review.
        [ForeignKey("MemberId")]
        public virtual  required Member Member { get; set; }

        // The Book being reviewed.
        [ForeignKey("BookId")]
        public virtual  required Book Book { get; set; }
    }
}