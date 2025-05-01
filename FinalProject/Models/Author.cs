using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents an author.
    public class Author
    {
        // Primary key for the Author entity.
        [Key]
        public int AuthorId { get; set; }

        // First name of the author. Required.
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        // Last name of the author.
        [StringLength(100)]
        public required string LastName { get; set; }

        // Biography of the author.
        public required string Biography { get; set; }

        // Date and time the author was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the author information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation property

        // Collection of books written by this author.
        public virtual  required ICollection<Book> Books { get; set; }

        // Full name of the author (derived property, not mapped to database).
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}