using System.ComponentModel.DataAnnotations;

namespace FinalProject.ViewModels
{
    public class OrderStepOneViewModel
    {
        public int TotalCartItems { get; set; }
        public decimal TotalCartPrice { get; set; }

        // Optional: Include a summary list of items if you want to display them
        // on the checkout page. You might use a simpler model than the full cart item view model.
        public List<OrderSummaryItemViewModel> SummaryItems { get; set; }

        // Properties for Shipping Address (you'll likely want more)
        [Required]
        [Display(Name = "Full Name")]
        public string ShippingFullName { get; set; }

        [Required]
        [Display(Name = "Address Line 1")]
        public string ShippingAddressLine1 { get; set; }

         [Display(Name = "Address Line 2")]
        public string ShippingAddressLine2 { get; set; }

        [Required]
        [Display(Name = "City")]
        public string ShippingCity { get; set; }

        [Required]
        [Display(Name = "State/Province")]
        public string ShippingStateProvince { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string ShippingPostalCode { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string ShippingCountry { get; set; }

        // Properties for Billing Address (can be same as shipping)
        public bool BillingSameAsShipping { get; set; } = true; // Default to true

        // Add billing address properties if not same as shipping (consider a separate AddressViewModel)
        public string BillingFullName { get; set; }
        public string BillingAddressLine1 { get; set; }
        public string BillingAddressLine2 { get; set; }
        public string BillingCity { get; set; }
        public string BillingStateProvince { get; set; }
        public string BillingPostalCode { get; set; }
        public string BillingCountry { get; set; }

        // You might also want contact info
        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [Required]
        [Phone]
        public string ContactPhone { get; set; }

        // Add other necessary fields for the first step of ordering
    }

    // Optional: A simpler ViewModel for displaying items on the summary page
    public class OrderSummaryItemViewModel
    {
        public string BookTitle { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Price per item
        public decimal TotalPrice { get; set; } // Price for Quantity * UnitPrice
    }
}