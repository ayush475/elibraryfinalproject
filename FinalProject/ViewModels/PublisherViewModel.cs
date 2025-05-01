

namespace FinalProject.ViewModels
{
    // ViewModel for displaying Publisher information.
    public class PublisherViewModel
    {
        // Unique identifier for the publisher.
        public int PublisherId { get; set; }

        // Name of the publisher.
        public required string Name { get; set; }

        // Address of the publisher.
        public required string Address { get; set; }

        // Contact number of the publisher.
        public required string ContactNumber { get; set; }

        // Email address of the publisher.
        public required string Email { get; set; }

        // Optional: Collection of Book ViewModels published by this publisher if needed for display
        // public List<BookViewModel> Books { get; set; }
    }
}