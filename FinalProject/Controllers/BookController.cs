using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data; // Make sure this namespace matches your DbContext location
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization; // Needed for the [Authorize] attribute
using System.Security.Claims; // Needed to access user claims
using Microsoft.AspNetCore.Http; // Needed for accessing Request.Headers.Referer
using Microsoft.AspNetCore.Authentication; // Needed for signing out
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for CookieAuthenticationDefaults.AuthenticationScheme
using System.Diagnostics; // Required for Debug.WriteLine

namespace FinalProject.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Book
        // Modified Index action to include search, pagination, ordering, and filtering by Genre and Author
        // Now also fetches bookmarked book IDs for authenticated users.
        public async Task<IActionResult> Index(string searchString, int? pageNumber, int? genreId, int? authorId)
        {
            // Define the number of items per page
            int pageSize = 10; // You can adjust this value

            // Start with the base query including related entities
            var books = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Publisher)
                .AsQueryable(); // Use AsQueryable() to build the query before executing

            // Apply search filter if a search string is provided
            if (!string.IsNullOrEmpty(searchString))
            {
                // Filter books by Title, Author's FirstName, or Author's LastName
                // Add null checks for Author before accessing its properties (Fixes CS8602 warning)
                books = books.Where(b => b.Title.Contains(searchString)
                                       || (b.Author != null && b.Author.FirstName != null && b.Author.FirstName.Contains(searchString))
                                       || (b.Author != null && b.Author.LastName != null && b.Author.LastName.Contains(searchString))
                                       || (b.Isbn != null && b.Isbn.Contains(searchString))); // Also search by ISBN
            }

            // Apply Genre filter if a genreId is provided
            if (genreId.HasValue && genreId.Value > 0) // Check if genreId has a value and is not the "All" option (assuming 0 or null for All)
            {
                books = books.Where(b => b.GenreId == genreId.Value);
            }

            // Apply Author filter if an authorId is provided
            if (authorId.HasValue && authorId.Value > 0) // Check if authorId has a value and is not the "All" option
            {
                books = books.Where(b => b.AuthorId == authorId.Value);
            }

            // Add ordering - Order by DateAdded in descending order to show latest first
            // You could also order by PublicationDate if that's more appropriate for "latest"
            books = books.OrderByDescending(b => b.DateAdded);

            // Get the total number of items after filtering and ordering (needed for pagination)
            var totalItems = await books.CountAsync();

            // Calculate the total number of pages
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Determine the current page number (default to 1 if not provided or invalid)
            int currentPage = (pageNumber ?? 1);
            if (currentPage < 1)
            {
                currentPage = 1;
            }
            else if (totalPages > 0 && currentPage > totalPages) // Prevent going past the last page if there are books
            {
                 currentPage = totalPages;
            }
            else if (totalPages == 0) // Handle case where there are no books matching filters/search
            {
                 currentPage = 1;
            }

            // Apply pagination using Skip and Take
            var paginatedBooks = await books
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // --- Fetch Bookmarked Book IDs for Authenticated User ---
            // Use null-conditional operator for safer access to User.Identity
            if (User?.Identity?.IsAuthenticated == true)
            {
                // Get the MemberId (integer primary key) from the claims
                var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(memberIdString) && int.TryParse(memberIdString, out int memberId))
                {
                    // Get the IDs of books bookmarked by this member
                    var bookmarkedBookIds = await _context.Bookmarks
                        .Where(b => b.MemberId == memberId) // Use the integer MemberId
                        .Select(b => b.BookId)
                        .ToListAsync();

                    // Pass the list of bookmarked book IDs to the view
                    ViewBag.BookmarkedBookIds = bookmarkedBookIds;
                }
            }
            // --- End Fetch Bookmarked Book IDs ---


            // Pass pagination, search, and filter information to the view using ViewBag
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString; // Pass the search string back to the view
            ViewBag.SelectedGenreId = genreId; // Pass the selected genre ID back
            ViewBag.SelectedAuthorId = authorId; // Pass the selected author ID back

            // Populate dropdown lists for Genre and Author for the view
            // Add a default "All" option with value 0 or null
            ViewBag.Genres = new SelectList(_context.Genres.OrderBy(g => g.Name), "GenreId", "Name", genreId);
            // Add null checks for Author properties when creating SelectList (Fixes CS8602 warning)
            ViewBag.Authors = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).Select(a => new { a.AuthorId, FullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim() }), "AuthorId", "FullName", authorId);


            // Return the paginated, filtered, and ordered list of books to the view
            return View(paginatedBooks);
        }

        // GET: Book/Details/5
          public async Task<IActionResult> Details(int? id)
        {
            Debug.WriteLine($"BookController.Details action called for Book ID: {id}"); // Debugging line

            if (id == null)
            {
                Debug.WriteLine("Book ID is null. Returning NotFound."); // Debugging line
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                Debug.WriteLine($"Book with ID {id} not found. Returning NotFound."); // Debugging line
                return NotFound();
            }
            Debug.WriteLine($"Book found: {book.Title} (ID: {book.BookId})"); // Debugging line


            // --- Check if the logged-in user has a cancellable order item for this book ---
            Debug.WriteLine($"Checking if user is authenticated: {User.Identity.IsAuthenticated}"); // Debugging line
            if (User.Identity.IsAuthenticated)
            {
                var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                 Debug.WriteLine($"MemberId claim string: {memberIdString}"); // Debugging line

                if (int.TryParse(memberIdString, out int memberId))
                {
                     Debug.WriteLine($"Successfully parsed MemberId: {memberId}"); // Debugging line
                    // Define cancellable order statuses (must match those in OrdersController.CancelOrderItem)
                    var cancellableStatuses = new[] { "Pending", "Placed" };
                     Debug.WriteLine($"Cancellable statuses: {string.Join(", ", cancellableStatuses)}"); // Debugging line


                    // Find a cancellable order item for this book and user
                    var cancellableOrderItem = await _context.OrderItems
                        .Include(oi => oi.Order) // Include the parent Order to check status and member
                        .Where(oi => oi.BookId == id && // Match the current book
                                     oi.Order.MemberId == memberId && // Match the logged-in user
                                     cancellableStatuses.Contains(oi.Order.OrderStatus)) // Match cancellable statuses
                        .Select(oi => new { oi.OrderItemId }) // Select only the ID
                        .FirstOrDefaultAsync();

                     Debug.WriteLine($"Query for cancellable order item executed."); // Debugging line

                    // If a cancellable order item is found, pass its ID to the view
                    if (cancellableOrderItem != null)
                    {
                         Debug.WriteLine($"Cancellable OrderItem found! ID: {cancellableOrderItem.OrderItemId}"); // Debugging line
                        ViewBag.CancellableOrderItemId = cancellableOrderItem.OrderItemId;
                    }
                    else
                    {
                         Debug.WriteLine("No cancellable OrderItem found for this user and book."); // Debugging line
                        ViewBag.CancellableOrderItemId = null; // Explicitly set to null if not found
                    }

                 // --- Check if the book is bookmarked by the current user ---
                 // Ensure memberId is available before checking bookmarks
                 var isBookmarked = await _context.Bookmarks
                     .AnyAsync(b => b.MemberId == memberId && b.BookId == id);
                 ViewBag.IsBookmarked = isBookmarked;
                 Debug.WriteLine($"Bookmarked status for MemberId {memberId} and Book ID {id}: {isBookmarked}"); // Debugging line

                }
                else
                {
                     Debug.WriteLine("Failed to parse MemberId claim."); // Debugging line
                }
            }
            else
            {
                 Debug.WriteLine("User is not authenticated. Skipping order item and bookmark checks."); // Debugging line
                 ViewBag.CancellableOrderItemId = null; // Ensure ViewBag is null if not authenticated
                 ViewBag.IsBookmarked = false; // Ensure ViewBag is false if not authenticated
            }
            // --- End Check for Cancellable Order Item and Bookmark ---


            return View(book);
        }

        // GET: Book/Create
        public IActionResult Create()
        {
            // Add null checks for Author properties when creating SelectList (Fixes CS8602 warning)
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).Select(a => new { a.AuthorId, FullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim() }), "AuthorId", "FullName");
            ViewData["GenreId"] = new SelectList(_context.Genres.OrderBy(g => g.Name), "GenreId", "Name");
            ViewData["PublisherId"] = new SelectList(_context.Publishers.OrderBy(p => p.Name), "PublisherId", "Name");
            return View();
        }

        // POST: Book/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,Isbn,Title,Description,CoverImageUrl,PublicationDate,ListPrice,AuthorId,PublisherId,GenreId,Language,Format,AvailabilityStock,AvailabilityLibrary,Rating,RatingCount,OnSale,SaleDiscount,SaleStartDate,SaleEndDate,DateAdded,DateUpdated")] Book book)
        {
            if (ModelState.IsValid)
            {
                // Set DateAdded and DateUpdated when creating a new book
                book.DateAdded = DateTime.Now;
                book.DateUpdated = DateTime.Now;
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Add null checks for Author properties when creating SelectList (Fixes CS8602 warning)
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).Select(a => new { a.AuthorId, FullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim() }), "AuthorId", "FullName", book.AuthorId);
            ViewData["GenreId"] = new SelectList(_context.Genres.OrderBy(g => g.Name), "GenreId", "Name", book.GenreId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers.OrderBy(p => p.Name), "PublisherId", "Name", book.PublisherId);
            return View(book);
        }

        // GET: Book/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            // Add null checks for Author properties when creating SelectList (Fixes CS8602 warning)
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).Select(a => new { a.AuthorId, FullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim() }), "AuthorId", "FullName", book.AuthorId);
            ViewData["GenreId"] = new SelectList(_context.Genres.OrderBy(g => g.Name), "GenreId", "Name", book.GenreId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers.OrderBy(p => p.Name), "PublisherId", "Name", book.PublisherId);
            return View(book);
        }

        // POST: Book/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookId,Isbn,Title,Description,CoverImageUrl,PublicationDate,ListPrice,AuthorId,PublisherId,GenreId,Language,Format,AvailabilityStock,AvailabilityLibrary,Rating,RatingCount,OnSale,SaleDiscount,SaleStartDate,SaleEndDate,DateAdded,DateUpdated")] Book book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update DateUpdated when editing
                    book.DateUpdated = DateTime.Now;
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
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
            // Add null checks for Author properties when creating SelectList (Fixes CS8602 warning)
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).Select(a => new { a.AuthorId, FullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim() }), "AuthorId", "FullName", book.AuthorId);
            ViewData["GenreId"] = new SelectList(_context.Genres.OrderBy(g => g.Name), "GenreId", "Name", book.GenreId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers.OrderBy(p => p.Name), "PublisherId", "Name", book.PublisherId);
            return View(book);
        }

        // GET: Book/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Book/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }

        // --- Bookmark Action ---
        // This action allows an authenticated member to bookmark a book.
        [HttpPost] // Use POST for actions that change data
        [Authorize] // Requires the user to be authenticated
        [ValidateAntiForgeryToken] // Good practice for POST requests from views
        public async Task<IActionResult> Bookmark(int bookId)
        {
            // Get the authenticated user's MemberId (as a string) from the claims.
            // ClaimTypes.NameIdentifier stores the MemberId (the integer primary key) from the login process.
            var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Determine the return URL. Prioritize the Referer header if available and local.
            // Otherwise, default to the Index page.
            var returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action(nameof(Index), "Book"); // Default to Index
            }

            // Check if the MemberId claim is present and can be parsed as an integer.
            if (string.IsNullOrEmpty(memberIdString) || !int.TryParse(memberIdString, out int memberId))
            {
                // If the claim is missing or invalid, it indicates an authentication issue.
                TempData["Message"] = "Authentication failed. Please log in again.";
                // Consider signing out the user if the identity seems invalid
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Use constant for scheme
                // Redirect to the Login action in the Member controller using string literals
                return RedirectToAction("Login", "Member");
            }

            // Find the member using the MemberId (the primary key) obtained from the claims.
            // This is the correct way to identify the authenticated member in the database.
            var member = await _context.Members.FindAsync(memberId);

            if (member == null)
            {
                // Member not found in the database for the given MemberId from claims.
                // This is a serious issue indicating a potential database problem or data inconsistency.
                TempData["Message"] = "Error: Your member account data could not be found in the database.";
                // Consider logging this error internally for debugging.
                // Consider signing out the user as their authenticated identity doesn't match a database record.
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Use constant for scheme
                return RedirectToAction("Login", "Member");
            }

            // Check if the book exists.
            var book = await _context.Books.FindAsync(bookId); // FindAsync is efficient for primary keys
            if (book == null)
            {
                TempData["Message"] = $"Error: Book with ID {bookId} not found.";
                return Redirect(returnUrl);
            }

            // Check if the bookmark already exists for this member and book using AnyAsync for efficiency.
            var existingBookmark = await _context.Bookmarks
                .AnyAsync(b => b.MemberId == member.MemberId && b.BookId == bookId);

            if (existingBookmark)
            {
                // Bookmark already exists.
                 TempData["Message"] = "This book is already bookmarked.";
                 return Redirect(returnUrl);
            }

            // Create the new bookmark.
            var newBookmark = new Bookmark
            {
                MemberId = member.MemberId, // Use the MemberId from the found member
                BookId = bookId,
                DateAdded = DateTime.Now, // Set the date added
                // FIXED: Explicitly set the required navigation properties
                Member = member,
                Book = book
            };

            // Add the bookmark to the database context.
            _context.Bookmarks.Add(newBookmark);

            // Save changes to the database.
            await _context.SaveChangesAsync();

            // Indicate success and redirect back to the origin page.
             TempData["Message"] = "Book bookmarked successfully!";
             return Redirect(returnUrl);
        }

        // --- End Bookmark Action ---


        // --- AddToCart Action ---
        // This action allows an authenticated member to add a book to their shopping cart.
        [HttpPost] // Use POST for actions that change data
        [Authorize] // Requires the user to be authenticated
        [ValidateAntiForgeryToken] // Good practice for POST requests from views
        public async Task<IActionResult> AddToCart(int bookId, int quantity = 1) // Default quantity to 1
        {
            // Get the authenticated user's MemberId from the claims.
            var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Determine the return URL, similar to the Bookmark action.
            var returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action(nameof(Index), "Book"); // Default to Index
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

            // Check if the book is already in the member's shopping cart.
            var cartItem = await _context.ShoppingCartItems
                .FirstOrDefaultAsync(item => item.MemberId == member.MemberId && item.BookId == bookId);

            if (cartItem == null)
            {
                // Item is not in the cart, create a new one.
                cartItem = new ShoppingCartItem
                {
                    MemberId = member.MemberId,
                    BookId = bookId,
                    Quantity = quantity,
                    DateAdded = DateTime.Now,
                    // Explicitly set navigation properties if needed for EF Core tracking
                    Member = member,
                    Book = book
                };
                _context.ShoppingCartItems.Add(cartItem);
                TempData["Message"] = $"{book.Title} added to your cart.";
            }
            else
            {
                // Item is already in the cart, update the quantity.
                cartItem.Quantity += quantity;
                cartItem.DateAdded = DateTime.Now; // Optionally update date added on quantity change
                _context.Update(cartItem);
                TempData["Message"] = $"Quantity for {book.Title} updated in your cart.";
            }

            // Save changes to the database.
            await _context.SaveChangesAsync();

            // Redirect back to the origin page.
            return Redirect(returnUrl);
        }
        // --- End AddToCart Action ---

    }
}
