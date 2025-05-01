
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Announcement information.
    public class AnnouncementViewModel
    {
        // Unique identifier for the announcement.
        public int AnnouncementId { get; set; }

        // Title of the announcement.
        public required string Title { get; set; }

        // Message content of the announcement.
        public  required string Message { get; set; }

        // Optional start time, formatted for display (nullable).
        public required string StartTimeDisplay { get; set; }

        // Optional end time, formatted for display (nullable).
        public required string EndTimeDisplay { get; set; }

        // Indicates if the announcement is currently active.
        public  required bool IsActive { get; set; }
    }
}