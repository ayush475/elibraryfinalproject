using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a genre.
    public class Genre
    {
        // Primary key for the Genre entity.
        [Key]
        public int GenreId { get; set; }

        // Name of the genre. Required.
        [Required]
        [StringLength(100)]
        public  required string  Name { get; set; }

        // Description of the genre.
        public required string Description { get; set; }

        // Date and time the genre was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the genre information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation property

        // Collection of books belonging to this genre.
        public virtual  required ICollection<Book> Books { get; set; }
    }
}