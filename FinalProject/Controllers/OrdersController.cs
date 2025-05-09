using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // Added for Include and ToListAsync
using FinalProject.Data; // Assuming your DbContext is here
using FinalProject.ViewModels; // Assuming your ViewModels are here
using System.Linq; // Added for LINQ
using System.Threading.Tasks; // Added for async/await
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Collections.Generic; // Added for List
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectList
using FinalProject.Models; // Added for ShoppingCartItem model
using System.Diagnostics; // Required for Debug.WriteLine
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using FinalProject.Configuration;
using FinalProject.Services;


namespace FinalProject.Controllers // Adjust namespace as needed
{
    [Authorize] // Ensure only authenticated users can access this controller's actions
    public class OrdersController : Controller // Renamed from OrderController to OrdersController based on user's provided code
    {
        private readonly ApplicationDbContext _context; // Replace ApplicationDbContext
        private readonly IEmailService _emailService;

        public OrdersController(ApplicationDbContext context,IEmailService emailService) // Replace ApplicationDbContext
        {
            _context = context;
            _emailService = emailService;

        }
        
        

        // Action to display the authenticated user's orders
        // Accessible via /Order/Profile
        public async Task<IActionResult> Profile()
        {
            // Get the MemberId from the authenticated user's claims
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            // Since the controller is [Authorize], memberIdClaim should not be null,
            // but we keep the check as a safeguard.
            if (memberIdClaim == null)
            {
                // Log this unexpected scenario
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account"); // Assuming Login is in AccountController
            }

            // Parse the MemberId from the claim
            if (!int.TryParse(memberIdClaim.Value, out int memberId))
            {
                // Handle cases where the claim value is not a valid integer
                // Log this error and potentially log the user out or show an error page
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account"); // Assuming Login is in AccountController
            }

            // Retrieve the orders for the member
            var orders = await _context.Orders
                .Where(order => order.MemberId == memberId)
                .Include(order => order.OrderItems) // Include OrderItems to get the count
                .OrderByDescending(order => order.OrderDate) // Optional: Order by date
                .ToListAsync();

            // Map the Order models to OrderViewModels
            var viewModelList = new List<OrderViewModel>(); // Create a list of ViewModels

            foreach (var order in orders)
            {
                viewModelList.Add(new OrderViewModel
                {
                    OrderId = order.OrderId,
                    MemberId = order.MemberId,
                    OrderDateDisplay = order.OrderDate.ToString("g"), // Example formatting (general date/time)
                    OrderStatus = order.OrderStatus,
                    TotalAmountDisplay = order.TotalAmount.ToString("C"), // Currency formatting
                    DiscountAppliedDisplay = order.DiscountApplied.ToString("P2"), // Percentage formatting
                    ClaimCode = order.ClaimCode, // ClaimCode is nullable in the model and ViewModel
                    ItemCount = order.OrderItems?.Count ?? 0 // Count items, handling null OrderItems collection
                });
            }

            // Pass the list of ViewModels to the view
            return View(viewModelList);
        }

        // GET: Orders
        // Optionally add [Authorize] here if only logged-in users should see their orders
        public async Task<IActionResult> Index()
        {
            // If you want to show only the logged-in user's orders:
            // if (User.Identity.IsAuthenticated)
            // {
            //     var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //     if (int.TryParse(memberIdString, out int memberId))
            //     {
            //         var memberOrders = await _context.Orders
            //             .Include(o => o.Member)
            //             .Where(o => o.MemberId == memberId)
            //             .ToListAsync();
            //         return View(memberOrders);
            //     }
            // }
            // // Fallback for non-authenticated or if MemberId is missing (maybe show nothing or a message)
            // // Or if this Index is for Admins, keep the original logic
            // var applicationDbContext = _context.Orders.Include(o => o.Member);
            // return View(await applicationDbContext.ToListAsync());

            // Keeping the original logic for now, assuming this Index might be for admins or show all
            var applicationDbContext = _context.Orders.Include(o => o.Member);
            return View(await applicationDbContext.ToListAsync());
        }
        //stepone ko barema
        public async Task<IActionResult> OrderStepOne()
    {
        // --- Step 1: Get the current user's ID from claims ---
        var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        // Basic check if user is authenticated and ID is available
        // [Authorize] should handle most cases, but this adds robustness.
        if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
        {
            // If MemberId is missing or invalid, sign out and redirect to login
             await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
             // Assuming "Login" action is in "Account" controller
             return RedirectToAction("Login", "Account");
        }

        // --- Step 2: Re-fetch the shopping cart items for the current user from the database ---
        // This is crucial for security and data integrity - do NOT trust client-side data.
        var cartItems = await _context.ShoppingCartItems
            .Where(item => item.MemberId == memberId) // Filter by the current user's ID
            .Include(item => item.Book) // Eagerly load the related Book entity
            .ThenInclude(book => book.Author) // Further eagerly load the related Author for each Book
            .ToListAsync(); // Execute the query and get results as a list

         // --- Step 3: Handle Empty Cart scenario ---
         // If the cart is empty at this point, redirect back to the cart page with a message.
         if (!cartItems.Any())
         {
             // Use TempData to pass a temporary message to the next request
             TempData["ErrorMessage"] = "Your shopping cart is empty. Please add items before proceeding to checkout.";
             // Redirect back to the shopping cart profile page
             return RedirectToAction("Profile", "ShoppingCartItems");
         }

        // --- Step 4: Prepare data for the view using ViewModel and calculate totals ---
        // We reuse the ShoppingCartItemViewModel as it contains the necessary display properties.
        var viewModelList = new List<ShoppingCartItemViewModel>();
        decimal initialTotalCartPrice = 0m; // Initialize total price BEFORE discounts
        int totalCartItems = 0; // Initialize total item count

        foreach (var item in cartItems)
        {
            // Populate the ViewModel for each cart item
            string bookAuthorName = item.Book?.Author?.FullName ?? "Unknown Author";
            string bookTitle = item.Book?.Title ?? "Unknown Title";
            // Format price as currency (adjust culture if needed)
            string bookListPriceDisplay = item.Book?.ListPrice.ToString("C") ?? "$0.00";
            // Calculate the total price for the current item (Quantity * ListPrice)
            decimal itemTotalPrice = item.Quantity * (item.Book?.ListPrice ?? 0m);
            // Format the item total price as currency
            string totalPriceDisplay = itemTotalPrice.ToString("C");
            // Format the date added (short date format)
            string dateAddedDisplay = item.DateAdded.ToString("d");

             viewModelList.Add(new ShoppingCartItemViewModel
            {
                CartItemId = item.CartItemId, // Include ID if needed, though not for review display
                MemberId = item.MemberId,
                BookId = item.BookId,
                Quantity = item.Quantity,
                DateAddedDisplay = dateAddedDisplay, // May or may not display this on review page
                BookTitle = bookTitle,
                BookAuthorName = bookAuthorName,
                BookListPriceDisplay = bookListPriceDisplay,
                TotalPriceDisplay = totalPriceDisplay,
                
            });

            // Accumulate the totals server-side (initial total before discounts)
            initialTotalCartPrice += itemTotalPrice; // Add item total to grand total price
            totalCartItems += item.Quantity; // Add item quantity to total item count
        }

        // --- Step 5: Apply Discounts ---

        decimal bulkDiscountPercentage = 0m;
        // Bulk Discount: 5% for 5 or more items
        if (totalCartItems >= 5)
        {
            bulkDiscountPercentage = 0.05m; // 5%
        }

        decimal loyaltyDiscountPercentage = 0m;
        // Loyalty Discount: 10% for every 10 successful orders
        // Assumption: A "successful order" is one that is NOT in "Pending" status.
        var successfulOrdersCount = await _context.Orders
            .Where(o => o.MemberId == memberId && o.OrderStatus != "Pending")
            .CountAsync();

        if (successfulOrdersCount > 0)
        {
            int loyaltyDiscountBatches = successfulOrdersCount / 10; // Integer division gives full batches of 10
            loyaltyDiscountPercentage = loyaltyDiscountBatches * 0.10m; // 10% per batch
        }

        // Total stackable discount (additive)
        decimal totalDiscountPercentage = bulkDiscountPercentage + loyaltyDiscountPercentage;

        // Ensure total discount doesn't exceed 100% (though unlikely with these rules)
        if (totalDiscountPercentage > 1.0m)
        {
            totalDiscountPercentage = 1.0m; // Cap discount at 100%
        }

        decimal totalDiscountAmount = initialTotalCartPrice * totalDiscountPercentage;

        // Calculate individual discount amounts for display in the view
        decimal bulkDiscountAmount = initialTotalCartPrice * bulkDiscountPercentage;
        decimal loyaltyDiscountAmount = initialTotalCartPrice * loyaltyDiscountPercentage;


        decimal finalTotalCartPrice = initialTotalCartPrice - totalDiscountAmount;

        // Ensure final price doesn't go below zero
         if (finalTotalCartPrice < 0m)
        {
            finalTotalCartPrice = 0m;
        }


        // --- Step 6: Pass data (ViewModel list, totals, and discounts) to the OrderStepOne view ---
        // Use ViewData to pass the calculated totals and discount information.
        ViewData["InitialTotalCartPrice"] = initialTotalCartPrice.ToString("C"); // Pass original total price
        ViewData["TotalCartItems"] = totalCartItems; // Pass total item count
        ViewData["BulkDiscountPercentage"] = bulkDiscountPercentage.ToString("P0"); // Pass formatted bulk discount %
        ViewData["LoyaltyDiscountPercentage"] = loyaltyDiscountPercentage.ToString("P0"); // Pass formatted loyalty discount %
        ViewData["TotalDiscountPercentage"] = totalDiscountPercentage.ToString("P0"); // Pass formatted total discount %
        ViewData["TotalDiscountAmount"] = totalDiscountAmount.ToString("C"); // Pass formatted total discount amount
        ViewData["BulkDiscountAmount"] = bulkDiscountAmount.ToString("C"); // Pass formatted bulk discount amount
        ViewData["LoyaltyDiscountAmount"] = loyaltyDiscountAmount.ToString("C"); // Pass formatted loyalty discount amount
        ViewData["FinalTotalCartPrice"] = finalTotalCartPrice.ToString("C"); // Pass formatted final total price


        // Return the OrderStepOne view, passing the list of ViewModels as the model.
        return View(viewModelList);
    }
            // POST: Orders/PlaceOrder
        // This method handles the "Place Order" button submission from the OrderStepOne view.
        // It creates a new Order, new OrderItems, and clears the shopping cart.
        // POST: Orders/PlaceOrder
// This method handles the "Place Order" button submission from the OrderStepOne view.
// It creates a new Order, new OrderItems, and clears the shopping cart.
[HttpPost]
[Authorize] // Ensures the user is authenticated
[ValidateAntiForgeryToken] // Protects against CSRF attacks
public async Task<IActionResult> PlaceOrder()
{
    // --- Step 1: Get the current user's ID ---
    var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
    {
        // If MemberId is missing or invalid, sign out and redirect to login
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account"); // Assuming Login action is in AccountController
    }

    // --- Step 2: Re-fetch shopping cart items (CRUCIAL for security and data integrity) ---
    // This ensures that the order is based on the current state of the cart from the database.
    var cartItems = await _context.ShoppingCartItems
        .Where(item => item.MemberId == memberId)
        .Include(item => item.Book) // Ensure Book is loaded for PriceAtPurchase and BookId
        .ToListAsync();

    // --- Step 3: Handle Empty Cart ---
    if (!cartItems.Any())
    {
        TempData["ErrorMessage"] = "Your shopping cart is empty or your session has expired. Please try again.";
        return RedirectToAction("Profile", "ShoppingCartItems"); // Redirect to cart page (assuming ShoppingCartItemsController has Profile action)
    }

    // --- Step 4: Re-calculate totals and discounts (Server-side calculation is key for accuracy) ---
    decimal initialTotalCartPrice = 0m;
    int totalCartItems = 0;
    foreach (var item in cartItems)
    {
        if (item.Book == null)
        {
            // Handle cases where a book might have been removed from the system
            TempData["ErrorMessage"] = "An item in your cart is no longer available. Please review your cart.";
            return RedirectToAction("Profile", "ShoppingCartItems");
        }
        initialTotalCartPrice += item.Quantity * item.Book.ListPrice;
        totalCartItems += item.Quantity;
    }

    // Recalculate Bulk Discount
    decimal bulkDiscountPercentage = (totalCartItems >= 5) ? 0.05m : 0m; // 5% for 5 or more items

    // Recalculate Loyalty Discount (based on member's past non-pending orders)
    var successfulOrdersCount = await _context.Orders
        .Where(o => o.MemberId == memberId && o.OrderStatus != "Pending")
        .CountAsync();
    decimal loyaltyDiscountPercentage = (successfulOrdersCount / 10) * 0.10m; // 10% for every 10 successful orders

    // Calculate Total Discount
    decimal totalDiscountPercentage = bulkDiscountPercentage + loyaltyDiscountPercentage;
    if (totalDiscountPercentage > 1.0m) totalDiscountPercentage = 1.0m; // Cap discount at 100%

    // Calculate Final Price
    decimal finalTotalCartPrice = initialTotalCartPrice * (1 - totalDiscountPercentage);
    if (finalTotalCartPrice < 0m) finalTotalCartPrice = 0m; // Ensure final price is not negative

    // --- Step 5: Create the Order and OrderItems within a Database Transaction ---
    // Using a transaction ensures that all database operations either complete successfully or none do.
    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
            // Create the main Order object
            // This will result in an INSERT into the 'Orders' table.
            var order = new Order
            {
                MemberId = memberId, // Associates the order with the current member
                OrderDate = DateTime.UtcNow, // Use UTC for consistency
                OrderStatus = "Pending", // Initial status, can be updated later (e.g., "Processing", "Shipped")
                TotalAmount = finalTotalCartPrice, // The final calculated price after all discounts
                DiscountApplied = totalDiscountPercentage, // Store the total discount percentage applied
                ClaimCode = null, // Can be set if a claim code is used
                DateAdded = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                OrderItems = new List<OrderItem>() // Initialize the collection for order items
            };

            // Create OrderItem records for each item in the shopping cart
            // This will result in INSERTs into the 'OrderItems' table.
            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    BookId = cartItem.BookId,
                    Quantity = cartItem.Quantity,
                    Order = order, // Set the Order navigation property
                    UnitPrice=cartItem.Book.ListPrice,
                    Book = cartItem.Book // Set the Book navigation property
                };
                order.OrderItems.Add(orderItem);
            }

            // Add the new Order (which includes its OrderItems) to the DbContext
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save the Order and its OrderItems to the database
            //member table ni update garau na
            var member = await _context.Members.FindAsync(memberId);
            if (member != null)
            {
                member.OrderCount++; // Increment the order count
                _context.Members.Update(member); // Mark the member entity as modified
                await _context.SaveChangesAsync(); // Save the changes to the member
            }
            member = await _context.Members.FindAsync(memberId); // Re-fetch member
            if (member == null || string.IsNullOrEmpty(member.Email))
            {
                TempData["WarningMessage"] = "Order placed, but email could not be sent (member or email missing).";
                // Log this warning
            }
            else
            {
                try
                {
                    string userEmail = member.Email;
                    string subject = $"Your Order Confirmation - Order #{order.OrderId}";
                    // Build the email body
                    string body = BuildOrderConfirmationEmailBody(order, cartItems, initialTotalCartPrice, totalDiscountPercentage, finalTotalCartPrice);

                    // Call the SendEmailAsync method on the injected email service
                    await _emailService.SendEmailAsync(userEmail, subject, body);

                    TempData["SuccessMessage"] = $"Order #{order.OrderId} placed successfully and confirmation email sent to {userEmail}.";
                }
                catch (Exception emailEx)
                {
                    TempData["WarningMessage"] = $"Order #{order.OrderId} placed successfully, but failed to send confirmation email. Please try again or contact support.";
                    // Log the exception
                }
            }
            // --- Step 6: Clear the shopping cart ---
            // This updates the 'ShoppingCartItems' table by removing the processed items.
            _context.ShoppingCartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync(); // Save changes to clear the cart

            // If all operations are successful, commit the transaction
            await transaction.CommitAsync();

            // --- Step 7: Redirect to an Order Confirmation page ---
            return Json(new { success = true, message = $"Order #{order.OrderId} placed successfully!", orderId = order.OrderId });


        }
        catch (Exception ex) // Consider logging the specific exception 'ex'
        {
            // If any error occurs during the process, roll back the transaction
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "There was an error placing your order. Please try again or contact support.";
            // Redirect back to the order review page or an error page
            return RedirectToAction("OrderStepOne");
        }
    }
}
// GET: /Orders/MyOrders or /myorders (depending on routing)
        // Displays a list of orders for the logged-in member using ViewModels.
        public async Task<IActionResult> MyOrders()
        {
            // Get the current user's ID
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                // If MemberId is missing or invalid, sign out and redirect to login
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account"); // Assuming Login action is in AccountController
            }

            // Fetch orders for the logged-in member
            // Include OrderItems and the associated Book and Author for each item
            var memberOrders = await _context.Orders
                .Where(o => o.MemberId == memberId)
                .Include(o => o.OrderItems) // Include the collection of order items
                    .ThenInclude(oi => oi.Book) // Then include the Book for each order item
                        .ThenInclude(b => b.Author) // Then include the Author for the Book
                .OrderByDescending(o => o.OrderDate) // Order by most recent first
                .ToListAsync();

            // Map the fetched data to the ViewModels
            var myOrdersViewModel = new MyOrdersViewModel();

            foreach (var order in memberOrders)
            {
                var orderViewModel = new OrderViewModel
                {
                    OrderId = order.OrderId,
                    MemberId = order.MemberId, // Mapping MemberId
                    OrderDateDisplay = order.OrderDate.ToLocalTime().ToString("g"), // Format date for display
                    OrderStatus = order.OrderStatus,
                    TotalAmountDisplay = order.TotalAmount.ToString("C"), // Format total amount as currency
                    DiscountAppliedDisplay = order.DiscountApplied.ToString("P2"), // Format discount as percentage
                    ClaimCode = order.ClaimCode, // Mapping ClaimCode
                    ItemCount = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0, // Calculate total item count
                    // OrderItems collection is not part of this OrderViewModel structure as per your definition
                    // If you need item details here, you would uncomment the List<OrderItemViewModel> property
                    // and map the items below.
                };

                // If you need to populate the OrderItems collection in OrderViewModel, uncomment the property
                // and the following mapping logic:
                // if (order.OrderItems != null)
                // {
                //     foreach (var item in order.OrderItems)
                //     {
                //         var orderItemViewModel = new OrderItemViewModel
                //         {
                //             OrderItemId = item.OrderItemId,
                //             OrderId = item.OrderId,
                //             BookId = item.BookId,
                //             BookTitle = item.Book?.Title ?? "N/A",
                //             BookAuthorName = item.Book?.Author?.Name ?? "N/A",
                //             Quantity = item.Quantity,
                //             UnitPriceDisplay = item.UnitPrice.ToString("C"),
                //             DiscountDisplay = item.Discount.ToString("P2"),
                //             TotalPriceDisplay = item.TotalPrice.ToString("C")
                //         };
                //         orderViewModel.OrderItems.Add(orderItemViewModel);
                //     }
                // }


                myOrdersViewModel.Orders.Add(orderViewModel);
            }

            // Pass the MyOrdersViewModel to the view
            return View(myOrdersViewModel);
        }

        // GET: /Orders/OrderDetail/{id}
        // Displays the details of a specific order for the logged-in member using ViewModels.
        // --- Action to display details of a specific order ---
        // Accessible via /Orders/OrderDetail/{id}
       // --- Action to display details of a specific order ---
        // Accessible via /Orders/OrderDetail/{id}
        public async Task<IActionResult> OrderDetail(int? id)
        {
            // 1. Validate input ID
            if (id == null)
            {
                return NotFound("Order ID is required.");
            }

            // 2. Get the current user's MemberId from claims
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account"); // Assuming Login action is in an AccountController
            }

            // 3. Fetch the specific order for the logged-in member from the database
            // Include related entities: OrderItems, and for each OrderItem, its Book and the Book's Author.
            var order = await _context.Orders
                .Where(o => o.OrderId == id && o.MemberId == memberId) // Security: Ensure order belongs to the current member
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book) // OrderItem has a navigation property 'Book'
                        .ThenInclude(b => b.Author) // Book has a navigation property 'Author'
                .FirstOrDefaultAsync();

            // 4. Handle cases where the order is not found or doesn't belong to the member
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found or you do not have permission to view it.");
            }

            // 5. Map the fetched order data to your user-provided OrderViewModel
            var orderViewModel = new OrderViewModel
            {
                OrderId = order.OrderId,
                MemberId = order.MemberId,
                OrderDateDisplay = order.OrderDate.ToLocalTime().ToString("g"),
                OrderStatus = order.OrderStatus,
                ClaimCode = order.ClaimCode,

                ItemCount = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,

                // Map each OrderItem entity to the user-provided OrderItemViewModel
                OrderItems = order.OrderItems?.Select(oi => new OrderItemViewModel
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    BookId = oi.BookId,
                    BookTitle = oi.Book?.Title ?? "N/A",
                    Quantity = oi.Quantity,

                    // - Setting the required 'BookAuthorName' property in the ViewModel
                    // - Assuming your 'Author' model has a property named 'AuthorName'
                    BookAuthorName = oi.Book?.Author?.FullName ?? "N/A", // *** Adjusted property name based on error ***

                    UnitPriceDisplay = oi.UnitPrice.ToString("C"),
                    DiscountDisplay = oi.Discount.ToString("P2"),
                    TotalPriceDisplay = oi.TotalPrice.ToString("C")
                }).ToList() ?? new List<OrderItemViewModel>(),

                DiscountAppliedDisplay = order.DiscountApplied.ToString("P2"),
                TotalAmountDisplay = order.TotalAmount.ToString("C")
            };

            // 6. Pass the populated OrderViewModel to the view
            return View(orderViewModel);
        }

     


        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Member)
                .Include(o => o.OrderItems) // Include OrderItems to show items in the order
                    .ThenInclude(oi => oi.Book) // Include Book details for each OrderItem
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Optional: Add authorization check to ensure logged-in user owns this order
            // if (User.Identity.IsAuthenticated)
            // {
            //     var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //     if (int.TryParse(memberIdString, out int memberId))
            //     {
            //         if (order.MemberId != memberId)
            //         {
            //             return Forbid(); // Or challenge, or redirect to an access denied page
            //         }
            //     }
            // }


            return View(order);
        }

        // GET: Orders/Create
        // You might want to restrict who can create orders directly
        [Authorize] // Only authorized users can initiate order creation
        public IActionResult Create()
        {
            // In a real scenario, you'd likely associate this with the logged-in user
            // ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "MemberId"); // This might not be needed if linking to current user
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authorized users can post to create
        public async Task<IActionResult> Create([Bind("OrderId,OrderDate,OrderStatus,TotalAmount,DiscountApplied,ClaimCode,DateAdded,DateUpdated")] Order order)
        {
            // In a real application, you would likely get the MemberId from the authenticated user's claims
            // and assign it to the order. The Bind attribute is adjusted to exclude MemberId
            // since it should come from the authenticated user, not the form.

            // Get the authenticated user's MemberId
            var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memberIdString) || !int.TryParse(memberIdString, out int memberId))
            {
                // Authentication failed or MemberId claim missing/invalid
                TempData["Message"] = "Authentication failed. Please log in again.";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Member");
            }
            order.MemberId = memberId; // Assign the logged-in member's ID

            // Set DateAdded and DateUpdated
            order.DateAdded = DateTime.UtcNow;
            order.DateUpdated = DateTime.UtcNow;

            // Note: Discount and TotalAmount calculation should ideally happen based on OrderItems
            // added *after* the order is created, or in a separate checkout process.
            // This Create action as is, doesn't handle OrderItems.

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = order.OrderId }); // Redirect to details of the created order
            }
            // If model state is invalid, return the view with the order model
            return View(order);
        }

        // GET: Orders/Edit/5
        [Authorize] // Only authorized users can edit orders
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            // Ensure the logged-in user is authorized to edit this specific order
             if (User.Identity.IsAuthenticated)
             {
                 var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                 if (int.TryParse(memberIdString, out int memberId))
                 {
                     if (order.MemberId != memberId)
                     {
                         return Forbid(); // Prevent editing orders that don't belong to the user
                     }
                 }
                 else
                 {
                     // Authenticated but MemberId claim missing/invalid - treat as unauthorized
                     return Forbid();
                 }
             }
             else
             {
                 // Not authenticated, but action requires [Authorize] - this case should ideally
                 // be handled by the [Authorize] attribute itself redirecting to login.
                 // Including this check defensively.
                 return Unauthorized();
             }

            // ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "MemberId", order.MemberId); // Not needed if MemberId is fixed by user
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authorized users can post to edit
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,OrderDate,OrderStatus,TotalAmount,DiscountApplied,ClaimCode,DateAdded,DateUpdated")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            // Retrieve the existing order to check ownership and populate MemberId
            var existingOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == id);
            if (existingOrder == null)
            {
                return NotFound();
            }

            // Ensure the logged-in user is authorized to edit this specific order
            if (User.Identity.IsAuthenticated)
            {
                var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(memberIdString, out int memberId))
                {
                    if (existingOrder.MemberId != memberId)
                    {
                        return Forbid(); // Prevent editing orders that don't belong to the user
                    }
                    // Assign the correct MemberId from the existing order to the bound model
                    order.MemberId = existingOrder.MemberId;
                }
                else
                {
                    // Authenticated but MemberId claim missing/invalid - treat as unauthorized
                    return Forbid();
                }
            }
            else
            {
                 // Not authenticated, but action requires [Authorize]
                 return Unauthorized();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // Set DateUpdated
                    order.DateUpdated = DateTime.UtcNow;
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = order.OrderId }); // Redirect to details after edit
            }
            // ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "MemberId", order.MemberId); // Not needed
            return View(order);
        }

        // GET: Orders/Delete/5
        [Authorize] // Only authorized users can delete orders
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Member)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }
            // Ensure the logged-in user is authorized to delete this specific order
             if (User.Identity.IsAuthenticated)
             {
                 var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                 if (int.TryParse(memberIdString, out int memberId))
                 {
                     if (order.MemberId != memberId)
                     {
                         return Forbid(); // Prevent deleting orders that don't belong to the user
                     }
                 }
                 else
                 {
                     // Authenticated but MemberId claim missing/invalid - treat as unauthorized
                     return Forbid();
                 }
             }
             else
             {
                 // Not authenticated, but action requires [Authorize]
                 return Unauthorized();
             }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authorized users can post to delete
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order != null)
            {
                 // Add a check here to ensure the logged-in user owns the order before deleting
                 if (User.Identity.IsAuthenticated)
                 {
                     var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                     if (int.TryParse(memberIdString, out int memberId))
                     {
                         if (order.MemberId != memberId)
                         {
                              return Forbid(); // Prevent deleting orders that don't belong to the user
                         }
                     }
                     else
                     {
                         // Authenticated but MemberId claim missing/invalid - treat as unauthorized
                         return Forbid();
                     }
                 }
                 else
                 {
                     // Not authenticated, but action requires [Authorize]
                     return Unauthorized();
                 }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }
        //helper function to send email
        // --- Helper method to build the email body (the bill) ---
// This method takes the necessary data and formats it into an HTML string
private string BuildOrderConfirmationEmailBody(Order order, List<ShoppingCartItem> cartItems, decimal initialTotal, decimal totalDiscountPercentage, decimal finalTotal)
{
    // Use StringBuilder for efficient string concatenation, especially for longer content
    StringBuilder bodyBuilder = new StringBuilder();

    // Add the main heading and introductory text
    bodyBuilder.AppendLine("<h1>Order Confirmation</h1>");
    bodyBuilder.AppendLine($"<p>Thank you for your order! Your order details are below:</p>");
    bodyBuilder.AppendLine($"<p><strong>Order ID:</strong> {order.OrderId}</p>");
    // Format the date for better readability in the email
    bodyBuilder.AppendLine($"<p><strong>Order Date:</strong> {order.OrderDate.ToLocalTime().ToString("g")}</p>"); // Display in local time
    bodyBuilder.AppendLine("<hr>"); // Add a horizontal rule for separation

    // Add a section for the order summary
    bodyBuilder.AppendLine("<h2>Order Summary</h2>");

    // Create an HTML table to display the order items like a bill
    bodyBuilder.AppendLine("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>"); // Added style for better table appearance and full width
    bodyBuilder.AppendLine("<thead><tr><th>Item</th><th>Quantity</th><th>Unit Price</th><th>Line Total</th></tr></thead>"); // Table header row
    bodyBuilder.AppendLine("<tbody>"); // Start the table body

    // Loop through each item that was in the shopping cart for this order
    foreach (var item in cartItems)
    {
        // Ensure Book is not null before accessing properties (safety check)
        if (item.Book != null)
        {
            // Calculate the total price for this specific item line
            decimal lineTotal = item.Quantity * item.Book.ListPrice;

            // Add a table row for each item
            bodyBuilder.AppendLine($"<tr>");
            bodyBuilder.AppendLine($"<td>{item.Book.Title}</td>"); // Display book title
            bodyBuilder.AppendLine($"<td>{item.Quantity}</td>"); // Display quantity
            // Format the unit price as currency
            bodyBuilder.AppendLine($"<td>{item.Book.ListPrice.ToString("C")}</td>");
            // Format the line total as currency
            bodyBuilder.AppendLine($"<td>{lineTotal.ToString("C")}</td>");
            bodyBuilder.AppendLine($"</tr>");
        }
    }

    bodyBuilder.AppendLine("</tbody>"); // End the table body
    bodyBuilder.AppendLine("</table>"); // End the table

    bodyBuilder.AppendLine("<hr>"); // Add another horizontal rule

    // Add sections for totals and discounts
    bodyBuilder.AppendLine($"<p><strong>Subtotal:</strong> {initialTotal.ToString("C")}</p>"); // Display the total before discounts
    // Calculate the actual discount amount for display
    decimal totalDiscountAmount = initialTotal * totalDiscountPercentage;
    // Display the total discount applied, showing the percentage and the amount
    bodyBuilder.AppendLine($"<p><strong>Total Discount Applied ({totalDiscountPercentage:P0}):</strong> -{totalDiscountAmount.ToString("C")}</p>");
    bodyBuilder.AppendLine($"<p><strong>Final Total:</strong> {finalTotal.ToString("C")}</p>"); // Display the final total after discounts

    bodyBuilder.AppendLine("<hr>"); // Final horizontal rule
    bodyBuilder.AppendLine("<p>If you have any questions, please contact us.</p>");
    bodyBuilder.AppendLine("<p>Thank you!</p>");

    // Return the complete HTML string
    return bodyBuilder.ToString();
}
// --- End Helper method to build the email body ---



        // --- Action to Add a Single Item and Create a New Order ---
        [HttpPost] // Use POST for actions that change data
        [Authorize] // Requires the user to be authenticated
        [ValidateAntiForgeryToken] // Good practice for POST requests from views
        public async Task<IActionResult> AddSingleItemOrder(int bookId, int quantity = 1) // Default quantity to 1
        {
            // Get the authenticated user's MemberId from the claims.
            var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Determine the return URL.
            var returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action(nameof(Index), "Book"); // Default to Book Index
            }

            // Validate MemberId from claims.
            if (string.IsNullOrEmpty(memberIdString) || !int.TryParse(memberIdString, out int memberId))
            {
                TempData["Message"] = "Authentication failed. Please log in again.";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Member");
            }

            // Find the member in the database.
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                TempData["Message"] = "Error: Your member account data could not be found in the database.";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Member");
            }

            // Validate the bookId and check if the book exists.
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
            {
                TempData["Message"] = $"Error: Book with ID {bookId} not found.";
                return Redirect(returnUrl);
            }

            // Ensure quantity is at least 1.
            if (quantity < 1)
            {
                quantity = 1;
            }

            // --- Calculate Member Discount ---
            decimal discountPercentage = await CalculateMemberDiscount(memberId);
            Debug.WriteLine($"Member ID {memberId} qualifies for {discountPercentage * 100}% discount.");
            // --- End Calculate Member Discount ---


            // --- Create a NEW Order for this item ---
            var newOrder = new Order
            {
                MemberId = member.MemberId,
                Member = member, // Assign the fetched Member entity
                OrderDate = DateTime.UtcNow, // Use UTC for consistency
                OrderStatus = "Placed", // Set status to indicate a direct order (e.g., "Placed", "Completed")
                ClaimCode = null, // No claim code initially
                DateAdded = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                OrderItems = new List<OrderItem>() // Initialize the collection
            };

            // --- Create the OrderItem for the book ---
            var newOrderItem = new OrderItem
            {
                // OrderId is set implicitly when adding to the Order's collection and saving
                Order = newOrder, // Assign the new Order object to the navigation property
                BookId = book.BookId,
                Book = book, // Assign the fetched Book entity
                Quantity = quantity,
                UnitPrice = book.ListPrice, // Use ListPrice from the Book
                Discount = book.SaleDiscount ?? 0 // Use SaleDiscount from the Book, handle null
            };

            // Add the OrderItem to the new Order's collection
            newOrder.OrderItems.Add(newOrderItem);

            // Calculate the TotalAmount for the new order (based on the single item) BEFORE member discount
            decimal subtotal = (newOrderItem.Quantity * newOrderItem.UnitPrice) - newOrderItem.Discount;
            newOrder.TotalAmount = subtotal; // Start with subtotal

            // Apply the member-level discount
            newOrder.DiscountApplied = newOrder.TotalAmount * discountPercentage;
            newOrder.TotalAmount -= newOrder.DiscountApplied;

            // Add the new Order and its OrderItem to the context
            _context.Orders.Add(newOrder);
            // EF Core will automatically add the newOrderItem because it's part of the newOrder's collection

            // Save changes to the database (creates the new Order and OrderItem)
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Order placed for {book.Title}. A discount of {newOrder.DiscountApplied:C} was applied."; // Display applied discount

            // Redirect to the details page of the newly created order
            return RedirectToAction(nameof(Details), new { id = newOrder.OrderId });
            // Alternatively, redirect to an order confirmation page or the user's order history
        }
        // --- End AddSingleItemOrder Action ---


        // --- Original AddItemToOrder (likely used for "Add to Cart") ---
        // Keeping this action as it seems to be intended for adding to a pending cart
        [HttpPost] // This action will typically be called via a POST request (e.g., from a form or AJAX)
        [Authorize] // Ensures only logged-in users can add items
        [ValidateAntiForgeryToken] // Good practice for POST requests
        public async Task<IActionResult> AddItemToOrder(int bookId, int quantity = 1)
        {
            // 1. Get the MemberId of the currently logged-in user
            var (memberId, authError) = GetMemberIdFromClaims();
            if (authError != null)
            {
                return authError;
            }

            // 2. Find the book to be added
            var book = await FindBookByIdAsync(bookId);
            if (book == null)
            {
                return NotFound($"Book with ID {bookId} not found.");
            }

            // 3. Find or create the user's current active order (Pending status)
            var order = await FindOrCreatePendingOrderAsync(memberId);

            // 4. Add or update the order item for the book within the order
            AddOrUpdateOrderItem(order, book, quantity);

            // 5. Calculate and apply member discount and update order totals
            // Assuming CalculateMemberDiscount is a separate helper or service method
            decimal memberDiscountPercentage = await CalculateMemberDiscountAsync(memberId);
            UpdateOrderTotals(order, memberDiscountPercentage);

            // 6. Save all changes to the database (Order and OrderItems)
            await _context.SaveChangesAsync();

            // 7. Redirect to the order details page (cart view)
            TempData["Message"] = $"{book.Title} added to your cart. Member discount applied.";
            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }

        /// <summary>
        /// Extracts the MemberId from the current user's claims.
        /// </summary>
        /// <returns>A tuple containing the member ID and an IActionResult if authentication fails.</returns>
        private (int memberId, IActionResult? errorResult) GetMemberIdFromClaims()
        {
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (memberIdClaim == null)
            {
                return (0, Unauthorized("Could not identify the logged-in member."));
            }

            if (!int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return (0, Unauthorized("Invalid member identifier format."));
            }

            return (memberId, null);
        }

        /// <summary>
        /// Finds a book by its ID asynchronously.
        /// </summary>
        /// <param name="bookId">The ID of the book.</param>
        /// <returns>The Book entity or null if not found.</returns>
        private async Task<Book?> FindBookByIdAsync(int bookId)
        {
             return await _context.Books.FindAsync(bookId);
        }

        /// <summary>
        /// Finds an existing pending order for the member or creates a new one asynchronously.
        /// Includes OrderItems for checking existing items.
        /// </summary>
        /// <param name="memberId">The ID of the member.</param>
        /// <returns>The existing or newly created Order entity.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Member entity cannot be found for the authenticated user.</exception>
        private async Task<Order> FindOrCreatePendingOrderAsync(int memberId)
        {
            // Find the user's current active order with status "Pending" (acting as the cart)
            var order = await _context.Orders
                .Include(o => o.OrderItems) // Include OrderItems to check existing items efficiently
                .FirstOrDefaultAsync(o => o.MemberId == memberId && o.OrderStatus == "Pending");

            if (order == null)
            {
                // No active order found, create a new one
                // Fetch the Member entity to satisfy the required navigation property
                var member = await _context.Members.FindAsync(memberId);
                 if (member == null)
                {
                    // This is a critical error if MemberId is from an authenticated user claim
                    throw new InvalidOperationException("Member entity not found for authenticated user.");
                }

                order = new Order
                {
                    MemberId = memberId,
                    Member = member, // Assign the fetched Member entity
                    OrderDate = DateTime.UtcNow, // Use UTC for consistency
                    OrderStatus = "Pending", // Set initial status for a new cart
                    TotalAmount = 0, // Will be calculated based on items
                    DiscountApplied = 0, // Will be calculated based on items and member discount
                    ClaimCode = null, // Or generate a default claim code if needed
                    DateAdded = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    OrderItems = new List<OrderItem>() // Initialize the collection for new items
                };
                _context.Orders.Add(order); // Add the new order to the context
                // No need to SaveChangesAsync here, it will be saved with the OrderItem changes
            }

            return order;
        }
        

        /// <summary>
        /// Adds a new OrderItem to the order or updates the quantity if the book already exists.
        /// Modifies the Order entity's OrderItems collection directly.
        /// </summary>
        /// <param name="order">The Order entity to add/update the item in.</param>
        /// <param name="book">The Book entity to add/update.</param>
        /// <param name="quantity">The quantity to add (defaults to 1).</param>
        private void AddOrUpdateOrderItem(Order order, Book book, int quantity)
        {
            // Ensure quantity is at least 1
            if (quantity < 1)
            {
                quantity = 1;
            }

            // Check if the book is already in the order's items
            var existingOrderItem = order.OrderItems.FirstOrDefault(oi => oi.BookId == book.BookId);

            if (existingOrderItem != null)
            {
                // Book is already in the cart, update the quantity and price details
                existingOrderItem.Quantity += quantity;
                existingOrderItem.Discount = book.SaleDiscount ?? 0; // Use SaleDiscount, handle null
                existingOrderItem.UnitPrice = book.ListPrice; // Use ListPrice
                // EF Core tracks changes to entities already attached to the context,
                // so explicit _context.OrderItems.Update() is often not needed here.
            }
            else
            {
                // Book is not in the cart, add a new OrderItem
                var newOrderItem = new OrderItem
                {
                    // OrderId and Order navigation property will be set automatically by EF Core
                    // when adding to the Order.OrderItems collection of a tracked entity (the 'order').
                    // Explicitly setting them here is also fine for clarity.
                    OrderId = order.OrderId,
                    Order = order,
                    BookId = book.BookId,
                    Book = book,
                    Quantity = quantity,
                    UnitPrice = book.ListPrice, // Use ListPrice at the time of adding to cart
                    Discount = book.SaleDiscount ?? 0 // Use SaleDiscount at the time of adding
                };
                order.OrderItems.Add(newOrderItem); // Add to the order's collection
                // EF Core tracks changes when adding to a tracked entity's collection,
                // so explicit _context.OrderItems.Add() is often not needed here.
            }
        }

        /// <summary>
        /// Calculates the subtotal based on current order items, applies the member discount,
        /// and updates the order's total amount and date updated.
        /// Modifies the Order entity in place.
        /// </summary>
        /// <param name="order">The Order entity to update totals for.</param>
        /// <param name="memberDiscountPercentage">The percentage discount applicable to the member (e.g., 0.10 for 10%).</param>
        private void UpdateOrderTotals(Order order, decimal memberDiscountPercentage)
        {
             // Recalculate subtotal based on current order items
             // Item total = Quantity * UnitPrice * (1 - ItemDiscount)
            decimal subtotal = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice * (1 - oi.Discount));

            // Apply the member-level discount to the subtotal
            order.DiscountApplied = subtotal * memberDiscountPercentage;
            order.TotalAmount = subtotal - order.DiscountApplied;

            order.DateUpdated = DateTime.UtcNow;

            // EF Core tracks changes to entities already attached to the context,
            // so explicit _context.Orders.Update() is often not needed here.
        }


        // Assuming this helper method exists elsewhere in your controller or a service
        /// <summary>
        /// Calculates the member-specific discount percentage asynchronously.
        /// This is a placeholder implementation.
        /// </summary>
        /// <param name="memberId">The ID of the member.</param>
        /// <returns>The discount percentage (e.g., 0.10 for 10%).</returns>
        private async Task<decimal> CalculateMemberDiscountAsync(int memberId)
        {
            // Placeholder implementation - replace with your actual logic
            // e.g., fetch member tier from database and return corresponding discount
            var member = await _context.Members.FindAsync(memberId);
            if (member != null)
            {
                // Example: Members with ID > 10 get 10% discount
                return member.MemberId > 10 ? 0.10M : 0.00M;
            }
            return 0.00M; // No discount if member not found
        }


        // You would also need a Details action to view the order/cart
        /// <summary>
        /// Displays the details of a specific order (cart).
        /// Includes OrderItems and their associated Books.
        /// </summary>
        /// <param name="id">The ID of the order to display.</param>
        /// <returns>The View for the order details or a NotFound/Unauthorized result.</returns>
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book) // Include Book details for each item
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Optional: Check if the logged-in user owns this order for security
            var (memberId, authError) = GetMemberIdFromClaims();
            if (authError != null || order.MemberId != memberId)
            {
                 return Unauthorized("You are not authorized to view this order.");
            }

            return View(order); // Return the order to a view
        } // --- End AddItemToOrder Action ---


        // --- Action to Cancel a Specific Order Item ---
        [HttpPost] // Use POST as this action modifies data
        [Authorize] // Only authenticated users can cancel items
        [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery
        public async Task<IActionResult> CancelOrderItem(int orderItemId)
        {
            Debug.WriteLine($"CancelOrderItem action called with orderItemId: {orderItemId}"); // Debugging line

            // Determine the return URL - likely the page the request came from (Book Details page)
            var returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                // Fallback URL if Referer is missing or not local
                returnUrl = Url.Action(nameof(Index), "Book");
            }
             Debug.WriteLine($"Return URL: {returnUrl}"); // Debugging line

            // Get the authenticated user's MemberId
            var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memberIdString) || !int.TryParse(memberIdString, out int memberId))
            {
                Debug.WriteLine("Authentication failed or MemberId claim missing/invalid."); // Debugging line
                TempData["Message"] = "Authentication failed. Please log in again.";
                // Optionally redirect to login if authentication fails
                // return RedirectToAction("Login", "Account"); // Adjust controller/action as needed
                return Redirect(returnUrl); // Or just redirect back with the message
            }
             Debug.WriteLine($"Logged-in MemberId: {memberId}"); // Debugging line


            // Find the OrderItem, including its parent Order to check ownership and status
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order) // Include the parent Order to check MemberId and Status
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            Debug.WriteLine($"Attempting to find OrderItem with ID: {orderItemId}"); // Debugging line
            if (orderItem == null)
            {
                Debug.WriteLine($"OrderItem with ID {orderItemId} not found."); // Debugging line
                TempData["Message"] = "Error: Order item not found.";
                return Redirect(returnUrl);
            }
             Debug.WriteLine($"OrderItem found. OrderItem ID: {orderItem.OrderItemId}, Parent Order ID: {orderItem.OrderId}"); // Debugging line


            // --- Validation Checks ---
            // Ensure the order item belongs to the logged-in user
            if (orderItem.Order.MemberId != memberId)
            {
                Debug.WriteLine($"Unauthorized attempt: Order belongs to MemberId {orderItem.Order.MemberId}, logged-in MemberId is {memberId}"); // Debugging line
                TempData["Message"] = "Error: You do not have permission to cancel this item.";
                // Log this attempt for security reasons
                // _logger.LogWarning($"Unauthorized attempt to cancel OrderItem {orderItemId} by MemberId {memberId}");
                return Redirect(returnUrl);
            }
             Debug.WriteLine("Ownership check passed."); // Debugging line


            // Define which statuses are cancellable. Adjust these based on your business logic.
            // Assuming "Pending" (cart) and "Placed" (recent order) are cancellable
            var cancellableStatuses = new[] { "Pending", "Placed" };
             Debug.WriteLine($"Cancellable statuses: {string.Join(", ", cancellableStatuses)}"); // Debugging line


            // Check if the order is in a cancellable status
            if (!cancellableStatuses.Contains(orderItem.Order.OrderStatus))
            {
                Debug.WriteLine($"Order status '{orderItem.Order.OrderStatus}' is not cancellable."); // Debugging line
                TempData["Message"] = $"Error: Order item is not cancellable (Status: {orderItem.Order.OrderStatus}).";
                return Redirect(returnUrl);
            }
             Debug.WriteLine($"Order status '{orderItem.Order.OrderStatus}' is cancellable. Proceeding with cancellation."); // Debugging line
            // --- End Validation Checks ---


            // --- Perform Cancellation ---
            try
            {
                 Debug.WriteLine($"Removing OrderItem {orderItem.OrderItemId} from context."); // Debugging line
                // Remove the order item from the context
                _context.OrderItems.Remove(orderItem);

                // Recalculate the total for the parent order
                 // Ensure the OrderItems collection is loaded for recalculation
                 await _context.Entry(orderItem.Order).Collection(o => o.OrderItems).LoadAsync();

                 Debug.WriteLine($"Recalculating total for Order {orderItem.OrderId}. Current items count (before save): {orderItem.Order.OrderItems.Count}"); // Debugging line

                 // Calculate subtotal from remaining items
                 decimal subtotal = orderItem.Order.OrderItems
                    .Where(oi => oi.OrderItemId != orderItemId) // Exclude the item being removed from calculation
                    .Sum(oi => (oi.Quantity * oi.UnitPrice) - oi.Discount);

                // --- Recalculate Member Discount for the updated order ---
                // Fetch the member ID from the order itself
                int orderMemberId = orderItem.Order.MemberId;
                decimal memberDiscountPercentage = await CalculateMemberDiscount(orderMemberId);
                Debug.WriteLine($"Recalculating member discount for Order {orderItem.OrderId} (Member ID {orderMemberId}). Qualifies for {memberDiscountPercentage * 100}% discount.");
                // --- End Recalculate Member Discount ---

                // Apply the member-level discount to the new subtotal
                orderItem.Order.DiscountApplied = subtotal * memberDiscountPercentage;
                orderItem.Order.TotalAmount = subtotal - orderItem.Order.DiscountApplied;


                 Debug.WriteLine($"New TotalAmount for Order {orderItem.Order.TotalAmount} for Order {orderItem.OrderId}"); // Debugging line


                // If the order has no remaining items, change its status to "Cancelled" or "Empty"
                if (orderItem.Order.OrderItems.Count == 0)
                {
                    Debug.WriteLine($"Order {orderItem.OrderId} now has 0 items. Changing status."); // Debugging line
                    orderItem.Order.OrderStatus = "Cancelled"; // Or "Empty", depending on your desired status
                    orderItem.Order.TotalAmount = 0; // Ensure total is 0
                    orderItem.Order.DiscountApplied = 0; // Ensure discount is 0
                     _context.Orders.Update(orderItem.Order); // Mark the Order as updated
                } else {
                     _context.Orders.Update(orderItem.Order); // Mark the Order as updated if items remain
                }


                // Save changes to the database (removes the OrderItem and updates the Order)
                 Debug.WriteLine("Saving changes to database."); // Debugging line
                await _context.SaveChangesAsync();
                 Debug.WriteLine("Changes saved successfully."); // Debugging line


                TempData["Message"] = "Order item cancelled successfully.";
            }
            catch (Exception ex)
            {
                // Log the exception
                Debug.WriteLine($"Error cancelling order item {orderItemId}: {ex.Message}"); // Debugging line
                // _logger.LogError(ex, $"Error cancelling order item {orderItemId}");
                TempData["Message"] = "Error cancelling order item. Please try again.";
                // Consider more specific error handling based on exception type
            }
            // --- End Perform Cancellation ---


            // Redirect back to the page the request came from
            return Redirect(returnUrl);
            // Alternatively, redirect to the order details page or user's order history
            // return RedirectToAction(nameof(Details), new { id = orderItem.OrderId });
        }
        // --- End CancelOrderItem Action ---


        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }

        // Placeholder for member discount calculation logic
        // You would implement this based on your business rules (e.g., membership level, purchase history)
        private Task<decimal> CalculateMemberDiscount(int memberId)
        {
            // Example: Fetch member and apply a discount based on a property
            var member = _context.Members.Find(memberId);
            if (member != null)
            {
                // Assuming Member model has a StackableDiscount property (from your ProfileViewModel)
                return Task.FromResult(member.StackableDiscount / 100m); // Convert percentage (e.g., 5.00) to decimal (e.g., 0.05)
            }
            return Task.FromResult(0m); // No discount if member not found
        }

        // Assuming you have a Login action in an AccountController
        // private IActionResult Login()
        // {
        //     // Redirect to your login page
        //     return RedirectToAction("Login", "Account");
        // }
        
    }
}
