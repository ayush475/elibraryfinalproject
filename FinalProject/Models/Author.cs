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

        // First name of the author. Required as it's essential for identification.
        [Required] // Keep required
        [StringLength(100)]
        public required string FirstName { get; set; }

        // Last name of the author. Made nullable for temporary testing ease.
        // For robustness: Consider if LastName should be required in your final application.
        [StringLength(100)]
        public string? LastName { get; set; } // Made nullable ('required' removed)

        // Biography of the author. Made nullable for temporary testing ease.
        // For robustness: Consider if a biography is always required for authors.
        public string? Biography { get; set; } // Made nullable ('required' removed)

        // Date and time the author was added to the system. Has default value.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the author information was last updated. Has default value.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation property

        // Collection of books written by this author. Made nullable/non-required for temporary testing ease.
        // This allows you to create Author entities before you create Book entities linked to them.
        // For robustness: If an Author MUST always have books associated when loaded/saved,
        // you could make this 'required ICollection<Book> Books { get; set; }'.
        // However, a list of books is typically loaded explicitly, so making it nullable is often fine even in a robust model.
        public virtual ICollection<Book>? Books { get; set; } // Made nullable ('required' removed)

        // Full name of the author (derived property, not mapped to database).
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Optional: Add a constructor for easier creation during testing
        public Author()
        {
            // Initialize collections if needed, though nullable collections are often fine
            // Books = new List<Book>(); // Uncomment if you make Books non-nullable later
        }
    }
}