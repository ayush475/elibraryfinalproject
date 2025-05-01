
namespace FinalProject.ViewModels
{
    // ViewModel for displaying Order information.
    public class OrderViewModel
    {
        // Unique identifier for the order.
        public int OrderId { get; set; }

        // Identifier of the member who placed the order.
        public int MemberId { get; set; }

        // Order date, formatted for display.
        public required string OrderDateDisplay { get; set; }

        // Current status of the order.
        public required string OrderStatus { get; set; }

        // Total amount of the order, formatted for display.
        public required string TotalAmountDisplay { get; set; }

        // Discount applied to the order, formatted for display.
        public required string DiscountAppliedDisplay { get; set; }

        // Claim code associated with the order.
        public  required string ClaimCode { get; set; }

        // Optional: Collection of OrderItem ViewModels included in this order
        public required List<OrderItemViewModel> OrderItems { get; set; }
    }
}