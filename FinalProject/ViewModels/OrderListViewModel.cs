using System.Collections.Generic;
using FinalProject.Models; // Make sure to use your actual namespace for the Order model
using System; // Required for Math.Ceiling

namespace FinalProject.ViewModels // Using the namespace from your uploaded file
{
    public class OrderListViewModel
    {
        // List to hold the paginated orders
        public List<Order> Orders { get; set; }

        // Current page number
        public int PageNumber { get; set; }

        // Number of items per page
        public int PageSize { get; set; }

        // Total number of items across all pages
        public int TotalItems { get; set; }

        // Calculated total number of pages
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        // You might add other properties here if needed for your view,
        // e.g., a filter string, sort order, etc.
    }
}