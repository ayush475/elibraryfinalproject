using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;

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
                // You can add more fields to search if needed
                books = books.Where(b => b.Title.Contains(searchString)
                                       || (b.Author != null && b.Author.FirstName.Contains(searchString))
                                       || (b.Author != null && b.Author.LastName.Contains(searchString))
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
            ViewBag.Authors = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName), "AuthorId", "FirstName", authorId); // You might want to display full name later


            // Return the paginated, filtered, and ordered list of books to the view
            return View(paginatedBooks);
        }

        // GET: Book/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Book/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName), "AuthorId", "FirstName");
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
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName), "AuthorId", "FirstName", book.AuthorId);
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
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName), "AuthorId", "FirstName", book.AuthorId);
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
            ViewData["AuthorId"] = new SelectList(_context.Authors.OrderBy(a => a.LastName).ThenBy(a => a.FirstName), "AuthorId", "FirstName", book.AuthorId);
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
    }
}
