using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;

namespace FinalProject.Controllers
{
    public class GenresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GenresController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Genres
        // Displays a list of all genres in a view.
        public async Task<IActionResult> Index()
        {
            return View(await _context.Genres.ToListAsync());
        }

        // GET: Genres/Details/5
        // Displays the details of a single genre.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(m => m.GenreId == id);
            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }

        // GET: Genres/Create
        // Displays the form for creating a new genre.
        public IActionResult Create()
        {
            return View();
        }

        // POST: Genres/Create
        // Handles the form submission for creating a new genre.
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GenreId,Name,Description,DateAdded,DateUpdated")] Genre genre)
        {
            if (ModelState.IsValid)
            {
                 // Set creation and update dates (assuming these are managed by the app)
                genre.DateAdded = DateTime.Now;
                genre.DateUpdated = DateTime.Now;
                _context.Add(genre);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(genre);
        }

        // GET: Genres/Edit/5
        // Displays the form for editing an existing genre.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }

        // POST: Genres/Edit/5
        // Handles the form submission for editing an existing genre.
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GenreId,Name,Description,DateAdded,DateUpdated")] Genre genre)
        {
            if (id != genre.GenreId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the update date
                    genre.DateUpdated = DateTime.Now;
                    _context.Update(genre);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GenreExists(genre.GenreId))
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
            return View(genre);
        }

        // GET: Genres/Delete/5
        // Displays the confirmation page for deleting a genre.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres
                .FirstOrDefaultAsync(m => m.GenreId == id);
            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }

        // POST: Genres/Delete/5
        // Handles the confirmation and deletion of a genre.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre != null)
            {
                _context.Genres.Remove(genre);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if a genre exists.
        private bool GenreExists(int id)
        {
            return _context.Genres.Any(e => e.GenreId == id);
        }

        // --- New Action to List Genres for Dropdown ---

        /// <summary>
        /// Gets a list of all genres, ordered by name, suitable for populating a dropdown.
        /// Returns the list as a JSON result.
        /// </summary>
        // GET: Genres/GetGenresForDropdown
        [HttpGet] // Explicitly specify this responds to GET requests
        public async Task<IActionResult> GetGenresForDropdown()
        {
            var genres = await _context.Genres
                                     .OrderBy(g => g.Name)
                                     .ToListAsync();

            return Ok(genres);
        }

        // --- End New Action ---
    }
}
