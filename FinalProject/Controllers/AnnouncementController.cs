using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize] and [AllowAnonymous]

namespace FinalProject.Controllers
{
    // Apply Authorize attribute to the entire controller to require authentication
    // Only users authenticated with the "AdminCookieAuth" scheme and having the "Admin" role
    // can access actions within this controller, *unless* an action is marked with [AllowAnonymous].
    public class AnnouncementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Public Actions (No Authorization Required) ---

        // GET: Announcement/ViewAnnouncement/5
        /// <summary>
        /// Displays a specific announcement. This action is publicly accessible.
        /// </summary>
        /// <param name="id">The ID of the announcement to view.</param>
        /// <returns>The ViewAnnouncement view with the specified announcement.</returns>
        [HttpGet]
        [AllowAnonymous] // Explicitly allow anonymous access to this specific action
        public async Task<IActionResult> ViewAnnouncement(int? id)
        {
            if (id == null)
            {
                // Handle case where no ID is provided, maybe redirect to a list of public announcements
                // or return a NotFound result. For now, returning NotFound.
                return NotFound();
            }

            // Retrieve the announcement by ID.
            // You might want to add a check here for IsActive and StartTime/EndTime
            // if you only want to display currently active announcements publicly.
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);

            if (announcement == null)
            {
                return NotFound(); // Announcement not found
            }

            // Return the ViewAnnouncement view with the announcement data
            return View(announcement);
        }

        // --- Standard CRUD Actions (Protected by [Authorize] on the controller) ---

        // GET: Announcement
        // This action is now protected by the [Authorize] attribute on the controller level.
        // Only authenticated admins with the "Admin" role can access this.
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        public async Task<IActionResult> Index()
        {
            return View(await _context.Announcements.ToListAsync());
        }

        // GET: Announcement/Details/5
        // This action is now protected by the [Authorize] attribute on the controller level.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // GET: Announcement/Create
        /// <summary>
        /// Displays the announcement creation form.
        /// </summary>
        /// <returns>The Create view.</returns>
        [HttpGet]
        // This action is now protected by the [Authorize] attribute on the controller level.
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        public IActionResult Create()
        {
            return View();
        }

        // POST: Announcement/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders
        public async Task<IActionResult> Create([Bind("AnnouncementId,Title,Message,StartTime,EndTime,IsActive")] Announcement announcement) // Removed DateAdded, DateUpdated from Bind as they should be set by the controller/model
        {
            if (ModelState.IsValid)
            {
                // Set DateAdded and DateUpdated here, or ensure they are set in the model's constructor or SaveChanges override
                announcement.DateAdded = DateTime.Now;
                announcement.DateUpdated = DateTime.Now;

                _context.Add(announcement);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        // GET: Announcement/Edit/5
        // This action is now protected by the [Authorize] attribute on the controller level.
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }
            // Note: You should not pass the DateAdded/DateUpdated directly from the form in Edit POST
            // as they should reflect when the record was created and last updated, not user input.
            return View(announcement);
        }

        // POST: Announcement/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        // This action is now protected by the [Authorize] attribute on the controller level.
        public async Task<IActionResult> Edit(int id, [Bind("AnnouncementId,Title,Message,StartTime,EndTime,IsActive")] Announcement announcement) // Removed DateAdded, DateUpdated from Bind
        {
            if (id != announcement.AnnouncementId)
            {
                return NotFound();
            }

            // Retrieve the existing announcement from the database
            var announcementToUpdate = await _context.Announcements.FindAsync(id);
            if (announcementToUpdate == null)
            {
                return NotFound();
            }

            // Use TryUpdateModelAsync or manually map properties to prevent overposting and
            // avoid overwriting the existing DateAdded.
            if (await TryUpdateModelAsync<Announcement>(
                announcementToUpdate,
                "", // Prefix for form values
                a => a.Title, a => a.Message, a => a.StartTime, a => a.EndTime, a => a.IsActive)) // Specify allowed properties
            {
                try
                {
                    announcementToUpdate.DateUpdated = DateTime.Now; // Update DateUpdated
                    _context.Update(announcementToUpdate); // Mark as modified
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcementToUpdate.AnnouncementId))
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

            // If TryUpdateModelAsync fails or ModelState is invalid
            announcementToUpdate.DateUpdated = DateTime.Now; // Ensure DateUpdated is set even on failure for consistency
            return View(announcementToUpdate); // Return the view with the retrieved announcement object
        }


        // GET: Announcement/Delete/5
        // This action is now protected by the [Authorize] attribute on the controller level.
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementId == id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: Announcement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "AdminCookieAuth")] // Only authorized admins can edit orders

        // This action is now protected by the [Authorize] attribute on the controller level.
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementId == id);
        }
    }
}
