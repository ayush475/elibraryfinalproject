using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize] attribute
using System.Security.Claims; // Required to access user claims
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for SignOutAsync
using Microsoft.AspNetCore.Authentication; // Needed for SignOutAsync
using System.Diagnostics; // Required for Debug.WriteLine

namespace FinalProject.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
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
            // Assuming your authentication setup adds the MemberId as a claim (e.g., ClaimTypes.NameIdentifier)
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                // User is authenticated but MemberId claim is missing or invalid
                // This indicates an issue with the authentication setup or user data
                return Unauthorized("Could not identify the logged-in member.");
            }

            // 2. Find the user's current active order or create one if none exists
            // An "active" order might be one with a specific status, e.g., "Pending" or "Cart"
            var order = await _context.Orders
                .Include(o => o.OrderItems) // Include OrderItems to check if the book is already in the cart
                .FirstOrDefaultAsync(o => o.MemberId == memberId && o.OrderStatus == "Pending"); // Assuming "Pending" is the status for an active cart

            // 3. Find the book to be added
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
            {
                return NotFound($"Book with ID {bookId} not found.");
            }

            if (order == null)
            {
                // No active order found, create a new one
                // Fetch the Member entity to satisfy the required navigation property
                var member = await _context.Members.FindAsync(memberId);
                if (member == null)
                {
                    // This should not happen if MemberId is from authenticated user, but good to check
                    return Unauthorized("Could not find the member associated with the logged-in user.");
                }

                order = new Order
                {
                    MemberId = memberId,
                    Member = member, // Assign the fetched Member entity
                    OrderDate = DateTime.UtcNow, // Use UTC for consistency
                    OrderStatus = "Pending", // Set initial status
                    TotalAmount = 0, // Will be calculated based on items
                    DiscountApplied = 0, // Will be calculated based on items and member discount
                    ClaimCode = "", // Or generate a default claim code
                    DateAdded = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    OrderItems = new List<OrderItem>() // Initialize the collection
                };
                _context.Orders.Add(order);
                // No need to SaveChangesAsync here, it will be saved with the OrderItem
            }

            // Ensure quantity is at least 1
            if (quantity < 1)
            {
                quantity = 1;
            }

            // 4. Check if the book is already in the order
            var existingOrderItem = order.OrderItems.FirstOrDefault(oi => oi.BookId == bookId);

            if (existingOrderItem != null)
            {
                // Book is already in the cart, update the quantity
                existingOrderItem.Quantity += quantity;
                existingOrderItem.Discount = book.SaleDiscount ?? 0; // Use SaleDiscount, handle null
                existingOrderItem.UnitPrice = book.ListPrice; // Use ListPrice
                _context.OrderItems.Update(existingOrderItem); // Mark the OrderItem as updated
            }
            else
            {
                // Book is not in the cart, add a new OrderItem
                var newOrderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    Order = order, // Assign the fetched Order entity
                    BookId = bookId,
                    Book = book, // Assign the fetched Book entity
                    Quantity = quantity,
                    UnitPrice = book.ListPrice, // Use ListPrice
                    Discount = book.SaleDiscount ?? 0 // Use SaleDiscount, handle null
                };
                order.OrderItems.Add(newOrderItem); // Add to the order's collection
                // No need to explicitly Add to context if adding to a tracked entity's collection
                // _context.OrderItems.Add(newOrderItem);
            }

            // --- Calculate Member Discount ---
            decimal memberDiscountPercentage = await CalculateMemberDiscount(memberId);
            Debug.WriteLine($"Member ID {memberId} qualifies for {memberDiscountPercentage * 100}% discount for cart.");
            // --- End Calculate Member Discount ---


            // 5. Update the Order's TotalAmount and DateUpdated
            // This section updates the Orders table
            // Recalculate subtotal based on current order items (before member discount)
            decimal subtotal = order.OrderItems.Sum(oi => (oi.Quantity * oi.UnitPrice) - oi.Discount);

            // Apply the member-level discount to the subtotal
            order.DiscountApplied = subtotal * memberDiscountPercentage;
            order.TotalAmount = subtotal - order.DiscountApplied;

            order.DateUpdated = DateTime.UtcNow;

            _context.Orders.Update(order); // Mark the Order as updated

            // 6. Save all changes (Order and OrderItems)
            await _context.SaveChangesAsync();

            // 7. Redirect or return a success response
            // You might redirect to the order details page (cart view), a shopping cart page,
            // or return a JSON success message for AJAX calls.
            // Redirecting to the order details page which should show the updated cart.
            TempData["Message"] = $"{book.Title} added to your cart. Member discount applied.";
            return RedirectToAction(nameof(Details), new { id = order.OrderId });
            // Or return Json(new { success = true, message = "Item added to order." });
        }
        // --- End AddItemToOrder Action ---


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


                 Debug.WriteLine($"New TotalAmount for Order {orderItem.OrderId}: {orderItem.Order.TotalAmount}"); // Debugging line


                // If the order has no remaining items, change its status to "Cancelled"
                if (!orderItem.Order.OrderItems.Any(oi => oi.OrderItemId != orderItemId))
                {
                    Debug.WriteLine($"Order {orderItem.OrderId} is now empty. Setting status to 'Cancelled'."); // Debugging line
                    orderItem.Order.OrderStatus = "Cancelled"; // Or another appropriate status
                    orderItem.Order.TotalAmount = 0; // Ensure total is 0 if order is empty/cancelled
                    orderItem.Order.DiscountApplied = 0; // Ensure discount is 0 if order is empty/cancelled
                }

                orderItem.Order.DateUpdated = DateTime.UtcNow; // Update the order's modification date
                _context.Orders.Update(orderItem.Order); // Mark the parent order as updated
                 Debug.WriteLine($"Order {orderItem.OrderId} marked for update."); // Debugging line


                // Save the changes (removes the order item and updates the order)
                 Debug.WriteLine("Saving changes to database..."); // Debugging line
                await _context.SaveChangesAsync();
                 Debug.WriteLine("Changes saved successfully."); // Debugging line


                TempData["Message"] = "Order item cancelled successfully.";
                 Debug.WriteLine("TempData message set: 'Order item cancelled successfully.'"); // Debugging line
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Debug.WriteLine($"Error cancelling OrderItem {orderItemId}: {ex.Message}"); // Debugging line
                // _logger.LogError(ex, $"Error cancelling OrderItem {orderItemId}");
                TempData["Message"] = "An error occurred while cancelling the order item.";
                 Debug.WriteLine("TempData message set: 'An error occurred while cancelling the order item.'"); // Debugging line
            }

            // Redirect back to the original page
             Debug.WriteLine($"Redirecting to: {returnUrl}"); // Debugging line
            return Redirect(returnUrl);
        }
        // --- End CancelOrderItem Action ---


        // --- Helper method to calculate member discount based on purchase history ---
        /// <summary>
        /// Calculates the discount percentage for a member based on the total quantity of books
        /// purchased in completed orders.
        /// </summary>
        /// <param name="memberId">The ID of the member.</param>
        /// <returns>The discount percentage (e.g., 0.05 for 5%, 0.10 for 10%).</returns>
        private async Task<decimal> CalculateMemberDiscount(int memberId)
        {
            // Define completed order statuses - adjust this based on your OrderStatus values
            var completedStatuses = new[] { "Completed", "Shipped", "Delivered" }; // Example statuses

            // Query all order items from completed orders for this member
            var purchasedItems = await _context.OrderItems
                .Include(oi => oi.Order) // Include the parent Order to check status
                .Where(oi => oi.Order.MemberId == memberId && completedStatuses.Contains(oi.Order.OrderStatus))
                .ToListAsync();

            // Sum the quantities of all purchased items
            int totalPurchasedBooks = purchasedItems.Sum(oi => oi.Quantity);

            // Determine discount percentage based on total purchased books
            decimal discountPercentage = 0.0m;
            if (totalPurchasedBooks >= 10)
            {
                discountPercentage = 0.10m; // 10% discount
            }
            else if (totalPurchasedBooks >= 5)
            {
                discountPercentage = 0.05m; // 5% discount
            }
            // Else, discountPercentage remains 0.0m

            return discountPercentage;
        }
        // --- End Helper method ---


        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }

        // Helper method to get the current logged-in member's ID from claims
        // You might move this to a base controller or a helper class
        private int GetCurrentMemberId()
        {
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (memberIdClaim != null && int.TryParse(memberIdClaim.Value, out int memberId))
            {
                return memberId;
            }
            // Handle the case where the MemberId claim is not found or invalid
            // This should ideally not happen if [Authorize] is used correctly,
            // but returning 0 or throwing an exception might be appropriate depending on your error handling strategy.
            throw new InvalidOperationException("Could not retrieve MemberId from authenticated user.");
        }
    }
}
