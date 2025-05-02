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
    public class BookFormatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookFormatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BookFormat
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BookFormats.Include(b => b.Book);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: BookFormat/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookFormat = await _context.BookFormats
                .Include(b => b.Book)
                .FirstOrDefaultAsync(m => m.BookFormatId == id);
            if (bookFormat == null)
            {
                return NotFound();
            }

            return View(bookFormat);
        }

        // GET: BookFormat/Create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn");
            return View();
        }

        // POST: BookFormat/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookFormatId,BookId,FormatType,Details")] BookFormat bookFormat)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bookFormat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", bookFormat.BookId);
            return View(bookFormat);
        }

        // GET: BookFormat/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookFormat = await _context.BookFormats.FindAsync(id);
            if (bookFormat == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", bookFormat.BookId);
            return View(bookFormat);
        }

        // POST: BookFormat/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookFormatId,BookId,FormatType,Details")] BookFormat bookFormat)
        {
            if (id != bookFormat.BookFormatId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bookFormat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookFormatExists(bookFormat.BookFormatId))
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
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "Isbn", bookFormat.BookId);
            return View(bookFormat);
        }

        // GET: BookFormat/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookFormat = await _context.BookFormats
                .Include(b => b.Book)
                .FirstOrDefaultAsync(m => m.BookFormatId == id);
            if (bookFormat == null)
            {
                return NotFound();
            }

            return View(bookFormat);
        }

        // POST: BookFormat/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bookFormat = await _context.BookFormats.FindAsync(id);
            if (bookFormat != null)
            {
                _context.BookFormats.Remove(bookFormat);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookFormatExists(int id)
        {
            return _context.BookFormats.Any(e => e.BookFormatId == id);
        }
    }
}
