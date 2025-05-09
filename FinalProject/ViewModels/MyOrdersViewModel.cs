using System.Collections.Generic;

namespace FinalProject.ViewModels
{
    /// <summary>
    /// ViewModel for the entire My Orders page, containing a list of individual orders.
    /// </summary>
    public class MyOrdersViewModel
    {
        // Collection of individual order ViewModels for the logged-in member.
        public List<OrderViewModel> Orders { get; set; } = new List<OrderViewModel>();
    }
}