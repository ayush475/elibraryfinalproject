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

        // Foreign key for the Member who placed the order. Required.
        [Required]
        public int MemberId { get; set; }

        // Date and time the order was placed.
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Current status of the order (e.g., "Pending", "Processing", "Shipped").
        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending";

        // Total amount of the order after discounts. Required.
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalAmount { get; set; }

        // Discount percentage applied to the order.
        [Column(TypeName = "decimal(5, 2)")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal DiscountApplied { get; set; } = 0.00M;

        // Claim code associated with the order. Required.
        [Required]
        [StringLength(50)]
        public required string   ClaimCode { get; set; }

        // Date and time the order was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the order information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation properties

        // The Member who placed the order.
        [ForeignKey("MemberId")]
        public virtual required Member Member { get; set; }

        // Collection of items included in this order.
        public virtual  required ICollection<OrderItem> OrderItems { get; set; }
    }
}
