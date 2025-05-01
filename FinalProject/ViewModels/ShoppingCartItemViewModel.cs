
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Shopping Cart Item information.
    public class ShoppingCartItemViewModel
    {
        // Unique identifier for the cart item.
        public int CartItemId { get; set; }

        // Identifier of the member whose cart this item is in.
        public int MemberId { get; set; }

        // Identifier of the book in the cart item.
        public int BookId { get; set; }

        // Quantity of the book in the cart.
        public int Quantity { get; set; }

        // Date the item was added to the cart, formatted for display.
        public required string  DateAddedDisplay { get; set; }

        // Optional: Include basic info about the book
        public required string BookTitle { get; set; }
        public  required string BookAuthorName { get; set; }
        public  required string BookListPriceDisplay { get; set; } // Price of the book

        // Calculated total price for this item, formatted for display.
        public required string TotalPriceDisplay { get; set; }
    }
}