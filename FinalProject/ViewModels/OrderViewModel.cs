using System;
using System.Collections.Generic; // Added for List
using System.ComponentModel.DataAnnotations;

namespace FinalProject.ViewModels
{
    /// <summary>
    /// ViewModel for displaying individual order information.
    /// </summary>
    public class OrderViewModel
    {
        // Unique identifier for the order.
        public int OrderId { get; set; }

        // Identifier of the member who placed the order.
        public int MemberId { get; set; }

        // Date and time the order was placed, formatted for display.
        [Display(Name = "Order Date")]
        public required string OrderDateDisplay { get; set; }

        // Current status of the order.
        [Display(Name = "Status")]
        public required string OrderStatus { get; set; }

        // Total amount of the order after discounts, formatted for display.
        [Display(Name = "Total Amount")]
        public required string TotalAmountDisplay { get; set; }

        // Discount percentage applied to the order, formatted for display.
        [Display(Name = "Discount Applied")]
        public required string DiscountAppliedDisplay { get; set; }

        // Claim code associated with the order (nullable).
        [Display(Name = "Claim Code")]
        public string? ClaimCode { get; set; }

        // Optional: Include a summary or list of items in the order.
        // For simplicity, we can start with just a count.
        [Display(Name = "Number of Items")]
        public int ItemCount { get; set; }

        // If you need to display details of each item within the order in this ViewModel,
        // you would add a collection like this (requires an OrderItemViewModel):
        // public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
// Collection of items within this order for display.
        // This property is needed for the OrderDetail action to populate and the view to display items.
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // Note: Mapping from the Order model to this ViewModel will typically
        // be done in the controller action that retrieves the orders.
    }
        // Note: Mapping from the Order model to this ViewModel will typically
        // be done in the controller action that retrieves the orders.
    }