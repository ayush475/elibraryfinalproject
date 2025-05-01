using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents an announcement displayed to users.
    public class Announcement
    {
        // Primary key for the Announcement entity.
        [Key]
        public int AnnouncementId { get; set; }

        // Title of the announcement. Required.
        [Required]
        [StringLength(255)]
        public required string Title { get; set; }

        // Message content of the announcement. Required.
        [Required]
        public required string Message { get; set; }

        // Optional start time for displaying the announcement. Nullable.
        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        // Optional end time for displaying the announcement. Nullable.
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        // Indicates if the announcement is currently active.
        public bool IsActive { get; set; } = true;

        // Date and time the announcement was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the announcement information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;
    }
}