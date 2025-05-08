using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using FinalProject.Models;

namespace FinalProject.Controllers
{
    [Authorize]
    public class ShoppingCartItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShoppingCartItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action to display the authenticated user's shopping cart items
        // Accessible via /ShoppingCartItems/Profile
       public async Task<IActionResult> Profile()
        {
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // Assuming Login is in AccountController
                return RedirectToAction("Login", "Account");
            }

            if (!int.TryParse(memberIdClaim.Value, out int memberId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // Assuming Login is in AccountController
                return RedirectToAction("Login", "Account");
            }

            // Retrieve the shopping cart items for the member
            var cartItems = await _context.ShoppingCartItems
                .Where(item => item.MemberId == memberId)
                .Include(item => item.Book)
                .ThenInclude(book => book.Author)
                .ToListAsync();

            var viewModelList = new List<ShoppingCartItemViewModel>();

            // --- START: Calculate total items and total price ---
            decimal totalCartPrice = 0m;
            int totalCartItems = 0;
            // --- END: Calculate total items and total price ---


            foreach (var item in cartItems)
            {
                string bookAuthorName = item.Book?.Author?.FullName ?? "Unknown Author";
                string bookTitle = item.Book?.Title ?? "Unknown Title";
                // Ensure currency formatting uses the correct culture if needed
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
                    TotalPriceDisplay = totalPriceDisplay
                    // You might add a numeric property for the total price here too for easier summation
                    // ItemTotalPrice = itemTotalPrice // Add this to your ViewModel
                });

                // --- START: Accumulate totals ---
                totalCartPrice += itemTotalPrice; // Sum up for the total price
                totalCartItems += item.Quantity; // Sum up for the total quantity
                // --- END: Accumulate totals ---
            }

            // --- START: Pass totals to the view using ViewData ---
            ViewData["TotalCartPrice"] = totalCartPrice.ToString("C"); // Pass the total price, formatted as currency
            ViewData["TotalCartItems"] = totalCartItems; // Pass the total number of items
            // --- END: Pass totals to the view using ViewData ---


            return View(viewModelList);
        }

        // POST: ShoppingCartItems/Remove/5
        // This action handles removing an item from the cart
        [HttpPost] // Use POST for removal
        [ValidateAntiForgeryToken] // Add anti-forgery token for security
        public async Task<IActionResult> Remove(int id) // 'id' is the CartItemId
        {
            // Get the MemberId from the authenticated user's claims
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out int memberId))
            {
                // User not logged in or invalid MemberId claim
                // Log the issue or redirect to login
                 await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                 return RedirectToAction("Login", "Account"); // Assuming Login is in AccountController
            }

            // Find the cart item by ID
            var shoppingCartItem = await _context.ShoppingCartItems.FindAsync(id);

            // Check if the item exists and belongs to the current user
            if (shoppingCartItem == null || shoppingCartItem.MemberId != memberId)
            {
                // Item not found or does not belong to the user
                // Return Not Found or an Unauthorized result
                return NotFound(); // Or return Unauthorized() if you want to be explicit
            }

            // Remove the item from the context
            _context.ShoppingCartItems.Remove(shoppingCartItem);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            // Redirect back to the shopping cart profile page
            return RedirectToAction(nameof(Profile));
        }


        // GET: ShoppingCartItems
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ShoppingCartItems.Include(s => s.Book).Include(s => s.Member);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ShoppingCartItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shoppingCartItem = await _context.ShoppingCartItems
                .Include(s => s.Book)
                .Include(s => s.Member)
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (shoppingCartItem == null)
            {
                return NotFound();
            }

            return View(shoppingCartItem);
        }

        // GET: ShoppingCartItems/Create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn");
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email");
            return View();
        }

        // POST: ShoppingCartItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CartItemId,MemberId,BookId,Quantity,DateAdded")] ShoppingCartItem shoppingCartItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shoppingCartItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", shoppingCartItem.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email", shoppingCartItem.MemberId);
            return View(shoppingCartItem);
        }

        // GET: ShoppingCartItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shoppingCartItem = await _context.ShoppingCartItems.FindAsync(id);
            if (shoppingCartItem == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", shoppingCartItem.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email", shoppingCartItem.MemberId);
            return View(shoppingCartItem);
        }

        // POST: ShoppingCartItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CartItemId,MemberId,BookId,Quantity,DateAdded")] ShoppingCartItem shoppingCartItem)
        {
            if (id != shoppingCartItem.CartItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shoppingCartItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShoppingCartItemExists(shoppingCartItem.CartItemId))
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
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", shoppingCartItem.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email", shoppingCartItem.MemberId);
            return View(shoppingCartItem);
        }


        // Note: The Delete action below is for a confirmation page (GET) and actual deletion (POST).
        // The new 'Remove' action above is a common pattern for direct deletion from a list/cart view.
        // You can keep both if needed for different scenarios, but for the cart view, 'Remove' is more direct.

        // GET: ShoppingCartItems/Delete/5 (Confirmation page, likely not needed for a simple cart removal)
        // public async Task<IActionResult> Delete(int? id)
        // {
        //     if (id == null)
        //     {
        //         return NotFound();
        //     }

        //     var shoppingCartItem = await _context.ShoppingCartItems
        //         .Include(s => s.Book)
        //         .Include(s => s.Member)
        //         .FirstOrDefaultAsync(m => m.CartItemId == id);
        //     if (shoppingCartItem == null)
        //     {
        //         return NotFound();
        //     }

        //     return View(shoppingCartItem);
        // }

        // POST: ShoppingCartItems/Delete/5 (Actual deletion, often paired with the GET Delete)
        // [HttpPost, ActionName("Delete")]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> DeleteConfirmed(int id)
        // {
        //     var shoppingCartItem = await _context.ShoppingCartItems.FindAsync(id);
        //     if (shoppingCartItem != null)
        //     {
        //         // Optional: Add check to ensure the item belongs to the current user here too
        //         // if (shoppingCartItem.MemberId != GetCurrentMemberId()) { return Unauthorized(); }
        //         _context.ShoppingCartItems.Remove(shoppingCartItem);
        //         await _context.SaveChangesAsync();
        //     }
        //     // Redirect to the cart view (Profile) or Index, depending on where you want the user to go
        //     return RedirectToAction(nameof(Profile));
        // }


        private bool ShoppingCartItemExists(int id)
        {
            return _context.ShoppingCartItems.Any(e => e.CartItemId == id);
        }

        // Helper method to get the current member's ID - useful if you need it in multiple places
        // private int GetCurrentMemberId()
        // {
        //     var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        //     if (memberIdClaim != null && int.TryParse(memberIdClaim.Value, out int memberId))
        //     {
        //         return memberId;
        //     }
        //     // Handle cases where the member ID is not available (e.g., throw exception, return -1)
        //     throw new InvalidOperationException("Member ID not found in claims.");
        // }
    }
}