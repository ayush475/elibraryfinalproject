using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents an item in a member's shopping cart.
    public class ShoppingCartItem
    {
        // Primary key for the ShoppingCartItem entity.
        [Key]
        public int CartItemId { get; set; }

        // Foreign key for the Member whose cart this item is in. Required.
        [Required]
        public int MemberId { get; set; }

        // Foreign key for the Book in the shopping cart. Required.
        [Required]
        public int BookId { get; set; }

        // Quantity of the book in the shopping cart. Required.
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        // Date and time the item was added to the cart.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Navigation properties

        // The Member whose cart this item is in.
        [ForeignKey("MemberId")]
        public virtual required Member Member { get; set; }

        // The Book in the shopping cart.
        [ForeignKey("BookId")]
        public virtual required Book Book { get; set; }

        // Calculated total price for this item (derived property, not mapped to database).
        [NotMapped]
        public decimal TotalPrice => Book?.ListPrice * Quantity ?? 0;
    }
}