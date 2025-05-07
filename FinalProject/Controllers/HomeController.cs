using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FinalProject.Models;
using FinalProject.Data; // Required to access ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Required for Entity Framework Core methods like FirstOrDefaultAsync

namespace FinalProject.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context; // Add a field for the database context

    // Modify the constructor to inject ApplicationDbContext
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context; // Assign the injected context
    }

    // Modify the Index action to fetch and pass the announcement
    public async Task<IActionResult> Index(string searchString, int? pageNumber) // Added parameters for potential book search/pagination
    {
        // --- Announcement Fetching Logic ---
        // Fetch the latest active announcement that is currently within its display period
        var latestAnnouncement = await _context.Announcements
            .Where(a => a.IsActive && // Check if the announcement is marked as active
                        (a.StartTime == null || a.StartTime <= DateTime.Now) && // Check if StartTime is null or in the past/present
                        (a.EndTime == null || a.EndTime >= DateTime.Now)) // Check if EndTime is null or in the future/present
            .OrderByDescending(a => a.DateAdded) // Order by DateAdded descending to get the newest first
            .FirstOrDefaultAsync(); // Get the first (newest) matching announcement

        // If an active announcement is found, add it to ViewData to be accessible in the view
        if (latestAnnouncement != null)
        {
            ViewData["Announcement"] = latestAnnouncement;
        }
        // --- End Announcement Fetching Logic ---

        // --- Existing Book Fetching Logic (Placeholder - you'll need to add this) ---
        // Assuming your homepage also displays books with search and pagination,
        // you would add that logic here. For now, we'll pass an empty list or null
        // if you haven't implemented book fetching in this controller yet.
        // Example (You need to replace this with your actual book fetching code):
        var books = new List<Book>(); // Replace with your actual book fetching logic
        // Example of how you might pass books and pagination data:
        // var pageSize = 12; // Define your page size
        // var source = _context.Books.AsQueryable(); // Start with your book DbSet
        // if (!string.IsNullOrEmpty(searchString))
        // {
        //     source = source.Where(b => b.Title.Contains(searchString) || b.Author.FirstName.Contains(searchString) || b.Author.LastName.Contains(searchString) || b.ISBN.Contains(searchString)); // Example search
        // }
        // var paginatedBooks = await PaginatedList<Book>.CreateAsync(source.AsNoTracking(), pageNumber ?? 1, pageSize);
        // ViewData["CurrentPage"] = paginatedBooks.PageIndex;
        // ViewData["TotalPages"] = paginatedBooks.TotalPages;
        // ViewData["SearchString"] = searchString;
        // return View(paginatedBooks); // Pass the paginated list of books as the model
        // --- End Book Fetching Logic ---


        // If you are not displaying books on the homepage yet, just return the view without a model:
         return View();
        // If you are displaying books, uncomment the book fetching logic above and return the paginated list.
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
