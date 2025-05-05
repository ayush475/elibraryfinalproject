using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents an order placed by a member.
    public class Order
    {
        // Primary key for the Order entity.
        [Key]
        public int OrderId { get; set; }

        // Foreign key for the Member who placed the order. Required at the database level.
        [Required]
        public int MemberId { get; set; }

        // Date and time the order was placed.
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Current status of the order (e.g., "Pending", "Processing", "Shipped").
        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending"; // Default status

        // Total amount of the order after discounts. Required.
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}")] // Currency format for display
        public decimal TotalAmount { get; set; }

        // Discount percentage applied to the order.
        [Column(TypeName = "decimal(5, 2)")]
        [DisplayFormat(DataFormatString = "{0:P2}")] // Percentage format for display
        public decimal DiscountApplied { get; set; } = 0.00M; // Default discount

        // Claim code associated with the order. Relaxed to be nullable.
        // Removed [Required] attribute and made the string nullable using '?'.
        [StringLength(50)] // Maximum length for the claim code
        public string? ClaimCode { get; set; } // Nullable string

        // Date and time the order was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the order information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation properties

        // The Member who placed the order.
        // Made non-required by removing 'required' keyword.
        // Can be null if the Order is loaded without including the Member.
        [ForeignKey("MemberId")] // Specifies the foreign key property
        public virtual Member? Member { get; set; }

        // Collection of items included in this order.
        // Made non-required by removing 'required' keyword.
        // Can be null or empty if the Order is loaded without including OrderItems.
        public virtual ICollection<OrderItem>? OrderItems { get; set; }
    }
}
