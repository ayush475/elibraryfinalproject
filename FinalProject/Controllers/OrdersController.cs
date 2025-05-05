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
        public async Task<IActionResult> Create([Bind("OrderId,MemberId,OrderDate,OrderStatus,TotalAmount,DiscountApplied,ClaimCode,DateAdded,DateUpdated")] Order order)
        {
            // In a real application, you would likely get the MemberId from the authenticated user's claims
            // Example: var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Then find the corresponding Member in your database and assign the MemberId to the order.

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "MemberId", order.MemberId); // Might not be needed
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
            // Example: if (order.MemberId != GetCurrentMemberId()) { return Forbid(); }
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "MemberId", order.MemberId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authorized users can post to edit
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,MemberId,OrderDate,OrderStatus,TotalAmount,DiscountApplied,ClaimCode,DateAdded,DateUpdated")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            // Ensure the logged-in user is authorized to edit this specific order
            // Example: var existingOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == id);
            // if (existingOrder == null || existingOrder.MemberId != GetCurrentMemberId()) { return Forbid(); }

            if (ModelState.IsValid)
            {
                try
                {
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "MemberId", order.MemberId);
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
            // Example: if (order.MemberId != GetCurrentMemberId()) { return Forbid(); }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authorized users can post to delete
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Ensure the logged-in user is authorized to delete this specific order
            // Example: var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id && o.MemberId == GetCurrentMemberId());
            var order = await _context.Orders.FindAsync(id);

            if (order != null)
            {
                 // Add a check here to ensure the logged-in user owns the order before deleting
                 // Example: if (order.MemberId != GetCurrentMemberId()) { return Forbid(); }
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
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

            // --- Create a NEW Order for this item ---
            var newOrder = new Order
            {
                MemberId = member.MemberId,
                Member = member, // Assign the fetched Member entity
                OrderDate = DateTime.UtcNow, // Use UTC for consistency
                OrderStatus = "Placed", // Set status to indicate a direct order (e.g., "Placed", "Completed")
                DiscountApplied = 0, // No order-level discount applied initially
                ClaimCode = null, // No claim code initially
                DateAdded = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                OrderItems = new List<OrderItem>() // Initialize the collection
            };

            // --- Create the OrderItem for the book ---
            var newOrderItem = new OrderItem
            {
                // OrderId is set implicitly when adding to the Order's collection and saving
                Order = newOrder, // FIX: Assign the new Order object to the navigation property
                BookId = book.BookId,
                Book = book, // Assign the fetched Book entity
                Quantity = quantity,
                UnitPrice = book.ListPrice, // Use ListPrice from the Book
                Discount = book.SaleDiscount ?? 0 // Use SaleDiscount from the Book, handle null
            };

            // Add the OrderItem to the new Order's collection
            newOrder.OrderItems.Add(newOrderItem);

            // Calculate the TotalAmount for the new order (based on the single item)
            newOrder.TotalAmount = (newOrderItem.Quantity * newOrderItem.UnitPrice) - newOrderItem.Discount;

            // Add the new Order and its OrderItem to the context
            _context.Orders.Add(newOrder);
            // EF Core will automatically add the newOrderItem because it's part of the newOrder's collection

            // Save changes to the database (creates the new Order and OrderItem)
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Order placed for {book.Title}.";

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
                    TotalAmount = 0,
                    DiscountApplied = 0,
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
                _context.OrderItems.Add(newOrderItem); // Also add to the context for saving
            }

            // 5. Update the Order's TotalAmount and DateUpdated
            // This section updates the Orders table
            // Recalculate total based on current order items
            order.TotalAmount = order.OrderItems.Sum(oi => (oi.Quantity * oi.UnitPrice) - oi.Discount);
            order.DateUpdated = DateTime.UtcNow;

            _context.Orders.Update(order); // Mark the Order as updated

            // 6. Save all changes (Order and OrderItems)
            await _context.SaveChangesAsync();

            // 7. Redirect or return a success response
            // You might redirect to the order details page, a shopping cart page,
            // or return a JSON success message for AJAX calls.
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
                // Need to fetch the order again to ensure its OrderItems collection is up-to-date after removal
                // Or manually update the total before saving if removing the item directly impacts the collection
                // A simpler way after removal is just to update the DateUpdated and potentially the status if this was the last item
                // Let's recalculate the total from the remaining items
                 // Ensure the OrderItems collection is loaded for recalculation
                 await _context.Entry(orderItem.Order).Collection(o => o.OrderItems).LoadAsync();

                 Debug.WriteLine($"Recalculating total for Order {orderItem.OrderId}. Current items count (before save): {orderItem.Order.OrderItems.Count}"); // Debugging line

                 orderItem.Order.TotalAmount = orderItem.Order.OrderItems
                    .Where(oi => oi.OrderItemId != orderItemId) // Exclude the item being removed from calculation
                    .Sum(oi => (oi.Quantity * oi.UnitPrice) - oi.Discount);

                 Debug.WriteLine($"New TotalAmount for Order {orderItem.OrderId}: {orderItem.Order.TotalAmount}"); // Debugging line


                // If the order has no remaining items, you might change its status to "Cancelled" or "Empty"
                if (!orderItem.Order.OrderItems.Any(oi => oi.OrderItemId != orderItemId))
                {
                    Debug.WriteLine($"Order {orderItem.OrderId} is now empty. Setting status to 'Cancelled'."); // Debugging line
                    orderItem.Order.OrderStatus = "Cancelled"; // Or another appropriate status
                    orderItem.Order.TotalAmount = 0; // Ensure total is 0 if order is empty/cancelled
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
