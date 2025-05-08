using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // Needed for [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;
using FinalProject.Models;
using FinalProject.ViewModels;
using BCrypt.Net; // Ensure you have this library installed

namespace FinalProject.Controllers
{
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Member/Register
        /// <summary>
        /// Displays the member registration form.
        /// </summary>
        /// <returns>The Register view.</returns>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Member/Register
        /// <summary>
        /// Handles the form submission for member registration.
        /// </summary>
        /// <param name="model">The RegisterViewModel bound from the form data.</param>
        /// <returns>Redirects to Login on success, otherwise returns the Register view with validation errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model) // Corrected to use RegisterViewModel
        {
            // Check if the model state is valid based on data annotations.
            if (ModelState.IsValid)
            {
                // Check if a member with the same email or membership ID already exists
                if (await _context.Members.AnyAsync(m => m.Email == model.Email || m.MembershipId == model.MembershipId))
                {
                    ModelState.AddModelError(string.Empty, "A member with this email or membership ID already exists.");
                    return View(model);
                }

                // Hash the password before saving
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Create a new Member object from the ViewModel
                var member = new Member
                {
                    MembershipId = model.MembershipId,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = hashedPassword, // Store the hashed password
                    RegistrationDate = DateTime.Now,
                    DateAdded = DateTime.Now,
                    DateUpdated = DateTime.Now,
                    OrderCount = 0, // Initialize order count
                    StackableDiscount = 0.00M // Initialize discount
                    // LastLogin will be set on successful login
                };

                // Add the new member to the context.
                _context.Add(member);
                // Save changes to the database asynchronously.
                await _context.SaveChangesAsync();

                // Redirect to the Login page after successful registration.
                return RedirectToAction(nameof(Login));
            }

            // If model state is not valid, return the Register view with the current model
            // to display validation errors.
            return View(model);
        }

        // GET: Member/Login
        /// <summary>
        /// Displays the member login form.
        /// </summary>
        /// <param name="returnUrl">The URL to return to after successful login.</param>
        /// <returns>The Login view.</returns>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Member/Login
        /// <summary>
        /// Handles the form submission for member login.
        /// </summary>
        /// <param name="model">The LoginViewModel bound from the form data.</param>
        /// <param name="returnUrl">The URL to return to after successful login.</param>
        /// <returns>Redirects to the returnUrl or Home/Index on success, otherwise returns the Login view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) // Corrected to use LoginViewModel
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Check if the model state is valid.
            if (ModelState.IsValid)
            {
                // Find the member by email (or MembershipId, depending on your login strategy)
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Email == model.Email);

                // Verify the member exists and the password is correct
                if (member != null && BCrypt.Net.BCrypt.Verify(model.Password, member.PasswordHash))
                {
                    // Password is correct, sign in the user

                    // Create claims for the user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, member.MemberId.ToString()), // Store MemberId
                        new Claim(ClaimTypes.Name, member.FullName), // Store full name (using the derived property)
                        new Claim(ClaimTypes.Email, member.Email), // Store email
                        new Claim("MembershipId", member.MembershipId) // Custom claim for MembershipId
                        // Add other claims as needed (e.g., roles)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe, // RememberMe is now correctly accessed from LoginViewModel
                        // Set expiration if persistent (adjust time as needed)
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddMinutes(30) : (DateTimeOffset?)null
                    };

                    // Sign in the user using cookie authentication
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Update LastLogin timestamp
                    member.LastLogin = DateTime.Now;
                    _context.Update(member);
                    await _context.SaveChangesAsync();

                    // Redirect to the return URL or a default page
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home"); // Redirect to Home page by default
                    }
                }
                else
                {
                    // Invalid login attempt
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If model state is not valid, return the Login view with the current model
            // to display validation errors.
            return View(model);
        }

        // POST: Member/Logout
        /// <summary>
        /// Logs out the current member.
        /// </summary>
        /// <returns>Redirects to the Home page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the Home page
            return RedirectToAction("Index", "Home");
        }

        // GET: Member/Profile
        /// <summary>
        /// Displays the profile page for the currently logged-in member.
        /// Requires authentication.
        /// </summary>
        /// <returns>The Profile view with member data, or redirects to login if not authenticated.</returns>
        [HttpGet]
        [Authorize] // Ensures only authenticated users can access this action
        public async Task<IActionResult> Profile()
        {
            // Get the MemberId from the authenticated user's claims
            var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                // This should ideally not happen with [Authorize], but as a safeguard
                // Redirect to login or an error page
                return RedirectToAction(nameof(Login));
            }

            // Parse the MemberId from the claim
            if (!int.TryParse(memberIdClaim.Value, out int memberId))
            {
                // Handle cases where the claim value is not a valid integer
                // Log this error and potentially log the user out or show an error page
                // For now, redirect to login
                 await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            // Retrieve the member from the database
            var member = await _context.Members
                .Include(m => m.Orders) // Include related data if you want to display it
                .Include(m => m.Reviews) // Include related data if you want to display it
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
            {
                // Member not found in the database (should not happen if claim is valid)
                // Log this error and potentially log the user out or show an error page
                 await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            // Map the Member model to the ProfileViewModel
            var viewModel = ProfileViewModel.FromMember(member);

            // Pass the ViewModel to the view
            return View(viewModel);
        }


        // GET: Member
        public async Task<IActionResult> Index()
        {
            return View(await _context.Members.ToListAsync());
        }


        // GET: Member/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // GET: Member/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Member/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MemberId,MembershipId,FirstName,LastName,Email,PasswordHash,RegistrationDate,LastLogin,OrderCount,StackableDiscount,DateAdded,DateUpdated")] Member member)
        {
            if (ModelState.IsValid)
            {
                // Note: You might want to hash the password here as well if creating via this action
                _context.Add(member);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        // GET: Member/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }
            return View(member);
        }

        // POST: Member/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MemberId,MembershipId,FirstName,LastName,Email,PasswordHash,RegistrationDate,LastLogin,OrderCount,StackableDiscount,DateAdded,DateUpdated")] Member member)
        {
            if (id != member.MemberId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Note: If the password is being edited via this form, you should re-hash it here.
                    _context.Update(member);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MemberExists(member.MemberId))
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
            return View(member);
        }

        // GET: Member/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: Member/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                _context.Members.Remove(member);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MemberExists(int id)
        {
            return _context.Members.Any(e => e.MemberId == id);
        }

        // You might want to add an AccessDenied action and view later
        // [HttpGet]
        // public IActionResult AccessDenied()
        // {
        //     return View();
        // }
    }
}
