using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents an administrator user.
    public class Admin
    {
        // Primary key for the Admin entity.
        [Key]
        public int AdminId { get; set; }

        // Username for the admin login. Required.
        [Required]
        [StringLength(50)]
        public required string  Username { get; set; }

        // Hashed password for the admin account. Required.
        [Required]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public required string PasswordHash { get; set; }

        // Email address of the admin.
        [StringLength(100)]
        [DataType(DataType.EmailAddress)]
        public  required string Email { get; set; }

        // First name of the admin.
        [StringLength(100)]
        public required string FirstName { get; set; }

        // Last name of the admin.
        [StringLength(100)]
        public  required string LastName { get; set; }

        // Date and time of the admin's last login. Nullable.
        [DataType(DataType.DateTime)]
        public DateTime? LastLogin { get; set; }

        // Date and time the admin was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the admin information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Full name of the admin (derived property, not mapped to database).
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}