using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace FinalProject.Controllers
{
    public class BookmarkController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookmarkController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bookmark
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Bookmarks.Include(b => b.Book).Include(b => b.Member);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Bookmark/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookmark = await _context.Bookmarks
                .Include(b => b.Book)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(m => m.BookmarkId == id);
            if (bookmark == null)
            {
                return NotFound();
            }

            return View(bookmark);
        }

        // GET: Bookmark/Create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn");
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email");
            return View();
        }

        // POST: Bookmark/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookmarkId,MemberId,BookId,DateAdded")] Bookmark bookmark)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bookmark);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", bookmark.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email", bookmark.MemberId);
            return View(bookmark);
        }

        // GET: Bookmark/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookmark = await _context.Bookmarks.FindAsync(id);
            if (bookmark == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", bookmark.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email", bookmark.MemberId);
            return View(bookmark);
        }

        // POST: Bookmark/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookmarkId,MemberId,BookId,DateAdded")] Bookmark bookmark)
        {
            if (id != bookmark.BookmarkId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bookmark);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookmarkExists(bookmark.BookmarkId))
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
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", bookmark.BookId);
            ViewData["MemberId"] = new SelectList(_context.Members, "MemberId", "Email", bookmark.MemberId);
            return View(bookmark);
        }
        

        // GET: Bookmark/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookmark = await _context.Bookmarks
                .Include(b => b.Book)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(m => m.BookmarkId == id);
            if (bookmark == null)
            {
                return NotFound();
            }

            return View(bookmark);
        }

        // POST: Bookmark/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bookmark = await _context.Bookmarks.FindAsync(id);
            if (bookmark != null)
            {
                _context.Bookmarks.Remove(bookmark);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // GET: Bookmark/MyBookmarks
        // This method lists the books bookmarked by the currently logged-in user.
[Authorize]
        public async Task<IActionResult> MyBookmarks()
{
    Console.WriteLine("--- MyBookmarks Action Start ---"); // Debugging line

    // 1. Get the ID of the logged-in user.
    // The user's ID is typically stored in the NameIdentifier claim.
    Console.WriteLine("1. Attempting to get MemberId from claims.");
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

    // Check if the user is authenticated and the claim exists and is valid
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
    {
        Console.WriteLine("2. MemberId claim missing or invalid. Redirecting to Login.");
        // User is not logged in or user ID claim is missing/invalid.
        // Redirect to the login page or return an Unauthorized result.
        // Assuming your login path is /Member/Login based on Program.cs
        // Log this event
        // _logger.LogWarning("MyBookmarks: Unauthorized access attempt or invalid MemberId claim.");
        return RedirectToAction("Login", "Member");
        // Alternatively, return Unauthorized();
    }
    Console.WriteLine($"3. Successfully retrieved MemberId: {userId}");

    // 2. Query bookmarks for the logged-in user, including the related Book and Author data.
    Console.WriteLine($"4. Querying bookmarks for MemberId: {userId}");
    var bookmarks = await _context.Bookmarks
        .Where(b => b.MemberId == userId) // Filter by the logged-in user's ID
        .Include(b => b.Book)             // Include the Book entity
            .ThenInclude(book => book.Author) // Include the Author for the Book
        .ToListAsync();                   // Execute the query asynchronously
    Console.WriteLine($"5. Finished querying bookmarks. Found {bookmarks.Count} bookmarks.");

    // 3. Map the fetched data to a list of ViewModel objects using the provided BookmarkViewModel.
    Console.WriteLine("6. Mapping bookmarks to BookmarkViewModel list.");
    var viewModelList = new List<BookmarkViewModel>();

    foreach (var bookmark in bookmarks)
    {
        // Basic check to ensure the Book navigation property was loaded
        if (bookmark.Book != null)
        {
             viewModelList.Add(new BookmarkViewModel
            {
                BookmarkId = bookmark.BookmarkId, // Map Bookmark ID
                MemberId = bookmark.MemberId, // Map Member ID
                BookId = bookmark.BookId,
                BookTitle = bookmark.Book.Title,
                BookAuthorName = bookmark.Book.Author?.FullName ?? "Unknown Author", // Handle potential null Author
                DateAddedDisplay = bookmark.DateAdded.ToLocalTime().ToString("g") // Map and format the bookmark date
                // Note: BookListPrice is not in the provided BookmarkViewModel
            });
        }
        else
        {
            // Log a warning if a bookmark exists but the associated book is missing
            // _logger.LogWarning("MyBookmarks: Bookmark ID {BookmarkId} found for MemberId {MemberId}, but associated Book is null.", bookmark.BookmarkId, userId);
            Console.WriteLine($"Warning: Bookmark ID {bookmark.BookmarkId} has a null Book."); // Debugging line
        }
    }
    Console.WriteLine($"7. Finished mapping to ViewModel list. Created {viewModelList.Count} ViewModels.");

    // 4. Pass the list of ViewModel objects to the view.
    // You will need to create a corresponding View file named MyBookmarks.cshtml
    // in the Views/Bookmark folder to display this list of BookmarkViewModel objects.
    Console.WriteLine("8. Returning View with ViewModel list.");
    Console.WriteLine("--- MyBookmarks Action End ---"); // Debugging line
    return View(viewModelList);
}


        private bool BookmarkExists(int id)
        {
            return _context.Bookmarks.Any(e => e.BookmarkId == id);
        }
    }
}
