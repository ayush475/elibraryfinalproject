using System.ComponentModel.DataAnnotations;
using FinalProject.Models;
using System.Collections.Generic; // Added for List

namespace FinalProject.ViewModels
{
    /// <summary>
    /// ViewModel for displaying member profile information, including shopping cart items.
    /// </summary>
    public class ProfileViewModel
    {
        // Basic Member Information
        public int MemberId { get; set; }
        public string MembershipId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; } // Derived property from Member model
        public string Email { get; set; }

        // Account Details
        [Display(Name = "Registration Date")]
        [DataType(DataType.DateTime)]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Last Login")]
        [DataType(DataType.DateTime)]
        public DateTime? LastLogin { get; set; }

        // Activity Information
        [Display(Name = "Total Orders")]
        public int OrderCount { get; set; }

        [Display(Name = "Stackable Discount")]
        [DataType(DataType.Currency)] // Optional: Use currency data type for display hints
        public decimal StackableDiscount { get; set; }

        // Shopping Cart Items for this member
        public List<ShoppingCartItemViewModel> CartItems { get; set; } = new List<ShoppingCartItemViewModel>(); // Added

        // You could add collections for related data if you want to display them
        // on the profile page, but be mindful of performance if the collections are large.
        // For example:
        // public IEnumerable<Order> RecentOrders { get; set; }
        // public IEnumerable<Review> RecentReviews { get; set; }

        /// <summary>
        /// Static method to create a ProfileViewModel from a Member model.
        /// This is a common pattern to map data from the model to the ViewModel.
        /// Note: This method now only maps basic member data. Cart items will be
        /// populated separately in the controller.
        /// </summary>
        /// <param name="member">The Member object to map from.</param>
        /// <returns>A new ProfileViewModel instance.</returns>
        public static ProfileViewModel FromMember(Member member)
        {
            if (member == null)
            {
                return null; // Or throw an exception
            }

            return new ProfileViewModel
            {
                MemberId = member.MemberId,
                MembershipId = member.MembershipId,
                FirstName = member.FirstName,
                LastName = member.LastName,
                FullName = member.FullName, // Use the derived property
                Email = member.Email,
                RegistrationDate = member.RegistrationDate,
                LastLogin = member.LastLogin,
                OrderCount = member.OrderCount,
                StackableDiscount = member.StackableDiscount
                // CartItems will be populated in the controller
                // Map related collections if you added them above
                // RecentOrders = member.Orders?.OrderByDescending(o => o.OrderDate).Take(5), // Example: show last 5 orders
                // RecentReviews = member.Reviews?.OrderByDescending(r => o.ReviewDate).Take(5) // Example: show last 5 reviews
            };
        }
    }
}
