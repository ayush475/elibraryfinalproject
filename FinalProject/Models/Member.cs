using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models
{
    // Represents a library member.
    public class Member
    {
        // Primary key for the Member entity.
        [Key]
        public int MemberId { get; set; }

        // Unique membership identifier. Required.
        [Required]
        [StringLength(50)]
        public  required string MembershipId { get; set; }

        // First name of the member. Required.
        [Required]
        [StringLength(100)]
        public  required string FirstName { get; set; }

        // Last name of the member.
        [StringLength(100)]
        public  required string LastName { get; set; }

        // Email address of the member. Required.
        [Required]
        [StringLength(100)]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }

        // Hashed password for the member's account. Required.
        [Required]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public required string PasswordHash { get; set; }

        // Date and time the member registered.
        [DataType(DataType.DateTime)]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Date and time of the member's last login. Nullable.
        [DataType(DataType.DateTime)]
        public DateTime? LastLogin { get; set; }

        // Number of orders placed by the member.
        public int OrderCount { get; set; } = 0;

        // Stackable discount percentage applied to the member's orders.
        [Column(TypeName = "decimal(5, 2)")]
        public decimal StackableDiscount { get; set; } = 0.00M;

        // Date and time the member was added to the system.
        [DataType(DataType.DateTime)]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Date and time the member information was last updated.
        [DataType(DataType.DateTime)]
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        // Navigation properties

        // Collection of bookmarks created by the member.
        public  virtual required ICollection<Bookmark> Bookmarks { get; set; }

        // Collection of items in the member's shopping cart.
        public virtual required ICollection<ShoppingCartItem> ShoppingCartItems { get; set; }

        // Collection of orders placed by the member.
        public virtual required ICollection<Order> Orders { get; set; }

        // Collection of reviews written by the member.
        public virtual required ICollection<Review> Reviews { get; set; }

        // Full name of the member (derived property, not mapped to database).
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
