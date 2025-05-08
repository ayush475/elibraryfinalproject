using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // Added for Include and ToListAsync
using FinalProject.Data; // Assuming your DbContext is here
using FinalProject.ViewModels; // Assuming your ViewModels are here
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectList
using FinalProject.Models; // Added for ShoppingCartItem model

namespace FinalProject.Controllers // Adjust namespace as needed
{
    [Authorize] // Ensure only authenticated users can access this controller's actions
    public class ShoppingCartItemsController : Controller // New controller for shopping cart items
    {
        private readonly ApplicationDbContext _context; // Replace ApplicationDbContext

        public ShoppingCartItemsController(ApplicationDbContext context) // Replace ApplicationDbContext
        {
            _context = context;
        }

        // Action to display the authenticated user's shopping cart items
        // Accessible via /ShoppingCartItems/Profile
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

            // Retrieve the shopping cart items for the member
            var cartItems = await _context.ShoppingCartItems
                .Where(item => item.MemberId == memberId)
                .Include(item => item.Book) // Include the Book
                .ThenInclude(book => book.Author) // Include the Author related to the Book
                .ToListAsync();

            // Map the ShoppingCartItem models to ShoppingCartItemViewModels
            var viewModelList = new List<ShoppingCartItemViewModel>(); // Create a list of ViewModels

            foreach (var item in cartItems)
            {
                // Use the FullName property from the Author model, handling nulls for Book and Author
                string bookAuthorName = item.Book?.Author?.FullName ?? "Unknown Author";
                string bookTitle = item.Book?.Title ?? "Unknown Title";
                string bookListPriceDisplay = item.Book?.ListPrice.ToString("C") ?? "$0.00";
                string totalPriceDisplay = (item.Quantity * (item.Book?.ListPrice ?? 0m)).ToString("C");
                string dateAddedDisplay = item.DateAdded.ToString("d"); // Example formatting

                viewModelList.Add(new ShoppingCartItemViewModel
                {
                    CartItemId = item.CartItemId,
                    MemberId = item.MemberId,
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    DateAddedDisplay = dateAddedDisplay,
                    BookTitle = bookTitle,
                    BookAuthorName = bookAuthorName, // Now assigned using the FullName property
                    BookListPriceDisplay = bookListPriceDisplay,
                    TotalPriceDisplay = totalPriceDisplay
                });
            }

            // Pass the list of ViewModels to the view
            return View(viewModelList);
        }

        // GET: ShoppingCartItems
        // Note: This Index action might not be needed for a typical user-facing site,
        // but is included if you need an admin view or similar.
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: ShoppingCartItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

        // POST: ShoppingCartItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shoppingCartItem = await _context.ShoppingCartItems.FindAsync(id);
            if (shoppingCartItem != null)
            {
                _context.ShoppingCartItems.Remove(shoppingCartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShoppingCartItemExists(int id)
        {
            return _context.ShoppingCartItems.Any(e => e.CartItemId == id);
        }

        // Assuming you have a Login action in an AccountController
        // private IActionResult Login()
        // {
        //     // Redirect to your login page
        //     return RedirectToAction("Login", "Account");
        // }
    }
}
