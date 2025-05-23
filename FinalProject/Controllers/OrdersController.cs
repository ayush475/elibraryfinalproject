using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // Added for Include and ToListAsync
using FinalProject.Data; // Assuming your DbContext is here
using FinalProject.ViewModels; // Assuming your ViewModels are here
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using FinalProject.Models; // Added for ShoppingCartItem model
using System.Diagnostics; // Required for Debug.WriteLine
using System.Text;
using FinalProject.Services;
using Microsoft.EntityFrameworkCore.Storage;


namespace FinalProject.Controllers // Adjust namespace as needed
{
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
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            // Ensure pageNumber is at least 1
            pageNumber = Math.Max(1, pageNumber);
            // Ensure pageSize is within a reasonable range (optional, but good practice)
            pageSize = Math.Max(1, pageSize); // Minimum page size of 1
            pageSize = Math.Min(50, pageSize); // Maximum page size of 50 (adjust as needed)


            // Get the total number of items
            // Ensure this count is performed BEFORE Skip and Take
            var totalItems = await _context.Orders.CountAsync();

            // Calculate the number of items to skip
            var skipCount = (pageNumber - 1) * pageSize;

            // Retrieve the paginated data
            var orders = await _context.Orders
                .Include(o => o.Member) // Include related Member data as in your original code
                .OrderBy(o => o.OrderId) // Add an OrderBy clause for consistent pagination (replace OrderId with a suitable column if needed)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync();

            // Create the ViewModel to pass data and pagination info to the view
            var viewModel = new OrderListViewModel // Assuming you have a ViewModel named OrderListViewModel
            {
                Orders = orders,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
                // TotalPages is calculated in the ViewModel based on TotalItems and PageSize
            };

            // Pass the ViewModel to the view
            return View(viewModel);
        }


        //stepone ko barema

public async Task<IActionResult> OrderStepOne()
{
    // --- Step 1: Get the current user's ID from claims ---
    var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

    if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    // --- Step 2: Re-fetch the shopping cart items for the current user from the database ---
    var cartItems = await _context.ShoppingCartItems
        .Where(item => item.MemberId == memberId)
        .Include(item => item.Book)
        .ThenInclude(book => book.Author)
        .ToListAsync();

    // --- Step 3: Handle Empty Cart scenario ---
    if (!cartItems.Any())
    {
        TempData["ErrorMessage"] = "Your shopping cart is empty. Please add items before proceeding to checkout.";
        return RedirectToAction("Profile", "ShoppingCartItems");
    }

    // --- Step 4: Prepare data for the view using ViewModel and calculate totals ---
    var viewModelList = new List<ShoppingCartItemViewModel>();
    decimal initialTotalCartPrice = 0m;
    int totalCartItems = 0;

    foreach (var item in cartItems)
    {
        if (item.Book == null) // Check if the book associated with the cart item still exists
        {
            // Handle cases where a book might have been removed from the system
            // Or if Book navigation property wasn't loaded correctly (though Include should handle it)
            TempData["ErrorMessage"] = $"An item in your cart (Book ID: {item.BookId}) is no longer available or has missing details. Please review your cart.";
            // It might be better to remove this item from cart or guide user to do so.
            // For now, redirecting back to cart.
            return RedirectToAction("Profile", "ShoppingCartItems");
        }

        string bookAuthorName = item.Book?.Author?.FullName ?? "Unknown Author";
        string bookTitle = item.Book?.Title ?? "Unknown Title";
        string bookListPriceDisplay = item.Book?.ListPrice.ToString("C") ?? "$0.00";
        decimal itemTotalPrice = item.Quantity * (item.Book?.ListPrice ?? 0m);
        string totalPriceDisplay = itemTotalPrice.ToString("C");
        string dateAddedDisplay = item.DateAdded.ToString("d");

        viewModelList.Add(new ShoppingCartItemViewModel
        {
            CartItemId = item.CartItemId,
            MemberId = item.MemberId,
            BookId = item.BookId,
            Quantity = item.Quantity,
            DateAddedDisplay = dateAddedDisplay,
            BookTitle = bookTitle,
            BookAuthorName = bookAuthorName,
            BookListPriceDisplay = bookListPriceDisplay,
            TotalPriceDisplay = totalPriceDisplay,
        });

        initialTotalCartPrice += itemTotalPrice;
        totalCartItems += item.Quantity;
    }

    // --- Step 5: Apply Discounts using the helper methods for consistency ---

    // Calculate Bulk Discount using the same helper method as PlaceOrder
    // This helper likely checks if totalCartItems > 5 for a 5% discount.
    decimal bulkDiscountPercentage = CalculateBulkDiscount(totalCartItems);

    // Calculate Loyalty Discount using the same helper method as PlaceOrder
    // This helper likely checks member.TotalSuccessfulItemsSinceLastLoyaltyDiscount >= 10 for a 10% discount.
    decimal loyaltyDiscountPercentage = await CalculateLoyaltyDiscountAsync(memberId);

    // Total stackable discount (additive)
    decimal totalDiscountPercentage = bulkDiscountPercentage + loyaltyDiscountPercentage;

    // Ensure total discount doesn't exceed 100%
    if (totalDiscountPercentage > 1.0m)
    {
        totalDiscountPercentage = 1.0m; // Cap discount at 100%
    }

    decimal totalDiscountAmount = initialTotalCartPrice * totalDiscountPercentage;

    // Calculate individual discount amounts for display in the view
    // These are based on the percentages obtained from the consistent helper methods
    decimal bulkDiscountAmount = initialTotalCartPrice * bulkDiscountPercentage;
    decimal loyaltyDiscountAmount = initialTotalCartPrice * loyaltyDiscountPercentage;

    decimal finalTotalCartPrice = initialTotalCartPrice - totalDiscountAmount;

    // Ensure final price doesn't go below zero
    if (finalTotalCartPrice < 0m)
    {
        finalTotalCartPrice = 0m;
    }

    // --- Step 6: Pass data (ViewModel list, totals, and discounts) to the OrderStepOne view ---
    ViewData["InitialTotalCartPrice"] = initialTotalCartPrice.ToString("C");
    ViewData["TotalCartItems"] = totalCartItems;
    ViewData["BulkDiscountPercentage"] = bulkDiscountPercentage.ToString("P0"); // "P0" for percentage with 0 decimal places
    ViewData["LoyaltyDiscountPercentage"] = loyaltyDiscountPercentage.ToString("P0");
    ViewData["TotalDiscountPercentage"] = totalDiscountPercentage.ToString("P0");
    ViewData["TotalDiscountAmount"] = totalDiscountAmount.ToString("C");
    ViewData["BulkDiscountAmount"] = bulkDiscountAmount.ToString("C");
    ViewData["LoyaltyDiscountAmount"] = loyaltyDiscountAmount.ToString("C");
    ViewData["FinalTotalCartPrice"] = finalTotalCartPrice.ToString("C");

    // Return the OrderStepOne view, passing the list of ViewModels as the model.
    return View(viewModelList);
}

        //I am writing claim code helper function in here
          /// <summary>
        /// Generates a unique alphanumeric claim code that does not currently exist in the Orders table.
        /// </summary>
        /// <returns>A unique claim code string.</returns>
        private async Task<string> GenerateUniqueClaimCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // Character set for the code
            const int codeLength = 8; // Desired length of the claim code
            const int maxAttempts = 10; // Maximum attempts to generate a unique code

            Random random = new Random(); // Use System.Random for general randomness
            // For higher security/cryptographic randomness, consider System.Security.Cryptography.RandomNumberGenerator

            string claimCode;
            int attempts = 0;

            do
            {
                // Generate a random string
                claimCode = new string(Enumerable.Repeat(chars, codeLength)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());

                // Check if the generated code already exists in the database
                // Use AnyAsync for efficient checking
                bool codeExists = await _context.Orders.AnyAsync(o => o.ClaimCode == claimCode);

                if (!codeExists)
                {
                    // Code is unique, return it
                    return claimCode;
                }

                attempts++;
                // Optional: Add a small delay to avoid hammering the database on collisions
                // await Task.Delay(50);

            } while (attempts < maxAttempts); // Loop until unique code is found or max attempts reached

            // If after max attempts a unique code is not found, handle this error.
            // This is highly unlikely with a reasonable code length and character set.
            // You might log an error, throw an exception, or return a specific error code.
            Debug.WriteLine($"Failed to generate a unique claim code after {maxAttempts} attempts.");
            throw new InvalidOperationException("Could not generate a unique claim code.");
        }
        // --- End Helper method to generate a unique claim code ---

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

    // --- Step 4: Calculate totals and discounts (Server-side calculation is key for accuracy) ---
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

    // --- Calculate and Apply Discounts ---

    // Calculate Bulk Discount based on current cart items
    decimal bulkDiscountPercentage = CalculateBulkDiscount(totalCartItems); // 5% for > 5 items

    // Calculate Loyalty Discount based on member's past successful order items
    // This now uses the TotalSuccessfulItemsSinceLastLoyaltyDiscount field from the Member model
    decimal loyaltyDiscountPercentage = await CalculateLoyaltyDiscountAsync(memberId); 

    // Total stackable discount (additive)
    decimal totalDiscountPercentage = bulkDiscountPercentage + loyaltyDiscountPercentage;

    // Ensure total discount doesn't exceed 100% (though unlikely with these rules)
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
            // --- Generate a unique claim code ---
            string uniqueClaimCode = await GenerateUniqueClaimCodeAsync();
            Debug.WriteLine($"Generated unique claim code: {uniqueClaimCode}");
            // --- End Generate unique claim code ---

            // Create the main Order object
            // This will result in an INSERT into the 'Orders' table.
            var order = new Order
            {
                MemberId = memberId, // Associates the order with the current member
                OrderDate = DateTime.UtcNow, // Use UTC for consistency
                OrderStatus = "Pending", // Initial status, can be updated later (e.g., "Processing", "Shipped")
                TotalAmount = finalTotalCartPrice, // The final calculated price after all discounts
                DiscountApplied = totalDiscountPercentage, // Store the total discount percentage applied
                ClaimCode = uniqueClaimCode, // Assign the generated unique claim code here
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
                    UnitPrice = cartItem.Book.ListPrice, // Use the list price at the time of order
                    Book = cartItem.Book // Set the Book navigation property
                };
                order.OrderItems.Add(orderItem);
            }

            // Add the new Order (which includes its OrderItems) to the DbContext
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save the Order and its OrderItems to the database

            // --- Update Member Loyalty Tracking AFTER successful order save ---
            var memberToUpdate = await _context.Members.FindAsync(memberId);
            if (memberToUpdate != null)
            {
                // If the loyalty discount was applied to THIS order, reset the counter
                if (loyaltyDiscountPercentage > 0)
                {
                    memberToUpdate.TotalSuccessfulItemsSinceLastLoyaltyDiscount %= 10;
                    Debug.WriteLine($"Member ID {memberId}: Loyalty discount applied, resetting successful items counter.");
                }
                else
                {
                    // If loyalty discount was NOT applied, add the items from the current order
                    // Assuming the order status will be updated from "Pending" to a successful status later.
                    // The loyalty calculation helper filters for orders != "Pending", so items
                    // from this new order will be counted towards the *next* discount once its status changes.
                    // For a more immediate update, you could add the items here IF the order status
                    // is guaranteed to be updated to successful immediately after this transaction.
                    // For now, sticking to the helper's logic which counts non-pending orders.

                    // Increment the general OrderCount (if still needed for other purposes)
                    memberToUpdate.OrderCount++;
                    Debug.WriteLine($"Member ID {memberId}: OrderCount incremented to {memberToUpdate.OrderCount}.");

                    // If you wanted to add items to the counter here immediately upon placing a *successful* order,
                    // you would do it like this (assuming OrderStatus is changed to non-Pending within this transaction):
                     memberToUpdate.TotalSuccessfulItemsSinceLastLoyaltyDiscount += totalCartItems;
                     Debug.WriteLine($"Member ID {memberId}: Added {totalCartItems} items to loyalty counter. New total: {memberToUpdate.TotalSuccessfulItemsSinceLastLoyaltyDiscount}.");
                }

                _context.Members.Update(memberToUpdate); // Mark the member entity as modified
                await _context.SaveChangesAsync(); // Save the changes to the member
            }
            // --- End Update Member Loyalty Tracking ---


            // Re-fetch member for email (optional, could use memberToUpdate)
            var memberForEmail = await _context.Members.FindAsync(memberId);
            if (memberForEmail == null || string.IsNullOrEmpty(memberForEmail.Email))
            {
                TempData["WarningMessage"] = "Order placed, but email could not be sent (member or email missing).";
                // Log this warning
            }
            else
            {
                try
                {
                    string userEmail = memberForEmail.Email;
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
            // Returning JSON is suitable for AJAX calls. If this is a standard form post,
            // you might prefer a RedirectToAction to a confirmation page.
            return Json(new { success = true, message = $"Order #{order.OrderId} placed successfully!", orderId = order.OrderId });


        }
        catch (Exception ex) // Consider logging the specific exception 'ex'
        {
            // If any error occurs during the process, roll back the transaction
            await transaction.RollbackAsync();
            Debug.WriteLine($"Error placing order: {ex.Message}"); // Log the error
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
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] 
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


            return View(order);
        }
       
    //get:orders/admineditview/5
    public async Task<IActionResult> AdminEditView(int? id)
    {
        // Check if an ID was provided
        if (id == null)
        {
            return NotFound();
        }

        // Fetch the order from the database
        // Include related entities if you need to display them in the admin edit view
        var order = await _context.Orders
            .Include(o => o.Member) // Include Member if you want to display member info
            // .Include(o => o.OrderItems) // Include OrderItems if you want to display them
            //    .ThenInclude(oi => oi.Book) // Include Book details for items if needed
            .FirstOrDefaultAsync(m => m.OrderId == id);

        // Check if the order was found
        if (order == null)
        {
            return NotFound();
        }

        // No ownership check needed here for the admin view

        // Pass the order model to the view
        return View(order);
    }
    // POST: Orders/AdminEdit/5
        // This action handles the form submission from AdminEditView to update the order status.
        [HttpPost]
        [ValidateAntiForgeryToken] // Protect against CSRF attacks

   public async Task<IActionResult> AdminEdit(int id, [Bind("OrderId,OrderStatus")] Order order)
{
    Console.WriteLine("--- AdminEdit POST Action Start ---"); // Start of the method
    Console.WriteLine($"1. Received id from route: {id}");
    // Console.WriteLine($"2. Received order.OrderId from bound model: {order.OrderId}"); // Removed as order.OrderId is not used for ID check
    Console.WriteLine($"3. Received order.OrderStatus from bound model: {order.OrderStatus}");

    // The ID from the route (int id) is the authoritative identifier for the order.
    // We no longer check if it matches order.OrderId from the bound model,
    // as order.OrderId is not reliably populated from the form body after removing the hidden input.
    // The check `if (id != order.OrderId)` is removed.

    // Fetch the existing order from the database based on the route ID.
    // This is crucial to get all the original data (MemberId, TotalAmount, etc.)
    // and to ensure we are updating an existing entity tracked by the context.
    // Include Member again here if you need it when returning the view on validation failure.
    Console.WriteLine($"7. Fetching existing order with ID: {id} from database.");
    var existingOrder = await _context.Orders
                              .Include(o => o.Member) // Include Member if your view needs it for display on error
                              // .Include(o => o.OrderItems) // Include OrderItems if your view displays them
                              //    .ThenInclude(oi => oi.Book) // Include Book details for items if needed
                              .FirstOrDefaultAsync(o => o.OrderId == id);
    Console.WriteLine("8. Finished fetching existing order.");

    // Check if the order was found in the database
    Console.WriteLine("9. Checking if existing order was found.");
    if (existingOrder == null)
    {
        Console.WriteLine("10. Existing order not found. Returning NotFound.");
        // Log that the order was not found
        // _logger.LogWarning("AdminEdit POST: Order with ID {OrderId} not found for editing.", id);
        return NotFound(); // Order not found in the database
    }
    Console.WriteLine("11. Existing order found.");

    // --- Update only the allowed properties from the submitted 'order' model ---
    // This is done BEFORE checking ModelState.IsValid.
    // If validation fails, existingOrder will contain the user's submitted status,
    // allowing the view to redisplay it correctly.
    Console.WriteLine($"12. Updating existingOrder.OrderStatus to: {order.OrderStatus}");
    existingOrder.OrderStatus = order.OrderStatus;
    Console.WriteLine("13. OrderStatus updated on existingOrder entity.");

    // Check for model state validity before attempting to save.
    // This primarily validates the OrderStatus field based on any model annotations.
    Console.WriteLine("14. Checking ModelState.IsValid.");
    if (ModelState.IsValid)
    {
        Console.WriteLine("15. ModelState is valid. Proceeding to save changes.");
        try
        {
            Console.WriteLine("16. Inside try block.");
            // existingOrder.OrderStatus is already updated above

            // Update the DateUpdated timestamp to reflect the change
            Console.WriteLine("17. Updating DateUpdated timestamp.");
            existingOrder.DateUpdated = DateTime.UtcNow;
            Console.WriteLine("18. DateUpdated timestamp updated.");

            // Entity Framework is already tracking 'existingOrder' because we fetched it.
            // It will detect the change to OrderStatus and DateUpdated.
            // No need to explicitly set state to Modified.

            Console.WriteLine("19. Calling _context.SaveChangesAsync().");
            await _context.SaveChangesAsync(); // Save the changes to the database
            Console.WriteLine("20. _context.SaveChangesAsync() completed.");

            // Optional: Add a success message to TempData for display on the next page
            Console.WriteLine("21. Adding success message to TempData.");
            TempData["SuccessMessage"] = $"Order #{existingOrder.OrderId} status updated to {existingOrder.OrderStatus}.";
            Console.WriteLine("22. Success message added to TempData.");

            Console.WriteLine("23. Redirecting to Index action.");
            // Redirect to the Admin Order List page after successful update.
            // This follows the Post/Redirect/Get pattern.
            return RedirectToAction(nameof(Index)); // Assuming Index is your admin list view
            // Or redirect to an Admin Details page if you have one:
            // return RedirectToAction(nameof(Details), new { id = existingOrder.OrderId });
        }
        catch (DbUpdateConcurrencyException concurrencyEx) // Catch specific concurrency exception
        {
            Console.WriteLine($"24. Caught DbUpdateConcurrencyException: {concurrencyEx.Message}");
            // Handle concurrency issues if the order was changed by someone else
            // Check if the order still exists (wasn't deleted concurrently)
            Console.WriteLine($"25. Checking if order {order.OrderId} still exists."); // Note: order.OrderId will be 0 here
            if (!OrderExists(id)) // Use the route 'id' here to check existence
            {
                Console.WriteLine("26. Order was concurrently deleted. Returning NotFound.");
                // Log that the order was deleted concurrently
                // _logger.LogWarning("AdminEdit POST: Order with ID {OrderId} was concurrently deleted during update attempt.", id);
                return NotFound(); // Order was deleted by another process
            }
            else
            {
                Console.WriteLine("27. Other concurrency issue. Re-throwing exception.");
                // Log other concurrency issues
                // _logger.LogError("AdminEdit POST: Concurrency error updating order status for OrderId {OrderId}", id);
                throw; // Re-throw the exception if it's a different concurrency issue
            }
        }
        catch (Exception ex) // Catch any other unexpected errors during the save process
        {
            Console.WriteLine($"28. Caught general Exception: {ex.Message}");
            // Log the exception (use your logging framework)
            // _logger.LogError(ex, "AdminEdit POST: Error updating order status for OrderId {OrderId}", id);

            // Add an error message to TempData
            Console.WriteLine("29. Adding error message to TempData.");
            TempData["ErrorMessage"] = "An error occurred while updating the order status. Please try again.";
            Console.WriteLine("30. Error message added to TempData.");

            // Return to the view to show the error.
            // 'existingOrder' already has the user's submitted status and potentially validation errors.
            // The .Include(o => o.Member) above ensures Member is available if needed by the view.
            Console.WriteLine("31. Returning AdminEditView with existingOrder model.");
            return View("AdminEditView", existingOrder); // Return the existing order (with user's status) to the view
        }
    }
    else // if (ModelState.IsValid) is false
    {
        Console.WriteLine("32. ModelState is NOT valid. Falling through to return view.");
        // If ModelState is NOT valid (e.g., validation error on OrderStatus), fall through here.
        // 'existingOrder' now holds the original data + the user's submitted OrderStatus.
        // The .Include(o => o.Member) above ensures Member is available if needed by the view.
        // Validation errors will be displayed by asp-validation-summary and asp-validation-for tag helpers.
        Console.WriteLine("33. Returning AdminEditView with existingOrder model (due to invalid model state).");
        return View("AdminEditView", existingOrder); // Return the existing order (with user's status) to the view so user can correct input
    }
     // This line will only be reached if there's no valid return path above (shouldn't happen in this structure)
    // Console.WriteLine("--- AdminEdit POST Action End (Unexpected Path) ---");
    // return something appropriate if needed, though the above returns cover all cases.
}


        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] 
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
    if (!string.IsNullOrEmpty(order.ClaimCode))
    {
        bodyBuilder.AppendLine($"<p><strong>Claim Code:</strong> {order.ClaimCode}</p>");
    }
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
        /// <summary>
        /// Calculates the bulk discount percentage based on the number of items in the current cart.
        /// </summary>
        /// <param name="currentCartItemCount">The total number of items in the member's current cart.</param>
        /// <returns>The bulk discount percentage (e.g., 0.05 for 5%).</returns>
               /// <summary>
        /// Calculates the bulk discount percentage based on the number of items in the current cart.
        /// </summary>
        /// <param name="currentCartItemCount">The total number of items in the member's current cart.</param>
        /// <returns>The bulk discount percentage (e.g., 0.05 for 5%).</returns>
        private decimal CalculateBulkDiscount(int currentCartItemCount)
        {
            decimal bulkDiscountPercentage = 0m;
            // --- Bulk Discount Logic ---
            // Apply 5% discount if there are MORE THAN 5 items in the CURRENT order/cart.
            // If the rule is 5 or more, change > 5 to >= 5.
            if (currentCartItemCount > 5)
            {
                bulkDiscountPercentage = 0.05m; // 5% discount
                Debug.WriteLine($"Bulk Discount Applied: 5% for {currentCartItemCount} items in current cart.");
            }
            return bulkDiscountPercentage;
        }

        /// <summary>
        /// Calculates the loyalty discount percentage based on the total number of items
        /// in past successful orders for a member, using the tracking field in the Member model.
        /// This implements the "10 items then reset" loyalty logic using the
        /// TotalSuccessfulItemsSinceLastLoyaltyDiscount field.
        /// </summary>
        /// <param name="memberId">The ID of the member.</param>
        /// <returns>The loyalty discount percentage (e.g., 0.10 for 10%).</returns>
        private async Task<decimal> CalculateLoyaltyDiscountAsync(int memberId)
        {
            decimal loyaltyDiscountPercentage = 0m;

            // --- Loyalty Discount Logic ---
            // Fetch the member to check their TotalSuccessfulItemsSinceLastLoyaltyDiscount
            var member = await _context.Members.FindAsync(memberId);

            if (member != null)
            {
                Debug.WriteLine($"Member ID {memberId} has {member.TotalSuccessfulItemsSinceLastLoyaltyDiscount} successful items since last loyalty discount.");

                // Apply 10% loyalty discount if the total successful order items
                // since the last discount is 10 or more.
                if (member.TotalSuccessfulItemsSinceLastLoyaltyDiscount >= 10)
                {
                    loyaltyDiscountPercentage = 0.10M; // 10% discount
                    Debug.WriteLine($"Loyalty Discount Applied: 10% for {member.TotalSuccessfulItemsSinceLastLoyaltyDiscount} successful items.");

                    
                }
            }

            return loyaltyDiscountPercentage; 
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
                // Apply 10% if successful orders is exactly 10, 20, 30, etc.
                // This uses the total OrderCount. For a true "next order" and "reset",
                // you'd need a separate counter or state in the Member model.
                if (member.OrderCount > 0 && member.OrderCount % 10 == 0)
                {
                    return 0.10M; // 10% discount
                }
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
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CancelOrder(int id) // 'id' here is the OrderId
{
    // 1. Get the current user's ID
    var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
    {
        // This should ideally not happen if [Authorize] is effective
        // and claims are correctly set up during login.
        TempData["ErrorMessage"] = "User not authenticated. Please log in.";
        return RedirectToAction("Login", "Account"); // Or your login page
    }

    // 2. Retrieve the Order with its Items, ensuring it belongs to the current user
    var order = await _context.Orders
                              .Include(o => o.OrderItems) // Crucial: Include OrderItems for deletion
                              .FirstOrDefaultAsync(o => o.OrderId == id && o.MemberId == memberId);

    if (order == null)
    {
        TempData["ErrorMessage"] = "Order not found or you do not have permission to modify it.";
        return RedirectToAction(nameof(MyOrders));
    }

    // 3. Check Cancellability based on OrderStatus
    // Define statuses that prevent cancellation/deletion by the user.
    // Orders that are "Shipped" or "Completed" typically cannot be cancelled by the user this way.
    var nonCancellableStatuses = new[] { "Completed", "Shipped" }; // Add other statuses if needed, e.g., "In Transit"
    
    if (nonCancellableStatuses.Contains(order.OrderStatus, StringComparer.OrdinalIgnoreCase))
    {
        TempData["ErrorMessage"] = $"Order #{order.OrderId} cannot be cancelled because its status is '{order.OrderStatus}'. Please contact support if you need assistance.";
        return RedirectToAction(nameof(MyOrders));
    }

    // 4. Perform Deletion within a Transaction for atomicity
    using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
            // First, remove all associated OrderItems
            if (order.OrderItems != null && order.OrderItems.Any())
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
            }

            // Then, remove the Order itself
            _context.Orders.Remove(order);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // If all operations were successful, commit the transaction
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Order #{order.OrderId} has been successfully cancelled and removed.";
        }
        catch (DbUpdateException dbEx) // Catch specific database update errors
        {
            await transaction.RollbackAsync();
            // Log the detailed exception (dbEx) with your logging framework
            // For example: _logger.LogError(dbEx, "Error cancelling order {OrderId}", order.OrderId);
            TempData["ErrorMessage"] = "An error occurred while updating the database. The order could not be cancelled. Please try again.";
        }
        catch (Exception ex) // Catch any other unexpected errors
        {
            await transaction.RollbackAsync();
            // Log the detailed exception (ex)
            // For example: _logger.LogError(ex, "Unexpected error cancelling order {OrderId}", order.OrderId);
            TempData["ErrorMessage"] = "An unexpected error occurred. The order could not be cancelled. Please try again.";
        }
    }

    // 5. Redirect back to the MyOrders page
    return RedirectToAction(nameof(MyOrders));
}


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

        
        
    }
}
