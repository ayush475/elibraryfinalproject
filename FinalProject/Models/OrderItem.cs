using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents an item within an order.
    public class OrderItem
    {
        // Primary key for the OrderItem entity.
        [Key]
        public int OrderItemId { get; set; }

        // Foreign key for the Order this item belongs to. Required.
        [Required]
        public int OrderId { get; set; }

        // Foreign key for the Book in this order item. Required.
        [Required]
        public int BookId { get; set; }

        // Quantity of the book in this order item. Required.
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Unit price of the book at the time of the order. Required.
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal UnitPrice { get; set; }

        // Discount applied to this specific order item.
        [Column(TypeName = "decimal(5, 2)")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal Discount { get; set; } = 0.00M;

        // Navigation properties

        // The Order this item belongs to.
        [ForeignKey("OrderId")]
        public virtual  required Order Order { get; set; }

        // The Book in this order item.
        [ForeignKey("BookId")]
        public virtual  required Book Book { get; set; }

        // Calculated total price for this order item (derived property, not mapped to database).
        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity * (1 - Discount);
    }
}