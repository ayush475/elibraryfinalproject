
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Order Item information.
    public class OrderItemViewModel
    {
        // Unique identifier for the order item.
        public int OrderItemId { get; set; }

        // Identifier of the order this item belongs to.
        public int OrderId { get; set; }

        // Identifier of the book in this order item.
        public int BookId { get; set; }

        // Quantity of the book.
        public int Quantity { get; set; }

        // Unit price of the book at the time of the order, formatted for display.
        public required string UnitPriceDisplay { get; set; }

        // Discount applied to this specific item, formatted for display.
        public required string DiscountDisplay { get; set; }

        // Calculated total price for this item, formatted for display.
        public required string TotalPriceDisplay { get; set; }

        // Optional: Include basic info about the book
        public  required string BookTitle { get; set; }
        public required string BookAuthorName { get; set; }
    }
}