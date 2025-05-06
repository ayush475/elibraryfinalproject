using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    /// <summary>
    /// Represents an administrator user.
    /// </summary>
    public class Admin
    {
        /// <summary>
        /// Primary key for the Admin entity.
        /// </summary>
        [Key]
        public int AdminId { get; set; }

        /// <summary>
        /// Username for the admin login. Required for authentication.
        /// </summary>
        [Required] // Still required for login functionality
        [StringLength(50)]
        public required string Username { get; set; }

        /// <summary>
        /// Hashed password for the admin account. Required for authentication.
        /// </summary>
        [Required] // Still required for login functionality
        [StringLength(255)] // Store a longer hash
        [DataType(DataType.Password)] // Provides a hint to the UI/tools
        public required string PasswordHash { get; set; }

        /// <summary>
        /// Email address of the admin. Made nullable as requested to relax constraints.
        /// Consider making this required again for password recovery or notifications in the future.
        /// Also consider adding a [EmailAddress] attribute for client-side validation.
        /// Consider adding a unique constraint at the database level.
        /// </summary>
        [StringLength(100)]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; } // Relaxed: made nullable

        /// <summary>
        /// First name of the admin. Made nullable as requested to relax constraints.
        /// Consider making this required for better identification in the future.
        /// </summary>
        [StringLength(100)]
        public string? FirstName { get; set; } // Relaxed: made nullable

        /// <summary>
        /// Last name of the admin. Made nullable as requested to relax constraints.
        /// Consider making this required for better identification in the future.
        /// </summary>
        [StringLength(100)]
        public string? LastName { get; set; } // Relaxed: made nullable

        /// <summary>
        /// Date and time of the admin's last login. Nullable.
        /// Consider adding tracking for failed login attempts or IP address.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Date and time the admin was added to the system. Automatically set on creation.
        /// Consider adding a 'CreatedBy' field to track which user created this admin account.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now; // Default value set

        /// <summary>
        /// Date and time the admin information was last updated. Automatically set on update.
        /// Consider adding a 'DateCreated' field distinct from 'DateAdded' if 'DateAdded' represents something else.
        /// Consider adding a 'DateModified' field and a 'ModifiedBy' field to track changes.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now; // Default value set

        // --- Future Improvement Considerations ---
        // public bool IsActive { get; set; } // Consider adding a status flag (active/inactive)
        // public DateTime? LockoutEnd { get; set; } // For implementing account lockout
        // public int AccessFailedCount { get; set; } // For tracking failed login attempts
        // public ICollection<AdminRole> AdminRoles { get; set; } // For implementing role-based access control (many-to-many relationship)
        // public string? ProfilePictureUrl { get; set; } // For storing a link to a profile picture

        /// <summary>
        /// Full name of the admin (derived property, not mapped to database).
        /// Provides a convenient way to get the full name.
        /// </summary>
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
        // If FirstName or LastName are null, this will still work, producing " LastName" or "FirstName ". Trim() cleans this up.
    }
}