using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Claims; // Added for ClaimsPrincipal and ClaimTypes
using Microsoft.AspNetCore.Authentication; // Added for HttpContext.SignOutAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Added for CookieAuthenticationDefaults
using System.Diagnostics; // Added for Debug.WriteLine

namespace FinalProject.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reviews
        // Displays a list of all reviews. Includes Book and Member navigation properties.
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Reviews.Include(r => r.Book).Include(r => r.Member);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Reviews/Details/5
        // Displays the details of a specific review. Includes Book and Member navigation properties.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(m => m.ReviewId == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // GET: Reviews/Create
        // Displays the form to create a new review.
        // Note: This GET Create action might not be directly used if reviews are
        // created via a form on the Book Details page, but it's kept for completeness.
        public IActionResult Create()
        {
            // Populate dropdown lists for Book and Member.
            // For a real application, you might filter these lists or populate them differently.
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Title"); // Changed "Isbn" to "Title" for display
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "UserName"); // Changed "Email" to "UserName" for display
            return View();
        }

        // POST: Reviews/Create
        // This is the action that handles the submission of the review form,
        // including validation for purchase history and duplicate reviews.
        [HttpPost]
        [Authorize] 
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Create([Bind("BookId,Rating,Comment")] Review review)
        {
            // Debugging line to trace action start.
            Debug.WriteLine($"ReviewsController.Create action called for Book ID: {review.BookId}");

            // 1. Get the authenticated user's MemberId from their claims.
            // ClaimTypes.NameIdentifier is typically used to store the user's unique ID (like the primary key).
            var memberIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Debug.WriteLine($"MemberId claim string: {memberIdString}");

            // Determine the return URL. Prioritize the Referer header if available and local.
            // Otherwise, default to the Book Details page for the book being reviewed.
            // This is where the user likely came from when submitting the review.
            var returnUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                // Default fallback if Referer is missing or external.
                returnUrl = Url.Action("Details", "Book", new { id = review.BookId }); // Redirect back to the book details page
                if (returnUrl == null) // Fallback if generating the URL fails
                {
                     returnUrl = "/"; // Go to home page
                }
            }

           
            if (string.IsNullOrEmpty(memberIdString) || !int.TryParse(memberIdString, out int memberId))
            {
                TempData["Message"] = "Authentication failed. Please log in again.";
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); 
                return RedirectToAction("Login", "Member"); 
            }

            review.MemberId = memberId;

            var hasPurchased = await HasUserPurchasedBookAsync(review.MemberId, review.BookId);
            Debug.WriteLine($"User has purchased book check result: {hasPurchased}");

            if (!hasPurchased)
            {
                // If the user has NOT purchased the book, prevent review submission.
                // Add a model error (for validation summary) and use TempData (for redirection message).
                ModelState.AddModelError("", "You can only review books you have purchased.");
                TempData["Message"] = "You can only review books you have purchased.";

                // Redirect the user back to the page they came from (likely book details).
                return Redirect(returnUrl);
            }


            var existingReview = await _context.Reviews
                .AnyAsync(r => r.MemberId == review.MemberId && r.BookId == review.BookId);
            Debug.WriteLine($"Existing review check result: {existingReview}");

            if (existingReview)
            {
                // Handle case where the user has already reviewed this book.
                ModelState.AddModelError("", "You have already reviewed this book.");
                TempData["Message"] = "You have already reviewed this book."; 

                // Redirect the user back to the page they came from.
                return Redirect(returnUrl);
            }

            
            if (ModelState.IsValid)
            {
                review.ReviewDate = DateTime.Now;
                review.DateUpdated = DateTime.Now; 

                
                _context.Add(review);
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Review saved successfully for Book ID: {review.BookId}");

                await UpdateBookRatingAsync(review.BookId);
                Debug.WriteLine($"Book rating updated for Book ID: {review.BookId}");

                TempData["Message"] = "Your review has been submitted successfully!";
                return RedirectToAction("Details", "Book", new { id = review.BookId });
            } 
            Debug.WriteLine("ModelState is invalid. Review not saved.");
            TempData["Message"] = "Review submission failed. Please check your rating and comment."; 

            return Redirect(returnUrl);
        }


        // GET: Reviews/Edit/5
        // Displays the form to edit an existing review.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }
            // Populate dropdown lists for Book and Member (pre-selecting current values).
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Title", review.BookId); // Changed "Isbn" to "Title"
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "UserName", review.MemberId); // Changed "Email" to "UserName"
            return View(review);
        }

        // POST: Reviews/Edit/5
        // Handles the submission of the edited review form.
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewId,MemberId,BookId,Rating,Comment,ReviewDate,DateUpdated")] Review review)
        {
            if (id != review.ReviewId)
            {
                return NotFound();
            }

            // Note: You might want to add authorization here to ensure only the review author can edit.
            // Also, consider if MemberId and BookId should be editable.

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the DateUpdated timestamp.
                    review.DateUpdated = DateTime.Now;
                    _context.Update(review);
                    await _context.SaveChangesAsync();

                    // Optional: Re-update the book rating after an edit
                    await UpdateBookRatingAsync(review.BookId);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.ReviewId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // Redirect back to the review details or index page.
                return RedirectToAction(nameof(Index));
            }
            // If ModelState is invalid, return the view with the review object to show errors.
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Title", review.BookId); // Changed "Isbn" to "Title"
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "UserName", review.MemberId); // Changed "Email" to "UserName"
            return View(review);
        }

        // GET: Reviews/Delete/5
        // Displays the confirmation page for deleting a review.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(m => m.ReviewId == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        // Handles the deletion of a review after confirmation.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                // Store BookId before removing the review to update the book rating later.
                var bookId = review.BookId;
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                // Optional: Update the book rating after deleting a review.
                await UpdateBookRatingAsync(bookId);
            }

            // Redirect back to the reviews index page.
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if a review exists by its ID.
        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewId == id);
        }

         // --- Helper Method to Check Purchase Status ---
        /// <summary>
        /// Checks if a member has purchased a specific book by looking for corresponding order items.
        /// This is a crucial server-side check.
        /// </summary>
        /// <param name="memberId">The ID of the member.</param>
        /// <param name="bookId">The ID of the book.</param>
        /// <returns>True if the member has purchased the book, false otherwise.</returns>
        private async Task<bool> HasUserPurchasedBookAsync(int memberId, int bookId)
        {
            // Query the database to see if there's any OrderItem for this book
            // that belongs to an Order placed by this member.
            var hasPurchased = await _context.OrderItems
                .AnyAsync(oi => oi.BookId == bookId && oi.Order.MemberId == memberId);

            return hasPurchased;
        }

        // --- Optional Helper Method to Update Book Rating ---
        /// <summary>
        /// Recalculates and updates the average rating and rating count for a book.
        /// </summary>
        /// <param name="bookId">The ID of the book to update.</param>
        private async Task UpdateBookRatingAsync(int bookId)
        {
            // Fetch all reviews for the book
            var reviews = await _context.Reviews
                .Where(r => r.BookId == bookId)
                .ToListAsync();

            // Find the book to update
            var book = await _context.Books.FindAsync(bookId);

            if (book != null)
            {
                if (reviews.Any())
                {
                    // Calculate the new average rating
                    var averageRating = reviews.Average(r => r.Rating);

                    // Update the book's rating and count
                    book.Rating = (decimal)averageRating; // Cast double average to decimal
                    book.RatingCount = reviews.Count;
                }
                else
                {
                    // If no reviews, reset rating and count (optional, depends on desired behavior)
                    book.Rating = null; // Or 0.0m
                    book.RatingCount = 0;
                }

                // Mark the book as modified and save changes
                _context.Update(book);
                await _context.SaveChangesAsync();
            }
        }
    }
}
