using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data; 
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims; 
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies; 
using System.Diagnostics; 

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
            int pageSize = 10; // Items per page

            // 1. Build the base query with eager loading.
            var baseQuery = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Publisher)
                .AsQueryable(); // Use AsQueryable() to build the query

            // 2. Apply filters and ordering using a helper method.
            var filteredAndOrderedQuery = ApplyFilteringAndOrdering(baseQuery, searchString, genreId, authorId);

            // 3. Get total item count for pagination (executes a COUNT query).
            var totalItems = await filteredAndOrderedQuery.CountAsync();

            // 4. Calculate pagination details.
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            int currentPage = (pageNumber ?? 1);
            // Ensure current page is within valid bounds
             if (currentPage < 1) { currentPage = 1; }
             else if (totalPages > 0 && currentPage > totalPages) { currentPage = totalPages; }
             else if (totalPages == 0) { currentPage = 1; }


            // 5. Apply pagination (Skip/Take) and execute the main query.
            var paginatedBooks = await filteredAndOrderedQuery
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(); // Executes the main SELECT query

            // 6. Fetch bookmarked book IDs for the current user using a helper method.
            var bookmarkedBookIds = await GetBookmarkedBookIdsAsync(User);

            // 7. Populate dropdown lists for filters.
            // Create SelectList objects needed for the view's dropdown helpers.
             var genreList = new SelectList(_context.Genres.OrderBy(g => g.Name), "GenreId", "Name", genreId);
             // Note: The creation of anonymous type for Author FullName remains here as it's specific to the SelectList creation logic.
             var authorList = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName).Select(a => new { a.AuthorId, FullName = $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim() }), "AuthorId", "FullName", authorId);


            // 8. Set up ViewBag data for the view using a helper method.
            SetupIndexViewData(searchString, genreId, authorId, currentPage, totalPages, pageSize, bookmarkedBookIds, genreList, authorList);

            // 9. Return the view with the paginated list of books.
            return View(paginatedBooks);
        }

        

        // --- Helper Methods for Index Action ---

        /// <summary>
        /// Applies search, genre, and author filters, and default ordering to an IQueryable of Books.
        /// </summary>
        /// <param name="baseQuery">The starting IQueryable of Books.</param>
        /// <param name="searchString">The search term.</param>
        /// <param name="genreId">Optional Genre ID filter.</param>
        /// <param name="authorId">Optional Author ID filter.</param>
        /// <returns>The modified IQueryable of Books.</returns>
        private IQueryable<Book> ApplyFilteringAndOrdering(IQueryable<Book> baseQuery, string searchString, int? genreId, int? authorId)
        {
            var query = baseQuery;

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Title.Contains(searchString)
                                       || (b.Author != null && b.Author.FirstName != null && b.Author.FirstName.Contains(searchString))
                                       || (b.Author != null && b.Author.LastName != null && b.Author.LastName.Contains(searchString))
                                       || (b.Isbn != null && b.Isbn.Contains(searchString)));
            }

            // Apply Genre filter
            if (genreId.HasValue && genreId.Value > 0)
            {
                query = query.Where(b => b.GenreId == genreId.Value);
            }

            // Apply Author filter
            if (authorId.HasValue && authorId.Value > 0)
            {
                query = query.Where(b => b.AuthorId == authorId.Value);
            }

            // Add ordering - Order by DateAdded in descending order
            query = query.OrderByDescending(b => b.DateAdded);

            return query;
        }

        /// <summary>
        /// Fetches the IDs of books bookmarked by the current authenticated user.
        /// </summary>
        /// <param name="user">The current user principal (ClaimsPrincipal).</param>
        /// <returns>A list of bookmarked Book IDs, or an empty list if not authenticated or an error occurs.</returns>
        private async Task<List<int>> GetBookmarkedBookIdsAsync(ClaimsPrincipal user)
        {
            var bookmarkedBookIds = new List<int>();

            if (user?.Identity?.IsAuthenticated == true)
            {
                var memberIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(memberIdString) && int.TryParse(memberIdString, out int memberId))
                {
                    bookmarkedBookIds = await _context.Bookmarks
                        .Where(b => b.MemberId == memberId)
                        .Select(b => b.BookId)
                        .ToListAsync();
                }
                // Optional: Log if MemberId claim is missing or invalid despite authentication
                 // else { Debug.WriteLine("Authenticated user has missing or invalid MemberId claim."); }
            }
             // Optional: Log if user is not authenticated when trying to get bookmarks
             // else { Debug.WriteLine("User is not authenticated. Skipping bookmark fetch."); }


            return bookmarkedBookIds;
        }

        /// <summary>
        /// Sets up ViewBag properties with data needed by the Index view.
        /// </summary>
        private void SetupIndexViewData(string searchString, int? genreId, int? authorId, int currentPage, int totalPages, int pageSize, List<int> bookmarkedBookIds, SelectList genreList, SelectList authorList)
        {
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchString = searchString;
            ViewBag.SelectedGenreId = genreId;
            ViewBag.SelectedAuthorId = authorId;
            ViewBag.BookmarkedBookIds = bookmarkedBookIds; // Pass the fetched bookmarked IDs

            ViewBag.Genres = genreList; // Pass the prepared SelectList for genres
            ViewBag.Authors = authorList; // Pass the prepared SelectList for authors
        }


        // --- End Helper Methods for Index Action ---
        // GET: Book/Details/5
        // GET: Book/Details/5
// Refactored Details action using a helper method for user-specific checks.
/// <summary>
/// Performs user-specific checks (cancellable order item, bookmark status, purchased status) for a book
/// and sets relevant ViewBag properties for the Details view.
/// </summary>
/// <param name="bookId">The ID of the book being viewed.</param>
/// <param name="user">The current user principal (ClaimsPrincipal).</param>
/// <param name="viewBag">The ViewBag object to populate.</param>
/// <summary>
/// Performs user-specific checks (cancellable order item, bookmark status, purchased status) for a book
/// and sets relevant ViewBag properties for the Details view.
/// </summary>
/// <param name="bookId">The ID of the book being viewed.</param>
/// <param name="user">The current user principal (ClaimsPrincipal).</param>
/// <param name="viewBag">The ViewBag object to populate.</param>
private async Task GetAndSetUserSpecificBookViewDataAsync(int bookId, ClaimsPrincipal user, dynamic viewBag)
{
    // Debugging line to trace helper execution.
    Debug.WriteLine($"Checking user-specific data for Book ID: {bookId}");

    // 1. Set Default ViewBag Values:
    // Initialize ViewBag properties to default values (null/false).
    // This ensures these properties exist in the ViewBag even if the user isn't authenticated
    // or the checks don't find any relevant data.
    viewBag.CancellableOrderItemId = null;
    viewBag.IsBookmarked = false;
    viewBag.HasPurchased = false; // Initialize the new property

    // 2. Check if User is Authenticated:
    // Proceed with user-specific checks only if the user is logged in.
    if (user?.Identity?.IsAuthenticated == true)
    {
        // 3. Get and Parse Member ID from Claims:
        // Retrieve the user's unique identifier (MemberId) from their authentication claims.
        var memberIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Debug.WriteLine($"MemberId claim string: {memberIdString}");

        // Safely attempt to parse the claim value into an integer MemberId.
        if (int.TryParse(memberIdString, out int memberId))
        {
            Debug.WriteLine($"Successfully parsed MemberId: {memberId}");

            // --- Check for Cancellable Order Item ---
            // 4. Define Cancellable Order Statuses:
            // An array listing the order statuses that allow cancellation.
            var cancellableStatuses = new[] { "Pending", "Placed" };
            Debug.WriteLine($"Cancellable statuses: {string.Join(", ", cancellableStatuses)}");

            // 5. Query for Cancellable Order Item:
            // Search the OrderItems for one that matches:
            // - The current book (using the passed bookId).
            // - The logged-in user (by checking the parent Order's MemberId).
            // - A cancellable OrderStatus (by checking the parent Order's status).
            // .Include(oi => oi.Order) is needed to access Order properties.
            // .Select(oi => new { oi.OrderItemId }) projects to just the ID for efficiency.
            // .FirstOrDefaultAsync() gets the first match or null.
            var cancellableOrderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.BookId == bookId &&
                             oi.Order.MemberId == memberId &&
                             cancellableStatuses.Contains(oi.Order.OrderStatus))
                .Select(oi => new { oi.OrderItemId })
                .FirstOrDefaultAsync();

            Debug.WriteLine($"Query for cancellable order item executed.");

            // 6. Set ViewBag for Cancellable Order Item:
            // If a cancellable item was found, set the ViewBag property with its ID.
            if (cancellableOrderItem != null)
            {
                Debug.WriteLine($"Cancellable OrderItem found! ID: {cancellableOrderItem.OrderItemId}");
                viewBag.CancellableOrderItemId = cancellableOrderItem.OrderItemId;
            }
            else
            {
                Debug.WriteLine("No cancellable OrderItem found for this user and book.");
            }

            // --- Check if the book is bookmarked ---
            // 7. Check if the Book is Bookmarked:
            // Efficiently check if a Bookmark exists for this user and book.
            // .AnyAsync() returns true if at least one matching record is found, false otherwise.
            var isBookmarked = await _context.Bookmarks
                .AnyAsync(b => b.MemberId == memberId && b.BookId == bookId); // Use the passed bookId

            // 8. Set ViewBag for Bookmark Status:
            // Set the ViewBag property to true or false based on whether a bookmark exists.
            viewBag.IsBookmarked = isBookmarked;
            Debug.WriteLine($"Bookmarked status for MemberId {memberId} and Book ID {bookId}: {isBookmarked}");

            // --- NEW: Check if the user has purchased this book ---
            // Query the database to see if there's any OrderItem for this book
            // that belongs to an Order placed by this member.
            var hasPurchased = await _context.OrderItems
                .AnyAsync(oi => oi.BookId == bookId && oi.Order.MemberId == memberId);

            // Set the ViewBag property for the purchase status.
            viewBag.HasPurchased = hasPurchased;
            Debug.WriteLine($"Purchase status for Member ID {memberId} and Book ID {bookId}: {hasPurchased}");
            // --- End NEW: Check if the user has purchased this book ---

        }
        else
        {
            Debug.WriteLine("Failed to parse MemberId claim for authenticated user.");
             // This could indicate an issue with the authentication process or claims setup.
        }
    }
    else
    {
         Debug.WriteLine("User is not authenticated. Skipping user-specific checks.");
         // Default ViewBag values (set at the start) remain if not authenticated.
    }
    // The helper method finishes here. It doesn't return a value (Task).
    // Its purpose is to modify the ViewBag object passed to it.
}
// The Details action remains the same as it already calls the helper:

        public async Task<IActionResult> Details(int? id)
        {
            // Debugging line to trace when the action is called.
            Debug.WriteLine($"BookController.Details action called for Book ID: {id}");

            // 1. Check if ID is Provided:
            if (id == null)
            {
                Debug.WriteLine("Book ID is null. Returning NotFound.");
                return NotFound();
            }

            // 2. Fetch the Book from the Database:
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Publisher)
                // *** Include Reviews if you want to display existing reviews on the same page ***
                // Make sure to include Reviews here if your view displays them directly from the book model
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(m => m.BookId == id);

            // 3. Check if the Book was Found:
            if (book == null)
            {
                Debug.WriteLine($"Book with ID {id} not found. Returning NotFound.");
                return NotFound();
            }
            Debug.WriteLine($"Book found: {book.Title} (ID: {book.BookId})");

            // --- Get and Set User-Specific View Data using Helper ---
            // This helper now includes the purchase check and sets ViewBag.HasPurchased
            await GetAndSetUserSpecificBookViewDataAsync(book.BookId, User, ViewBag);
            // --- End Get and Set User-Specific View Data ---

            // 5. Return the View:
            // The view will use the book model and the data set in ViewBag by the helper,
            // including the ViewBag.HasPurchased value.
            return View(book);
        }

// --- End Helper Methods for Details Action ---

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
        returnUrl = Url.Action(nameof(Index), "Book"); // Default to Index (assuming an Index action in BookController)
    }

    // Check if the MemberId claim is present and can be parsed as an integer.
    if (string.IsNullOrEmpty(memberIdString) || !int.TryParse(memberIdString, out int memberId))
    {
        // If the claim is missing or invalid, it indicates an authentication issue.
        TempData["Message"] = "Authentication failed. Please log in again.";
        // Consider signing out the user if the identity seems invalid
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Use constant for scheme
        // Redirect to the Login action in the Member controller using string literals
        return RedirectToAction("Login", "Member"); // Adjust "Login" and "Member" as necessary
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
        return RedirectToAction("Login", "Member"); // Adjust "Login" and "Member" as necessary
    }

    // Check if the book exists.
    var book = await _context.Books.FindAsync(bookId); // FindAsync is efficient for primary keys
    if (book == null)
    {
        TempData["Message"] = $"Error: Book with ID {bookId} not found.";
        return Redirect(returnUrl);
    }

    // --- Bookmark Toggle Logic ---

    // Find the specific bookmark for this member and book.
    // Use FirstOrDefaultAsync to get the entity if it exists, or null otherwise.
    var existingBookmark = await _context.Bookmarks
        .FirstOrDefaultAsync(b => b.MemberId == member.MemberId && b.BookId == bookId);

    if (existingBookmark != null)
    {
        // Bookmark exists, so remove it.
        _context.Bookmarks.Remove(existingBookmark);
        await _context.SaveChangesAsync();
        TempData["Message"] = "Bookmark removed successfully.";
    }
    else
    {
        // Bookmark does not exist, so add it.
        var newBookmark = new Bookmark
        {
            MemberId = member.MemberId, // Use the MemberId from the found member
            BookId = bookId,
            DateAdded = DateTime.Now, // Set the date added
            // As per your model definition with 'required' navigation properties,
            // you must set these when creating a new Bookmark entity.
            Member = member,
            Book = book
        };

        _context.Bookmarks.Add(newBookmark);
        await _context.SaveChangesAsync();
        TempData["Message"] = "Book bookmarked successfully!";
    }

    // Redirect back to the origin page.
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

            return Redirect(returnUrl);
        }
        
      

    }
}
