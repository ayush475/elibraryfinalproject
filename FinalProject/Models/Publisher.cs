using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a publisher.
    public class Publisher
    {
        // Primary key for the Publisher entity.
        [Key]
        public int PublisherId { get; set; }

        // Name of the publisher. Required.
        [Required]
        [StringLength(255)]
        public required string  Name { get; set; }

        // Address of the publisher.
        [StringLength(255)]
        public required string Address { get; set; }

        // Contact number of the publisher.
        [StringLength(20)]
        [DataType(DataType.PhoneNumber)]
        public  required string ContactNumber { get; set; }

        // Email address of the publisher.
        [StringLength(100)]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }

        // Date and time the publisher was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the publisher information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation property

        // Collection of books published by this publisher.
        public virtual required ICollection<Book> Books { get; set; }
    }
}