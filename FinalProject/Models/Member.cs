
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; 

namespace FinalProject.Models
{
    // NOTE: [Required] attributes are commented out for testing flexibility.
    //       Restore them for production to ensure data integrity.
    public class Member
    {
        // Primary key for the Member entity.
        [Key]
        public int MemberId { get; set; }

        // Unique membership identifier. Required for production.
        //[Required] // Commented out for testing
        [StringLength(50)]
        public required string MembershipId { get; set; } // 'required' keyword still enforces non-nullability in C# 11+

        // First name of the member. Required for production.
        //[Required] // Commented out for testing
        [StringLength(100)]
        public required string FirstName { get; set; } // 'required' keyword still enforces non-nullability in C# 11+

        // Last name of the member.
        [StringLength(100)]
        public required string LastName { get; set; } // 'required' keyword still enforces non-nullability in C# 11+

        // Email address of the member. Required for production.
        //[Required] // Commented out for testing
        [StringLength(100)]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; } // 'required' keyword still enforces non-nullability in C# C# 11+

        // Hashed password for the member's account. Required for production.
        //[Required] // Commented out for testing
        [StringLength(255)]
        [DataType(DataType.Password)]
        public required string PasswordHash { get; set; } // 'required' keyword still enforces non-nullability in C# 11+

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
        // [JsonIgnore] prevents serialization cycles when serializing a Member
        [JsonIgnore] // <--- Added this attribute
        public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>(); // Initialize collections

        // Collection of items in the member's shopping cart.
        [JsonIgnore] // <--- Added this attribute (assuming ShoppingCartItem also links back to Member)
        public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>(); // Initialize collections

        // Collection of orders placed by the member.
        [JsonIgnore] // <--- Added this attribute (assuming Order also links back to Member)
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>(); // Initialize collections

        // Collection of reviews written by the member.
        [JsonIgnore] // <--- Added this attribute (assuming Review also links back to Member)
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>(); // Initialize collections


        // Full name of the member (derived property, not mapped to database).
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}